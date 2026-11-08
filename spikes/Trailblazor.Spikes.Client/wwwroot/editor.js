// Spike D's throwaway editor engine. See spikes/rich-text-editor/README.md.
// A paragraph is runs { text, marks in MARKS order }, '\n' is a line break, and offsets count UTF-16 code units.

const MARKS = ['bold', 'italic', 'underline', 'strikethrough', 'subscript', 'superscript'];
const MARK_TAGS = {
    bold: 'strong', italic: 'em', underline: 'u', strikethrough: 's', subscript: 'sub', superscript: 'sup',
};
const TAG_MARKS = {
    b: 'bold', strong: 'bold', i: 'italic', em: 'italic', u: 'underline', ins: 'underline',
    s: 'strikethrough', strike: 'strikethrough', del: 'strikethrough', sub: 'subscript', sup: 'superscript',
};
const FORMAT_INPUTS = {
    formatBold: 'bold', formatItalic: 'italic', formatUnderline: 'underline', formatStrikeThrough: 'strikethrough',
    formatSubscript: 'subscript', formatSuperscript: 'superscript',
};
const SHORTCUTS = { b: 'bold', i: 'italic', u: 'underline' };
const BLOCKS = new Set(['address', 'article', 'aside', 'blockquote', 'dd', 'div', 'dl', 'dt', 'figcaption', 'figure',
    'footer', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'header', 'li', 'main', 'nav', 'ol', 'p', 'pre', 'section', 'table',
    'td', 'th', 'tr', 'ul']);
const SKIPPED = new Set(['head', 'iframe', 'math', 'noscript', 'object', 'script', 'select', 'style', 'svg', 'template',
    'textarea', 'title']);
const TYPING_PAUSE = 1000;
const SEND_DELAY = 300;
const isMac = /Mac|iPhone|iPad/.test(navigator.userAgentData?.platform ?? navigator.platform);

export function attach(root, toolbar, dotnet, html) {
    const editor = new Editor(root, toolbar, dotnet, html);
    return { setValue: value => editor.setValue(value), dispose: () => editor.dispose() };
}

class Editor {
    constructor(root, toolbar, dotnet, html) {
        this.root = root;
        this.toolbar = toolbar;
        this.dotnet = dotnet;
        this.selection = null;
        this.storedMarks = null;
        this.undoStack = [];
        this.redoStack = [];
        this.lastEdit = null;
        this.composing = false;
        this.plainPaste = false;
        this.pendingBlocks = new Set();
        this.pendingAll = false;
        this.sendTimer = 0;
        this.abort = new AbortController();
        this.observer = new MutationObserver(records => {
            this.note(records);
            if (!this.composing) this.readBack();
        });

        const options = { signal: this.abort.signal };
        root.addEventListener('beforeinput', event => this.onBeforeInput(event), options);
        root.addEventListener('keydown', event => this.onKeyDown(event), options);
        root.addEventListener('keyup', () => this.plainPaste = false, options);
        root.addEventListener('paste', event => this.onPaste(event), options);
        root.addEventListener('compositionstart', () => this.composing = true, options);
        root.addEventListener('compositionend', () => {
            this.composing = false;
            this.readBack();
        }, options);
        root.addEventListener('focusout', event => {
            if (!root.contains(event.relatedTarget)) this.send();
        }, options);
        document.addEventListener('selectionchange', () => this.onSelectionChange(), options);
        // Pressing a toolbar button must not take the selection out of the editor.
        toolbar.addEventListener('mousedown', event => {
            if (event.target.closest('[data-mark]')) event.preventDefault();
        }, options);
        toolbar.addEventListener('click', event => {
            const button = event.target.closest('[data-mark]');
            if (!button) return;
            this.toggleMark(button.dataset.mark);
            root.focus();
            this.writeSelection();
        }, options);

        this.load(html);
        this.observer.observe(root, { childList: true, characterData: true, subtree: true });
        // Test hooks: the spike's tests read the engine's state directly.
        root.spikeEditor = this;
        root.dataset.ready = '';
    }

    value() {
        return toHtml(this.doc);
    }

    setValue(html) {
        if (html !== this.value()) this.load(html);
    }

    load(html) {
        this.doc = parseHtml(html);
        this.undoStack = [];
        this.redoStack = [];
        this.storedMarks = null;
        this.lastSent = this.value();
        this.selection = null;
        this.render();
        if (document.activeElement === this.root) {
            this.selection = this.endSelection();
            this.writeSelection();
        }
        this.updateToolbar();
    }

    dispose() {
        this.abort.abort();
        this.observer.disconnect();
        clearTimeout(this.sendTimer);
    }

    // Edits the browser performs itself (typing, composition, spellcheck) are read back from the DOM.

    note(records) {
        for (const record of records) {
            const block = record.target === this.root ? -1 : this.blockOf(record.target);
            if (block < 0) this.pendingAll = true;
            else this.pendingBlocks.add(block);
        }
    }

    readBack() {
        this.note(this.observer.takeRecords());
        if (!this.pendingAll && !this.pendingBlocks.size) return;
        const blocks = [...this.pendingBlocks];
        const readAll = this.pendingAll || this.root.children.length !== this.doc.length
            || blocks.some(block => !this.root.children[block]
                || readBlocks(this.root.children[block], false).length > 1);
        this.pendingAll = false;
        this.pendingBlocks.clear();
        const selection = this.readSelection();

        if (readAll) {
            // The browser changed the paragraphs themselves, so the DOM's own marks are taken as they are.
            this.checkpoint('type', '');
            const doc = readBlocks(this.root, false);
            this.doc = doc.length ? doc : [[]];
        } else {
            for (const block of blocks) {
                const after = textOf(readBlocks(this.root.children[block], false)[0] ?? []);
                const caret = selection?.head.block === block ? selection.head.offset : null;
                this.readText(block, after, caret);
            }
        }

        this.selection = selection ?? this.selection;
        this.render();
        this.writeSelection();
        this.changed();
    }

    // Applies the browser's change to one paragraph's text; the engine, not the DOM, decides the new text's marks.
    readText(block, after, caret) {
        const runs = this.doc[block];
        const before = textOf(runs);
        if (before === after) return;
        const { start, endBefore, endAfter } = diff(before, after, caret);
        const inserted = after.slice(start, endAfter);
        const marks = this.storedMarks
            ?? (endBefore > start ? slice(runs, start, start + 1)[0].marks : marksAt(runs, start));
        this.checkpoint('type', inserted);
        this.doc[block] = replace(runs, start, endBefore, [{ text: inserted, marks }]);
        if (inserted) this.storedMarks = null;
    }

    // Everything else the engine performs on the model, canceling the browser's own handling.

    onBeforeInput(event) {
        if (this.composing || event.isComposing) return;
        const type = event.inputType;
        const handle = action => {
            event.preventDefault();
            action();
        };

        if (FORMAT_INPUTS[type]) return handle(() => this.toggleMark(FORMAT_INPUTS[type]));
        if (type === 'historyUndo') return handle(() => this.undo());
        if (type === 'historyRedo') return handle(() => this.redo());

        this.readBack();
        this.selection = this.readSelection() ?? this.selection;
        const { from, to } = this.range();
        const collapsed = compare(from, to) === 0;
        const spans = from.block !== to.block;

        if (type === 'insertParagraph') return handle(() => this.edit(() => this.splitBlock()));
        if (type === 'insertLineBreak') return handle(() => this.edit(() => this.insertText('\n')));
        if (type === 'insertText' && spans) return handle(() => this.edit(() => this.insertText(event.data ?? '')));
        if (type === 'insertText' || type === 'insertReplacementText') return;
        // A drag's deletion is left to the drop, which the engine refuses below, so dragged text isn't lost.
        if (type.startsWith('delete') && type !== 'deleteByDrag') {
            if (spans) return handle(() => this.edit(() => this.deleteRange(from, to)));
            if (collapsed && from.offset === 0 && type.endsWith('Backward')) {
                return handle(() => from.block > 0 && this.edit(() => this.join(from.block - 1)));
            }
            if (collapsed && from.offset === length(this.doc[from.block]) && type.endsWith('Forward')) {
                return handle(() => from.block < this.doc.length - 1 && this.edit(() => this.join(from.block)));
            }
            return;
        }
        // Lists, links, drops and raw HTML have no place in this model.
        event.preventDefault();
    }

    onKeyDown(event) {
        // Safari ends a composition before the keydown of the Enter that commits it, marking that key only as 229.
        if (event.isComposing || event.keyCode === 229 || event.altKey) return;
        // WebKit reports Shift+Enter as a new paragraph, so Enter is handled here; beforeinput covers soft keyboards.
        if (event.key === 'Enter' && !event.ctrlKey && !event.metaKey) {
            event.preventDefault();
            this.readBack();
            this.selection = this.readSelection() ?? this.selection;
            this.edit(() => event.shiftKey ? this.insertText('\n') : this.splitBlock());
            return;
        }
        if (!(isMac ? event.metaKey : event.ctrlKey)) return;
        const key = event.key.toLowerCase();
        if (SHORTCUTS[key] && !event.shiftKey) {
            event.preventDefault();
            this.toggleMark(SHORTCUTS[key]);
        } else if (key === 'z') {
            event.preventDefault();
            if (event.shiftKey) this.redo();
            else this.undo();
        } else if (key === 'y' && !event.shiftKey) {
            event.preventDefault();
            this.redo();
        } else if (key === 'v' && event.shiftKey) {
            this.plainPaste = true;
        }
    }

    onPaste(event) {
        event.preventDefault();
        const html = this.plainPaste ? '' : event.clipboardData?.getData('text/html') ?? '';
        const text = event.clipboardData?.getData('text/plain') ?? '';
        this.plainPaste = false;
        this.readBack();
        this.selection = this.readSelection() ?? this.selection;
        const pasted = html ? readBlocks(parseDocument(html), true) : null;
        if (pasted?.length === 0 || (!pasted && !text)) return;
        this.edit(() => {
            const { from, to } = this.range();
            const marks = this.storedMarks ?? this.marksForReplacing(from, to);
            this.insertParagraphs(pasted ?? text.split(/\r\n?|\n/).map(line => normalize([{ text: line, marks }])));
        });
    }

    onSelectionChange() {
        if (this.composing || document.activeElement !== this.root) return;
        const selection = this.readSelection();
        if (!selection) return;
        if (!this.selection || compare(selection.anchor, this.selection.anchor) !== 0
            || compare(selection.head, this.selection.head) !== 0) {
            this.selection = selection;
            this.storedMarks = null;
        }
        this.updateToolbar();
    }

    // Model edits.

    edit(change) {
        this.checkpoint('edit', '');
        change();
        this.render();
        this.writeSelection();
        this.changed();
    }

    deleteRange(from, to) {
        const merged = normalize([
            ...slice(this.doc[from.block], 0, from.offset), ...slice(this.doc[to.block], to.offset)]);
        this.doc.splice(from.block, to.block - from.block + 1, merged);
        this.selection = { anchor: from, head: from };
    }

    insertText(text) {
        const { from, to } = this.range();
        const marks = this.storedMarks ?? this.marksForReplacing(from, to);
        this.deleteRange(from, to);
        this.doc[from.block] = replace(this.doc[from.block], from.offset, from.offset, [{ text, marks }]);
        const caret = { block: from.block, offset: from.offset + text.length };
        this.selection = { anchor: caret, head: caret };
        this.storedMarks = null;
    }

    insertParagraphs(paragraphs) {
        const { from, to } = this.range();
        this.deleteRange(from, to);
        const runs = this.doc[from.block];
        const before = slice(runs, 0, from.offset);
        const after = slice(runs, from.offset);
        const last = paragraphs.length - 1;
        const blocks = paragraphs.map((paragraph, i) =>
            normalize([...(i === 0 ? before : []), ...paragraph, ...(i === last ? after : [])]));
        this.doc.splice(from.block, 1, ...blocks);
        const caret = { block: from.block + last, offset: length(blocks[last]) - length(after) };
        this.selection = { anchor: caret, head: caret };
        this.storedMarks = null;
    }

    splitBlock() {
        const { from, to } = this.range();
        const marks = this.storedMarks ?? this.marksForReplacing(from, to);
        this.deleteRange(from, to);
        const runs = this.doc[from.block];
        this.doc.splice(from.block, 1, slice(runs, 0, from.offset), slice(runs, from.offset));
        const caret = { block: from.block + 1, offset: 0 };
        this.selection = { anchor: caret, head: caret };
        // A new empty paragraph keeps the formatting the caret had, as word processors do.
        this.storedMarks = this.doc[from.block + 1].length ? null : marks;
    }

    join(block) {
        const offset = length(this.doc[block]);
        this.doc.splice(block, 2, normalize([...this.doc[block], ...this.doc[block + 1]]));
        this.selection = { anchor: { block, offset }, head: { block, offset } };
    }

    toggleMark(mark) {
        this.readBack();
        const { from, to } = this.range();
        if (compare(from, to) === 0) {
            const current = this.storedMarks ?? marksAt(this.doc[from.block], from.offset);
            this.storedMarks = current.includes(mark) ? current.filter(m => m !== mark) : withMark(current, mark);
            this.updateToolbar();
            return;
        }
        const add = this.markState(mark) !== 'on';
        this.edit(() => {
            for (let block = from.block; block <= to.block; block++) {
                const start = block === from.block ? from.offset : 0;
                const end = block === to.block ? to.offset : Infinity;
                this.doc[block] = mapMarks(this.doc[block], start, end,
                    marks => add ? withMark(marks, mark) : marks.filter(m => m !== mark));
            }
        });
    }

    markState(mark) {
        const { from, to } = this.range();
        if (compare(from, to) === 0) {
            return (this.storedMarks ?? marksAt(this.doc[from.block], from.offset)).includes(mark) ? 'on' : 'off';
        }
        let some = false;
        let all = true;
        for (let block = from.block; block <= to.block; block++) {
            const start = block === from.block ? from.offset : 0;
            const end = block === to.block ? to.offset : Infinity;
            for (const run of slice(this.doc[block], start, end)) {
                if (/^\n*$/.test(run.text)) continue;
                if (run.marks.includes(mark)) some = true;
                else all = false;
            }
        }
        return some ? (all ? 'on' : 'mixed') : 'off';
    }

    // Typing over a selection keeps the formatting of its first character.
    marksForReplacing(from, to) {
        const runs = this.doc[from.block];
        return compare(from, to) < 0 && from.offset < length(runs)
            ? slice(runs, from.offset, from.offset + 1)[0].marks
            : marksAt(runs, from.offset);
    }

    // History: snapshots of the document, with typing grouped into word-sized steps.

    checkpoint(kind, text) {
        const now = Date.now();
        const last = this.lastEdit;
        const continues = kind === 'type' && last?.kind === 'type' && now - last.time < TYPING_PAUSE
            && !/\s$/.test(last.text);
        if (!continues) {
            this.undoStack.push(this.snapshot());
            this.redoStack = [];
        }
        this.lastEdit = { kind, time: now, text };
    }

    snapshot() {
        return structuredClone({ doc: this.doc, selection: this.selection });
    }

    undo() {
        this.step(this.undoStack, this.redoStack);
    }

    redo() {
        this.step(this.redoStack, this.undoStack);
    }

    step(from, to) {
        if (this.composing) return;
        this.readBack();
        const entry = from.pop();
        if (!entry) return;
        to.push(this.snapshot());
        this.doc = entry.doc;
        this.selection = entry.selection;
        this.storedMarks = null;
        this.lastEdit = null;
        this.render();
        this.writeSelection();
        this.changed();
    }

    // Rendering and selection.

    render() {
        this.doc.forEach((runs, i) => {
            const html = paragraphHtml(runs, false);
            const element = this.root.children[i];
            if (element?.localName === 'p' && !element.attributes.length && element.innerHTML === html) return;
            const paragraph = document.createElement('p');
            // Safe: paragraphHtml escapes every character of text, and its only tags are the marks' own.
            paragraph.innerHTML = html;
            if (element) element.replaceWith(paragraph);
            else this.root.append(paragraph);
        });
        while (this.root.children.length > this.doc.length) this.root.lastElementChild.remove();
        for (const node of [...this.root.childNodes]) {
            if (node.nodeType !== Node.ELEMENT_NODE) node.remove();
        }
        this.observer.takeRecords();
    }

    blockOf(node) {
        while (node && node.parentNode !== this.root) node = node.parentNode;
        return node ? Array.prototype.indexOf.call(this.root.children, node) : -1;
    }

    readSelection() {
        const selection = document.getSelection();
        if (!selection.rangeCount || !this.root.contains(selection.anchorNode)) return null;
        return {
            anchor: this.positionOf(selection.anchorNode, selection.anchorOffset),
            head: this.positionOf(selection.focusNode, selection.focusOffset),
        };
    }

    positionOf(node, offset) {
        const last = this.root.children.length - 1;
        if (node === this.root) {
            if (offset <= last) return { block: offset, offset: 0 };
            if (last < 0) return { block: 0, offset: 0 };
            return { block: last, offset: length(readBlocks(this.root.children[last], false)[0] ?? []) };
        }
        const block = this.blockOf(node);
        // Text the browser put straight into the editor, outside any paragraph, maps to the start until read back.
        if (block < 0) return { block: 0, offset: 0 };
        return { block, offset: offsetAt(this.root.children[block], node, offset) };
    }

    writeSelection() {
        if (!this.selection || document.activeElement !== this.root) return;
        const { anchor, head } = this.selection;
        const [anchorNode, anchorOffset] = pointAt(this.root.children[anchor.block], anchor.offset);
        const [focusNode, focusOffset] = pointAt(this.root.children[head.block], head.offset);
        document.getSelection().setBaseAndExtent(anchorNode, anchorOffset, focusNode, focusOffset);
    }

    range() {
        const selection = this.selection ?? this.endSelection();
        return compare(selection.anchor, selection.head) <= 0
            ? { from: selection.anchor, to: selection.head }
            : { from: selection.head, to: selection.anchor };
    }

    endSelection() {
        const end = { block: this.doc.length - 1, offset: length(this.doc.at(-1)) };
        return { anchor: end, head: end };
    }

    updateToolbar() {
        for (const button of this.toolbar.querySelectorAll('[data-mark]')) {
            const state = this.markState(button.dataset.mark);
            button.setAttribute('aria-pressed', state === 'mixed' ? 'mixed' : String(state === 'on'));
        }
    }

    // The value goes to .NET after a pause in typing, and at once when focus leaves the editor.

    changed() {
        this.updateToolbar();
        clearTimeout(this.sendTimer);
        this.sendTimer = setTimeout(() => this.send(), SEND_DELAY);
    }

    send() {
        clearTimeout(this.sendTimer);
        const html = this.value();
        if (html === this.lastSent) return;
        this.lastSent = html;
        this.dotnet.invokeMethodAsync('OnValueChanged', html);
    }
}

// Paragraph helpers.

const sameMarks = (a, b) => a.length === b.length && a.every((mark, i) => mark === b[i]);
const length = runs => runs.reduce((sum, run) => sum + run.text.length, 0);
const textOf = runs => runs.map(run => run.text).join('');
const compare = (a, b) => a.block - b.block || a.offset - b.offset;

function normalize(runs) {
    const result = [];
    for (const run of runs) {
        if (!run.text) continue;
        const last = result.at(-1);
        if (last && sameMarks(last.marks, run.marks)) {
            result[result.length - 1] = { text: last.text + run.text, marks: last.marks };
        } else {
            result.push(run);
        }
    }
    return result;
}

function slice(runs, from, to = Infinity) {
    const result = [];
    let start = 0;
    for (const run of runs) {
        const end = start + run.text.length;
        const sliceStart = Math.max(from, start);
        const sliceEnd = Math.min(to, end);
        if (sliceStart < sliceEnd) {
            result.push({ text: run.text.slice(sliceStart - start, sliceEnd - start), marks: run.marks });
        }
        start = end;
    }
    return result;
}

const replace = (runs, from, to, inserted) => normalize([...slice(runs, 0, from), ...inserted, ...slice(runs, to)]);

const mapMarks = (runs, from, to, change) => normalize([
    ...slice(runs, 0, from),
    ...slice(runs, from, to).map(run => ({ text: run.text, marks: change(run.marks) })),
    ...slice(runs, to),
]);

// Text typed at an offset takes the marks of the character before it, or of the first at a paragraph's start.
function marksAt(runs, offset) {
    const at = offset > 0 ? offset - 1 : 0;
    return slice(runs, at, at + 1)[0]?.marks ?? [];
}

function withMark(marks, mark) {
    const set = new Set(marks);
    set.add(mark);
    if (mark === 'subscript') set.delete('superscript');
    if (mark === 'superscript') set.delete('subscript');
    return MARKS.filter(m => set.has(m));
}

// Repeated text makes a change ambiguous ("a" typed into "aa"), so the change slides back toward the caret.
function diff(before, after, caret) {
    const shorter = Math.min(before.length, after.length);
    let start = 0;
    while (start < shorter && before[start] === after[start]) start++;
    let endBefore = before.length;
    let endAfter = after.length;
    while (endBefore > start && endAfter > start && before[endBefore - 1] === after[endAfter - 1]) {
        endBefore--;
        endAfter--;
    }
    if (caret !== null && endBefore === start) {
        while (endAfter > caret && start > 0 && after[start - 1] === after[endAfter - 1]) {
            start--;
            endBefore--;
            endAfter--;
        }
    } else if (caret !== null && endAfter === start) {
        while (start > caret && before[start - 1] === before[endBefore - 1]) {
            start--;
            endBefore--;
            endAfter--;
        }
    }
    return { start, endBefore, endAfter };
}

// The value and the editor's DOM differ only in spaces: the value writes the ones HTML would collapse as &nbsp;.

function toHtml(doc) {
    if (doc.length === 1 && !doc[0].length) return '';
    return doc.map(runs => `<p>${paragraphHtml(runs, true)}</p>`).join('');
}

function paragraphHtml(runs, forValue) {
    const text = textOf(runs);
    let html = '';
    let offset = 0;
    for (const run of runs) {
        let inner = '';
        for (let i = 0; i < run.text.length; i++) inner += characterHtml(text, offset + i, forValue);
        offset += run.text.length;
        html += run.marks.reduceRight((content, mark) => `<${MARK_TAGS[mark]}>${content}</${MARK_TAGS[mark]}>`, inner);
    }
    // A paragraph's last line break shows nothing, so an empty paragraph or a trailing break needs one more.
    return !text || text.endsWith('\n') ? html + '<br>' : html;
}

function characterHtml(text, i, forValue) {
    const character = text[i];
    switch (character) {
        case '\n': return '<br>';
        case '&': return '&amp;';
        case '<': return '&lt;';
        case '>': return '&gt;';
        case ' ': return '&nbsp;';
        case ' ': {
            const collapses = i === 0 || i === text.length - 1 || ' \n'.includes(text[i - 1]) || text[i + 1] === '\n';
            return forValue && collapses ? '&nbsp;' : ' ';
        }
        default: return character;
    }
}

// HTML in: blocks become paragraphs, the six marks survive, and everything else becomes plain text.

// DOMParser's documents run no script and load nothing.
const parseDocument = html => new DOMParser().parseFromString(html, 'text/html').body;

function parseHtml(html) {
    const doc = readBlocks(parseDocument(html), true);
    return doc.length ? doc : [[]];
}

function readBlocks(container, collapse) {
    const paragraphs = [];
    let runs = null;
    const finish = () => {
        if (runs) {
            const paragraph = finishParagraph(runs, collapse);
            if (!collapse || paragraph.length) paragraphs.push(paragraph);
        }
        runs = null;
    };
    const walk = (node, marks, pre) => {
        for (const child of node.childNodes) {
            if (child.nodeType === Node.TEXT_NODE) {
                const text = collapse && !pre ? child.data.replace(/[\t\n\r\f ]+/g, ' ') : child.data;
                (runs ??= []).push({ text, marks });
            } else if (child.nodeType === Node.ELEMENT_NODE && !SKIPPED.has(child.localName)
                && !isWordListMarker(child)) {
                if (child.localName === 'br') {
                    (runs ??= []).push({ text: '\n', marks });
                    continue;
                }
                const childMarks = marksOf(child, marks);
                const childPre = pre || child.localName === 'pre';
                if (!BLOCKS.has(child.localName)) {
                    walk(child, childMarks, childPre);
                    continue;
                }
                finish();
                const start = paragraphs.length;
                walk(child, childMarks, childPre);
                finish();
                if (paragraphs.length === start) paragraphs.push([]);
            }
        }
    };
    walk(container, [], false);
    finish();
    return paragraphs;
}

function finishParagraph(runs, collapse) {
    if (collapse) {
        // As HTML renders it: no space after another space or at a line's start, and none at a line's end.
        runs = keepCharacters(runs, (text, i) => text[i] !== ' ' || (i > 0 && !' \n'.includes(text[i - 1])));
        runs = keepCharacters(runs, (text, i) => text[i] !== ' ' || (i < text.length - 1 && text[i + 1] !== '\n'));
    }
    // A paragraph's last line break shows nothing.
    const text = textOf(runs);
    return normalize(text.endsWith('\n') ? slice(runs, 0, text.length - 1) : runs);
}

function keepCharacters(runs, keep) {
    const text = textOf(runs);
    let offset = 0;
    return runs.map(run => {
        let kept = '';
        for (let i = 0; i < run.text.length; i++) {
            if (keep(text, offset + i)) kept += run.text[i];
        }
        offset += run.text.length;
        return { text: kept, marks: run.marks };
    });
}

// Word writes a list item's bullet or number as text, in a span styled mso-list: Ignore.
const isWordListMarker = element => /mso-list:\s*ignore/i.test(element.getAttribute('style') ?? '');

function marksOf(element, inherited) {
    let marks = inherited;
    const add = mark => marks = withMark(marks, mark);
    const remove = (...names) => marks = marks.filter(mark => !names.includes(mark));
    if (TAG_MARKS[element.localName]) add(TAG_MARKS[element.localName]);
    const style = element.style;
    if (style) {
        const weight = style.fontWeight;
        if (weight === 'bold' || weight === 'bolder' || Number(weight) >= 600) add('bold');
        else if (weight === 'normal' || weight === 'lighter' || (weight && Number(weight) < 600)) remove('bold');
        if (style.fontStyle === 'italic' || style.fontStyle === 'oblique') add('italic');
        else if (style.fontStyle === 'normal') remove('italic');
        const decoration = `${style.textDecorationLine} ${style.textDecoration}`;
        if (decoration.includes('underline')) add('underline');
        if (decoration.includes('line-through')) add('strikethrough');
        if (style.verticalAlign === 'super') add('superscript');
        else if (style.verticalAlign === 'sub') add('subscript');
        else if (style.verticalAlign === 'baseline') remove('subscript', 'superscript');
    }
    return marks;
}

// A paragraph's last line break shows nothing, so it counts for nothing in offsets.

function leaves(block) {
    const walker = document.createTreeWalker(block, NodeFilter.SHOW_ELEMENT | NodeFilter.SHOW_TEXT, {
        acceptNode: node => node.nodeType === Node.TEXT_NODE || node.localName === 'br'
            ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_SKIP,
    });
    const result = [];
    while (walker.nextNode()) result.push(walker.currentNode);
    return result;
}

const leafLength = (leaf, i, all) => leaf.nodeType === Node.TEXT_NODE ? leaf.data.length : i === all.length - 1 ? 0 : 1;
const indexOf = node => Array.prototype.indexOf.call(node.parentNode.childNodes, node);

function offsetAt(block, node, offset) {
    const all = leaves(block);
    const point = document.createRange();
    const size = node.nodeType === Node.TEXT_NODE ? node.data.length : node.childNodes.length;
    point.setStart(node, Math.min(offset, size));
    let count = 0;
    for (const [i, leaf] of all.entries()) {
        if (leaf === node) return count + offset;
        if (point.comparePoint(leaf.parentNode, indexOf(leaf) + 1) > 0) break;
        count += leafLength(leaf, i, all);
    }
    return count;
}

function pointAt(block, offset) {
    const all = leaves(block);
    let count = 0;
    for (const [i, leaf] of all.entries()) {
        const leafSize = leafLength(leaf, i, all);
        if (leaf.nodeType === Node.TEXT_NODE ? offset <= count + leafSize : offset === count) {
            return leaf.nodeType === Node.TEXT_NODE ? [leaf, offset - count] : [leaf.parentNode, indexOf(leaf)];
        }
        count += leafSize;
    }
    return [block, block.childNodes.length];
}
