// The rich text editor's engine. The browser types, composes and spellchecks natively and the engine reads the result
// back; every other edit the engine makes on its own model and renders. It never uses document.execCommand.
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
// Ctrl or Cmd with these keys, as in word processors; strikethrough is Ctrl+Shift+X, as in most web editors.
const SHORTCUTS = { b: 'bold', i: 'italic', u: 'underline', ',': 'subscript', '.': 'superscript' };
const BLOCKS = new Set(['address', 'article', 'aside', 'blockquote', 'caption', 'center', 'dd', 'details', 'dialog',
    'dir', 'div', 'dl', 'dt', 'fieldset', 'figcaption', 'figure', 'footer', 'form', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
    'header', 'hgroup', 'legend', 'li', 'listing', 'main', 'menu', 'nav', 'ol', 'p', 'plaintext', 'pre', 'search',
    'section', 'summary', 'table', 'td', 'th', 'tr', 'ul', 'xmp']);
const SKIPPED = new Set(['head', 'iframe', 'math', 'noembed', 'noframes', 'noscript', 'object', 'script', 'select',
    'style', 'svg', 'template', 'textarea', 'title']);
const PREFORMATTED = new Set(['pre', 'listing', 'xmp', 'plaintext']);
const TYPING_PAUSE = 1000;
// Keeps each part under Blazor Server's default 32 KB message limit, even at 6 bytes of JSON per control character.
const MAX_PART = 5000;
const isMac = /Mac|iPhone|iPad/.test(navigator.userAgentData?.platform ?? navigator.platform);

// options: { formats, readOnly, updateOnInput, updateDelay }, with formats as mark names split by spaces and
// updateDelay in milliseconds.
export function attachRichTextEditor(root, value, options) {
    const toolbar = root.closest('.tb-rich-text-editor')?.querySelector('[data-part="toolbar"]') ?? null;
    const label = document.getElementById(root.getAttribute('aria-labelledby') ?? '');
    const editor = new Editor(root, toolbar, label, value, options);
    return {
        setValue: html => editor.setValue(html),
        setOptions: changed => editor.setOptions(changed),
        dispose: () => editor.dispose(),
    };
}

class Editor {
    constructor(root, toolbar, label, value, options) {
        this.root = root;
        this.toolbar = toolbar;
        this.selection = null;
        this.storedMarks = null;
        this.undoStack = [];
        this.redoStack = [];
        this.lastEdit = null;
        this.composing = false;
        this.plainPaste = false;
        this.dragged = null;
        this.pendingBlocks = new Map();
        this.pendingAll = false;
        this.sendTimer = 0;
        this.abort = new AbortController();
        this.setOptions(options);
        this.observer = new MutationObserver(records => {
            this.note(records);
            if (!this.composing) this.readBack();
        });

        const on = (target, type, handler, capture = false) =>
            target?.addEventListener(type, handler, { signal: this.abort.signal, capture });
        on(root, 'beforeinput', event => this.onBeforeInput(event));
        on(root, 'keydown', event => this.onKeyDown(event));
        on(root, 'keyup', () => this.plainPaste = false);
        on(root, 'paste', event => this.onPaste(event));
        on(root, 'dragstart', () => {
            this.selection = this.readSelection() ?? this.selection;
            this.dragged = this.hasRange() ? this.range() : null;
        });
        on(root, 'dragend', () => this.dragged = null);
        on(root, 'drop', event => this.onDrop(event));
        on(root, 'compositionstart', () => this.onCompositionStart());
        on(root, 'compositionend', () => {
            this.composing = false;
            this.readBack();
        });
        on(root, 'focusout', event => {
            if (!root.contains(event.relatedTarget) && !toolbar?.contains(event.relatedTarget)) this.send();
        });
        on(document, 'selectionchange', () => this.onSelectionChange());
        // The label names the editor without a for attribute, so a click on it doesn't focus the editor by itself.
        on(label, 'click', () => {
            if (root.getAttribute('aria-disabled') !== 'true') root.focus();
        });
        // Pressing a toolbar button must not take the selection out of the editor.
        on(toolbar, 'mousedown', event => {
            if (event.target.closest('[data-format]')) event.preventDefault();
        });
        on(toolbar, 'click', event => {
            const button = event.target.closest('[data-format]');
            if (!button || button.disabled) return;
            this.toggleMark(button.dataset.format);
            root.focus();
            this.writeSelection();
        });

        this.load(value);
        this.observer.observe(root, { childList: true, characterData: true, subtree: true });
    }

    setOptions(options) {
        this.formats = new Set(options.formats.split(' ').filter(mark => MARKS.includes(mark)));
        this.readOnly = !!options.readOnly;
        this.updateOnInput = !!options.updateOnInput;
        this.updateDelay = options.updateDelay ?? 0;
        if (this.doc) this.updateToolbar();
    }

    value() {
        return toHtml(this.doc);
    }

    setValue(html) {
        if (html !== this.value()) this.load(html);
    }

    load(html) {
        this.doc = parseHtml(html, this.formats);
        this.undoStack = [];
        this.redoStack = [];
        this.lastEdit = null;
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
            if (block < 0) {
                this.pendingAll = true;
                continue;
            }

            const nodes = this.pendingBlocks.get(block) ?? new Set();
            if (record.type === 'characterData') nodes.add(record.target);
            this.pendingBlocks.set(block, nodes);
        }
    }

    readBack() {
        this.note(this.observer.takeRecords());
        if (!this.pendingAll && !this.pendingBlocks.size) return;
        const pending = [...this.pendingBlocks];
        const readAll = this.pendingAll || this.root.children.length !== this.doc.length
            || pending.some(([block]) => !this.root.children[block]
                || readBlocks(this.root.children[block], false, this.formats).length > 1);
        this.pendingAll = false;
        this.pendingBlocks.clear();
        // A read-only editor puts back whatever an input method or extension changed.
        if (this.readOnly) {
            this.render();
            this.writeSelection();
            return;
        }

        const selection = this.readSelection();
        if (readAll) {
            // The browser changed the paragraphs themselves, so the DOM's own marks are taken as they are.
            this.checkpoint('type', '');
            this.doc = this.readDocument();
        } else {
            for (const [block, nodes] of pending) {
                const read = readBlocks(this.root.children[block], false, this.formats)[0] ?? [];
                // Changes in several text nodes at once can't be told apart, so their marks come from the DOM.
                if (nodes.size > 1) {
                    if (textOf(read) !== textOf(this.doc[block]) || !sameRuns(read, this.doc[block])) {
                        this.checkpoint('type', '');
                        this.doc[block] = read;
                    }
                    continue;
                }

                const caret = selection?.head.block === block ? selection.head.offset : null;
                this.readText(block, textOf(read), caret);
            }
        }

        this.selection = selection ?? this.selection;
        this.render();
        this.writeSelection();
        this.changed();
    }

    readDocument() {
        const doc = readBlocks(this.root, false, this.formats);
        return doc.length ? doc : [[]];
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

    // Everything else is a cancelable beforeinput that the engine performs on the model.

    onBeforeInput(event) {
        if (this.composing || event.isComposing) return;
        const type = event.inputType;
        const handle = action => {
            event.preventDefault();
            action();
        };

        if (this.readOnly) return handle(() => {});
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
        // Drops are handled whole in onDrop.
        if (type === 'deleteByDrag' || type === 'insertFromDrop') return handle(() => {});
        if (type.startsWith('delete')) {
            if (spans) return handle(() => this.edit(() => this.deleteRange(from, to)));
            if (collapsed && from.offset === 0 && type.endsWith('Backward')) {
                return handle(() => from.block > 0 && this.edit(() => this.join(from.block - 1)));
            }
            if (collapsed && from.offset === length(this.doc[from.block]) && type.endsWith('Forward')) {
                return handle(() => from.block < this.doc.length - 1 && this.edit(() => this.join(from.block)));
            }
            return;
        }
        // Lists, links and raw HTML have no place in this model.
        event.preventDefault();
    }

    onKeyDown(event) {
        // Safari ends a composition before the keydown of the Enter that commits it, marking that key only as 229.
        if (event.isComposing || event.keyCode === 229 || event.altKey) return;
        const mod = isMac ? event.metaKey : event.ctrlKey;
        // WebKit reports Shift+Enter as a new paragraph, so Enter is handled here; beforeinput covers soft keyboards.
        if (event.key === 'Enter' && !event.ctrlKey && !event.metaKey) {
            event.preventDefault();
            if (this.readOnly) return;
            this.readBack();
            this.selection = this.readSelection() ?? this.selection;
            this.edit(() => event.shiftKey ? this.insertText('\n') : this.splitBlock());
            return;
        }

        if (!mod) return;
        const key = event.key.toLowerCase();
        const mark = event.shiftKey ? (key === 'x' ? 'strikethrough' : null) : SHORTCUTS[key];
        if (mark) {
            event.preventDefault();
            // WebKit can report a selection made just before the key only after the key.
            this.selection = this.readSelection() ?? this.selection;
            this.toggleMark(mark);
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
        const plain = this.plainPaste;
        this.plainPaste = false;
        if (this.readOnly) return;
        this.readBack();
        this.selection = this.readSelection() ?? this.selection;
        this.insertTransfer(event.clipboardData, plain);
    }

    onDrop(event) {
        event.preventDefault();
        const dragged = this.dragged;
        this.dragged = null;
        if (this.readOnly) return;
        const point = document.caretPositionFromPoint?.(event.clientX, event.clientY);
        const range = point ? null : document.caretRangeFromPoint?.(event.clientX, event.clientY);
        const node = point?.offsetNode ?? range?.startContainer;
        if (!node || !this.root.contains(node)) return;
        this.readBack();
        let target = this.positionOf(node, point?.offset ?? range.startOffset);
        const copy = isMac ? event.altKey : event.ctrlKey;
        if (!dragged || copy) {
            this.root.focus();
            this.selection = { anchor: target, head: target };
            this.insertTransfer(event.dataTransfer, false);
            return;
        }

        const { from, to } = dragged;
        if (compare(target, from) >= 0 && compare(target, to) <= 0) return;
        this.edit(() => {
            const moved = this.paragraphsIn(from, to);
            this.deleteRange(from, to);
            if (compare(target, to) > 0) {
                target = target.block === to.block
                    ? { block: from.block, offset: from.offset + target.offset - to.offset }
                    : { block: target.block - (to.block - from.block), offset: target.offset };
            }
            this.selection = { anchor: target, head: target };
            this.insertParagraphs(moved);
        });
        this.root.focus();
        this.writeSelection();
    }

    onCompositionStart() {
        this.composing = true;
        if (this.readOnly) return;
        this.readBack();
        this.selection = this.readSelection() ?? this.selection;
        // A composition over paragraphs would let the browser merge them its own way, so the engine does it first.
        if (this.hasRange() && this.range().from.block !== this.range().to.block) {
            const { from, to } = this.range();
            this.edit(() => this.deleteRange(from, to));
        }
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

    // Paste and drop: HTML keeps its formats, and plain text takes the formats of where it goes.
    insertTransfer(data, plain) {
        const html = plain ? '' : data?.getData('text/html') ?? '';
        const text = data?.getData('text/plain') ?? '';
        const pasted = html ? readBlocks(parseDocument(html), true, this.formats) : null;
        if (pasted?.length === 0 || (!pasted && !text)) return;
        this.edit(() => {
            const { from, to } = this.range();
            const marks = this.storedMarks ?? this.marksForReplacing(from, to);
            this.insertParagraphs(pasted ?? text.split(/\r\n?|\n/)
                .map(line => normalize([{ text: line.replace(/[\t\f\xa0]/g, ' '), marks }])));
        });
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

    paragraphsIn(from, to) {
        const paragraphs = [];
        for (let block = from.block; block <= to.block; block++) {
            paragraphs.push(slice(this.doc[block], block === from.block ? from.offset : 0,
                block === to.block ? to.offset : Infinity));
        }
        return paragraphs;
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
        if (this.readOnly || !this.formats.has(mark)) return;
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

    // History: each step keeps only the paragraphs it replaced, with typing grouped into word-sized steps.

    checkpoint(kind, text) {
        const now = Date.now();
        const last = this.lastEdit;
        const continues = kind === 'type' && last?.kind === 'type' && now - last.time < TYPING_PAUSE
            && !/\s$/.test(last.text) && this.undoStack.length > 0;
        if (!continues) {
            if (this.undoStack.length) this.undoStack.at(-1).before = null;
            // Paragraphs are never changed in place, so a copy of the list is enough to compare against later.
            this.undoStack.push({ before: [...this.doc], selectionBefore: this.selection });
            this.redoStack = [];
        }
        this.lastEdit = { kind, time: now, text };
    }

    // Shrinks the open step to the paragraphs that differ, once its change is made.
    closeStep() {
        const step = this.undoStack.at(-1);
        if (!step?.before) return;
        const before = step.before;
        let start = 0;
        while (start < before.length && start < this.doc.length && before[start] === this.doc[start]) start++;
        let end = 0;
        while (end < before.length - start && end < this.doc.length - start
            && before[before.length - 1 - end] === this.doc[this.doc.length - 1 - end]) end++;
        step.start = start;
        step.removed = before.slice(start, before.length - end);
        step.added = this.doc.slice(start, this.doc.length - end);
        step.selectionAfter = this.selection;
        // Typing that continues the step compares against the same starting point.
        step.before = this.lastEdit?.kind === 'type' ? before : null;
    }

    undo() {
        this.step(this.undoStack, this.redoStack, true);
    }

    redo() {
        this.step(this.redoStack, this.undoStack, false);
    }

    step(from, to, back) {
        if (this.composing || this.readOnly) return;
        this.readBack();
        const entry = from.pop();
        if (!entry?.removed) return;
        const [current, restored] = back ? [entry.added, entry.removed] : [entry.removed, entry.added];
        this.doc.splice(entry.start, current.length, ...restored);
        if (!this.doc.length) this.doc = [[]];
        to.push(entry);
        this.selection = back ? entry.selectionBefore : entry.selectionAfter;
        this.storedMarks = null;
        this.lastEdit = null;
        this.render();
        this.writeSelection();
        this.changed(false);
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
        this.root.toggleAttribute('data-empty', this.doc.length === 1 && !this.doc[0].length);
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
            return { block: last, offset: length(readBlocks(this.root.children[last], false, this.formats)[0] ?? []) };
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

    hasRange() {
        return !!this.selection && compare(this.selection.anchor, this.selection.head) !== 0;
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
        for (const button of this.toolbar?.querySelectorAll('[data-format]') ?? []) {
            const state = this.markState(button.dataset.format);
            button.setAttribute('aria-pressed', state === 'mixed' ? 'mixed' : String(state === 'on'));
        }
    }

    // The value goes to .NET as the field asks: when focus leaves, or on each change after its delay.

    changed(edited = true) {
        if (edited) this.closeStep();
        this.updateToolbar();
        clearTimeout(this.sendTimer);
        if (this.updateOnInput) this.sendTimer = setTimeout(() => this.send(), this.updateDelay);
    }

    send() {
        clearTimeout(this.sendTimer);
        const html = this.value();
        if (html === this.lastSent) return;
        this.lastSent = html;
        // Each part's handler finishes before the next browser event, so a click right after leaving sees the value.
        for (let start = 0; ;) {
            let end = Math.min(start + MAX_PART, html.length);
            // A part never ends between the two halves of a surrogate pair, which JSON can't carry apart.
            if (end < html.length && /[\ud800-\udbff]/.test(html[end - 1])) end--;
            const continues = end < html.length;
            this.root.dispatchEvent(new CustomEvent('tb-rich-text-change',
                { bubbles: true, detail: { html: html.slice(start, end), continues } }));
            if (!continues) return;
            start = end;
        }
    }
}

// Paragraph helpers.

const sameMarks = (a, b) => a.length === b.length && a.every((mark, i) => mark === b[i]);
const sameRuns = (a, b) => a.length === b.length
    && a.every((run, i) => run.text === b[i].text && sameMarks(run.marks, b[i].marks));
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
        case ' ': {
            const collapses = i === 0 || i === text.length - 1 || ' \n'.includes(text[i - 1]) || text[i + 1] === '\n';
            return forValue && collapses ? '&nbsp;' : ' ';
        }
        default: return character;
    }
}

// HTML in: as RichTextDocument reads it, with the browser's own parser building the tree. Blocks become paragraphs,
// the allowed marks survive, and everything else becomes plain text.

// DOMParser's documents run no script and load nothing.
const parseDocument = html => new DOMParser().parseFromString(html, 'text/html').body;

function parseHtml(html, formats) {
    const doc = readBlocks(parseDocument(html ?? ''), true, formats);
    return doc.length ? doc : [[]];
}

function readBlocks(container, collapse, formats) {
    const paragraphs = [];
    let runs = null;
    const finish = () => {
        if (runs) {
            const hasBreak = runs.some(run => run.text.includes('\n'));
            const paragraph = finishParagraph(runs, collapse);
            // A line break shows a line even with nothing on it, such as a <br> between two paragraphs.
            if (!collapse || paragraph.length || hasBreak) paragraphs.push(paragraph);
        }
        runs = null;
    };
    const walk = (node, marks, pre) => {
        for (const child of node.childNodes) {
            if (child.nodeType === Node.TEXT_NODE) {
                const text = collapse && !pre ? child.data.replace(/[\t\n\r\f ]+/g, ' ') : child.data;
                (runs ??= []).push({ text, marks: marks.filter(mark => formats.has(mark)), pre });
            } else if (child.nodeType === Node.ELEMENT_NODE && !SKIPPED.has(child.localName)
                && !isWordListMarker(child)) {
                if (child.localName === 'br') {
                    (runs ??= []).push({ text: '\n', marks: marks.filter(mark => formats.has(mark)), pre });
                    continue;
                }
                // A rule between two lines leaves them separate paragraphs.
                if (child.localName === 'hr') {
                    finish();
                    continue;
                }
                const childMarks = marksOf(child, marks);
                const childPre = pre || PREFORMATTED.has(child.localName);
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
        // As HTML renders it outside <pre>: no space after another or at a line's start, and none at a line's end.
        runs = keepCharacters(runs, (text, i, pre) => pre || text[i] !== ' '
            || (i > 0 && !' \n'.includes(text[i - 1])));
        runs = keepCharacters(runs, (text, i, pre) => pre || text[i] !== ' '
            || (i < text.length - 1 && text[i + 1] !== '\n'));
    }
    // A paragraph's last line break shows nothing.
    const text = textOf(runs);
    const trimmed = text.endsWith('\n') ? slice(runs, 0, text.length - 1) : runs;
    // The model holds plain spaces only, so the value it writes parses back to the same text.
    return normalize(trimmed.map(run => ({ text: run.text.replace(/[\t\f\r\xa0]/g, ' '), marks: run.marks })));
}

function keepCharacters(runs, keep) {
    const text = textOf(runs);
    let offset = 0;
    return runs.map(run => {
        let kept = '';
        for (let i = 0; i < run.text.length; i++) {
            if (keep(text, offset + i, run.pre)) kept += run.text[i];
        }
        offset += run.text.length;
        return { text: kept, marks: run.marks, pre: run.pre };
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
