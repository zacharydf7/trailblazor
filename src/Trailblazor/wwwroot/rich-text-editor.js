import { isComposing } from '../Trailblazor.Primitives/composing.js';
// The rich text editor's engine. The browser types, composes and spellchecks natively and the engine reads the result
// back; every other edit the engine makes on its own model and renders. It never uses document.execCommand.
// A paragraph is { kind, level, runs }, never changed in place; a run is { text, marks in MARKS order, link or null };
// '\n' is a line break, and offsets count UTF-16 code units.

const MARKS = ['bold', 'italic', 'underline', 'strikethrough', 'subscript', 'superscript', 'code'];
const MARK_TAGS = {
    bold: 'strong', italic: 'em', underline: 'u', strikethrough: 's', subscript: 'sub', superscript: 'sup',
    code: 'code',
};
const TAG_MARKS = {
    b: 'bold', strong: 'bold', i: 'italic', em: 'italic', u: 'underline', ins: 'underline',
    s: 'strikethrough', strike: 'strikethrough', del: 'strikethrough', sub: 'subscript', sup: 'superscript',
    code: 'code', kbd: 'code', samp: 'code', tt: 'code',
};
const FORMAT_INPUTS = {
    formatBold: 'bold', formatItalic: 'italic', formatUnderline: 'underline', formatStrikeThrough: 'strikethrough',
    formatSubscript: 'subscript', formatSuperscript: 'superscript',
};
// Ctrl or Cmd with these keys, as in word processors; strikethrough is Ctrl+Shift+X, as in most web editors.
const SHORTCUTS = { b: 'bold', i: 'italic', u: 'underline', ',': 'subscript', '.': 'superscript', e: 'code' };
// Ctrl+Alt with a digit and Ctrl+Shift with 7, 8 or 9, by key position, as in Google Docs; Cmd on a Mac.
const ALT_BLOCK_KEYS = { Digit0: 'paragraph', Digit1: 'heading1', Digit2: 'heading2', Digit3: 'heading3' };
const SHIFT_BLOCK_KEYS = { Digit7: 'number', Digit8: 'bullet', Digit9: 'quote' };
// Typed at the start of a plain paragraph and followed by a space, these start a block, as in Markdown.
const MARKDOWN_STARTS = new Map([['#', 'heading1'], ['##', 'heading2'], ['###', 'heading3'], ['>', 'quote'],
    ['-', 'bullet'], ['*', 'bullet'], ['1.', 'number']]);
// Which way each key moves the caret, in left-to-right text.
const CARET_MOVES = { ArrowLeft: -1, ArrowUp: -1, Home: -1, PageUp: -1, ArrowRight: 1, ArrowDown: 1, End: 1, PageDown: 1 };
const KINDS = ['paragraph', 'heading1', 'heading2', 'heading3', 'quote', 'bullet', 'number', 'rule'];
const HEADING_TAGS = { heading1: 'h1', heading2: 'h2', heading3: 'h3' };
const BLOCKS = new Set(['address', 'article', 'aside', 'blockquote', 'caption', 'center', 'dd', 'details', 'dialog',
    'dir', 'div', 'dl', 'dt', 'fieldset', 'figcaption', 'figure', 'footer', 'form', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
    'header', 'hgroup', 'legend', 'li', 'listing', 'main', 'menu', 'nav', 'ol', 'p', 'plaintext', 'pre', 'search',
    'section', 'summary', 'table', 'td', 'th', 'tr', 'ul', 'xmp']);
const SKIPPED = new Set(['head', 'iframe', 'math', 'noembed', 'noframes', 'noscript', 'object', 'script', 'select',
    'style', 'svg', 'template', 'textarea', 'title']);
const PREFORMATTED = new Set(['pre', 'listing', 'xmp', 'plaintext']);
const LISTS = new Set(['ul', 'ol', 'menu', 'dir']);
// The elements that hold one paragraph's own text in the editor, and rules, which hold none.
const BLOCK_ELEMENTS = 'p, h1, h2, h3, li, hr';
const TYPING_PAUSE = 1000;
// Keeps each part under Blazor Server's default 32 KB message limit, even at 6 bytes of JSON per control character.
const MAX_PART = 5000;
const isMac = /Mac|iPhone|iPad/.test(navigator.userAgentData?.platform ?? navigator.platform);

// options: { formats, blocks, links, transformsPaste, readOnly, updateOnInput, updateDelay }, with formats as mark
// names, blocks as kinds and links as URL schemes split by spaces, links null where links aren't allowed, and
// updateDelay in milliseconds.
export function attachRichTextEditor(root, value, options) {
    const toolbar = root.closest('.tb-rich-text-editor')?.querySelector('[data-part="toolbar"]') ?? null;
    const label = document.getElementById(root.getAttribute('aria-labelledby') ?? '');
    const editor = new Editor(root, toolbar, label, value, options);
    root.dataset.ready = '';
    return {
        setValue: html => editor.setValue(html),
        setOptions: changed => editor.setOptions(changed),
        setLink: (url, text) => editor.setLink(url, text),
        insertPasted: html => editor.insertPasted(html),
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
        this.tabLeaves = false;
        this.caretMove = 0;
        this.linkTarget = null;
        this.restoreSelection = false;
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
        on(root, 'pointerdown', () => this.caretMove = 0);
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
            this.tabLeaves = false;
            if (!root.contains(event.relatedTarget) && !toolbar?.contains(event.relatedTarget)) this.send();
        });
        // Focus coming back from the link dialog brings back the selection it was opened on.
        on(root, 'focus', () => {
            if (!this.restoreSelection) return;
            this.restoreSelection = false;
            this.writeSelection();
        });
        // A click on a link places the caret and mustn't follow it, which Blazor's router would do for a link in the
        // app; Ctrl+click (Cmd+click on a Mac) opens it in a new tab, as word processors do.
        on(root, 'click', event => {
            const anchor = event.target.closest?.('a[href]');
            if (!anchor || !root.contains(anchor)) return;
            event.preventDefault();
            if (isMac ? event.metaKey : event.ctrlKey) window.open(anchor.href, '_blank', 'noopener');
        });
        on(document, 'selectionchange', () => this.onSelectionChange());
        // The label names the editor without a for attribute, so a click on it doesn't focus the editor by itself.
        on(label, 'click', () => {
            if (root.getAttribute('aria-disabled') !== 'true') root.focus();
        });
        // Pressing a toolbar button must not take the selection out of the editor.
        on(toolbar, 'mousedown', event => {
            if (event.target.closest('[data-format], [data-block], [data-command]')) event.preventDefault();
        });
        on(toolbar, 'click', event => {
            const button = event.target.closest('[data-format], [data-block], [data-command]');
            if (!button || button.disabled) return;
            // The editor takes focus first, so the link dialog hands it back to the editor when it closes.
            root.focus();
            if (button.dataset.format) this.toggleMark(button.dataset.format);
            else if (button.dataset.block) this.setBlock(button.dataset.block);
            else if (button.dataset.command === 'link') this.requestLink();
            else if (button.dataset.command === 'rule') this.insertRule();
            else if (button.dataset.command === 'clear') this.clearFormatting();
            else this.indent(button.dataset.command === 'indent' ? 1 : -1);
            this.writeSelection();
        });

        this.load(value);
        this.observer.observe(root, { childList: true, characterData: true, subtree: true });
    }

    setOptions(options) {
        this.schema = {
            formats: new Set(options.formats.split(' ').filter(mark => MARKS.includes(mark))),
            blocks: new Set(options.blocks.split(' ').filter(kind => KINDS.includes(kind))),
            links: options.links === null ? null : options.links.toLowerCase().split(' ').filter(Boolean),
        };
        this.transformsPaste = !!options.transformsPaste;
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
        this.doc = parseHtml(html, this.schema);
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
            const block = this.blockOf(record.target);
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
        const elements = this.blockElements();
        const readAll = this.pendingAll || elements.length !== this.doc.length
            || pending.some(([block]) => !elements[block] || this.readOwn(elements[block]).length > 1);
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
            // The browser changed the paragraphs themselves, so the DOM's own marks and blocks are taken as they are.
            this.checkpoint('type', '');
            this.doc = this.readDocument();
        } else {
            for (const [block, nodes] of pending) {
                const read = this.readOwn(elements[block])[0]?.runs ?? [];
                // Changes in several text nodes at once can't be told apart, so their marks come from the DOM.
                if (nodes.size > 1) {
                    if (textOf(read) !== textOf(this.doc[block].runs) || !sameRuns(read, this.doc[block].runs)) {
                        this.checkpoint('type', '');
                        this.doc[block] = { ...this.doc[block], runs: read };
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
        return withText(normalizeLevels(readBlocks(this.root, false, this.schema)));
    }

    // A list item's own text, without the lists nested inside it.
    readOwn(element) {
        const own = element.cloneNode(true);
        for (const nested of own.querySelectorAll(':scope > ul, :scope > ol')) nested.remove();
        return readBlocks(own, false, this.schema);
    }

    // Applies the browser's change to one paragraph's text; the engine, not the DOM, decides the new text's marks.
    readText(block, after, caret) {
        const runs = this.doc[block].runs;
        const before = textOf(runs);
        if (before === after) return;
        const { start, endBefore, endAfter } = diff(before, after, caret);
        const inserted = after.slice(start, endAfter);
        const replaced = endBefore > start ? slice(runs, start, start + 1)[0] : null;
        const marks = this.storedMarks ?? replaced?.marks ?? marksAt(runs, start);
        const link = replaced ? replaced.link : linkInside(runs, start);
        this.checkpoint('type', inserted);
        this.doc[block] = {
            ...this.doc[block], runs: replace(runs, start, endBefore, [{ text: inserted, marks, link }]),
        };
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
        if (type === 'formatIndent') return handle(() => this.indent(1));
        if (type === 'formatOutdent') return handle(() => this.indent(-1));
        if (type === 'historyUndo') return handle(() => this.undo());
        if (type === 'historyRedo') return handle(() => this.redo());

        this.readBack();
        this.selection = this.readSelection() ?? this.selection;
        const { from, to } = this.range();
        const collapsed = compare(from, to) === 0;
        const spans = from.block !== to.block;

        if (type === 'insertParagraph') return handle(() => this.edit(() => this.splitBlock()));
        if (type === 'insertLineBreak') return handle(() => this.edit(() => this.insertText('\n')));
        const start = type === 'insertText' && event.data === ' ' && collapsed ? this.markdownStart(from) : null;
        if (start) return handle(() => this.startBlock(from, start));
        // The browser types at its own caret, which can still be beside a rule before selectionchange moves it.
        if (type === 'insertText' && (spans || this.browserCaretBesideRule())) {
            return handle(() => this.edit(() => this.insertText(event.data ?? '')));
        }
        if (type === 'insertText' || type === 'insertReplacementText') return;
        // Drops are handled whole in onDrop.
        if (type === 'deleteByDrag' || type === 'insertFromDrop') return handle(() => {});
        if (type.startsWith('delete')) {
            if (spans) return handle(() => this.edit(() => this.deleteRange(from, to)));
            if (collapsed && from.offset === 0 && type.endsWith('Backward')) {
                return handle(() => this.backspaceAtStart(from.block));
            }
            if (collapsed && from.offset === length(this.doc[from.block].runs) && type.endsWith('Forward')) {
                return handle(() => from.block < this.doc.length - 1 && this.edit(() => this.join(from.block)));
            }
            return;
        }
        // Other inputs, such as the browser's own links or raw HTML, have no place in this model.
        event.preventDefault();
    }

    onKeyDown(event) {
        if (isComposing(event)) return;
        const move = CARET_MOVES[event.key];
        if (move) {
            const flips = ['ArrowLeft', 'ArrowRight'].includes(event.key)
                && getComputedStyle(this.root).direction === 'rtl';
            this.caretMove = flips ? -move : move;
        }
        // In a list item, Escape lets the next Tab leave the editor, so a keyboard user is never held in it.
        // The first Escape is marked defaultPrevented so a surrounding dialog stays open; a second one closes it.
        if (event.key === 'Escape') {
            if (!this.tabLeaves && this.caretInListItem()) {
                event.preventDefault();
                this.tabLeaves = true;
            }
            return;
        }

        if (event.key !== 'Tab' && event.key !== 'Shift') this.tabLeaves = false;
        const mod = isMac ? event.metaKey : event.ctrlKey;
        // Many keyboards type characters with AltGr, which Windows reports as Ctrl+Alt.
        const altGraph = !isMac && event.getModifierState('AltGraph');
        const kind = mod && event.altKey && !event.shiftKey && !altGraph ? ALT_BLOCK_KEYS[event.code]
            : mod && event.shiftKey && !event.altKey ? SHIFT_BLOCK_KEYS[event.code] : undefined;
        if (kind) {
            event.preventDefault();
            this.selection = this.readSelection() ?? this.selection;
            this.setBlock(kind);
            return;
        }

        if (event.altKey) return;
        if (event.key === 'Tab' && !event.ctrlKey && !event.metaKey) {
            if (this.tabLeaves || !this.caretInListItem()) return;
            event.preventDefault();
            this.indent(event.shiftKey ? -1 : 1);
            return;
        }

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
        } else if (key === '\\' && !event.shiftKey) {
            event.preventDefault();
            this.selection = this.readSelection() ?? this.selection;
            this.clearFormatting();
        } else if (key === 'k' && !event.shiftKey && !this.readOnly && this.schema.links !== null) {
            event.preventDefault();
            this.requestLink();
        } else if (key === 'v' && event.shiftKey) {
            this.plainPaste = true;
        }
    }

    caretInListItem() {
        this.readBack();
        this.selection = this.readSelection() ?? this.selection;
        return !this.readOnly && isListItem(this.doc[this.range().from.block].kind);
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
        if (this.doc[target.block].kind === 'rule') target = this.outOfRule(target, false);
        const copy = isMac ? event.altKey : event.ctrlKey;
        if (!dragged || copy) {
            this.root.focus();
            this.selection = { anchor: target, head: target };
            // Move the page's caret too, or the selectionchange from focus() would undo it before a paste transform
            // answers.
            this.writeSelection();
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
        // If readSelection moved the caret off a rule, move it on the page too.
        if (this.browserCaretBesideRule()) this.writeSelection();
        this.updateToolbar();
    }

    browserCaretBesideRule() {
        const dom = document.getSelection();
        return dom.isCollapsed && this.root.contains(dom.focusNode) && this.besideRule(dom.focusNode, dom.focusOffset);
    }

    // Whether a point is on a rule or between paragraphs next to one, where browsers put the caret crossing a rule.
    besideRule(node, offset) {
        if ((node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement)?.localName === 'hr') return true;
        if (this.blockOf(node) >= 0) return false;
        const position = this.positionOf(node, offset);
        return this.doc[position.block]?.kind === 'rule'
            || (position.offset === 0 && this.doc[position.block - 1]?.kind === 'rule');
    }

    // Paste and drop: HTML keeps its formats and blocks, and plain text takes those of where it goes.
    insertTransfer(data, plain) {
        const html = plain ? '' : data?.getData('text/html') ?? '';
        const text = data?.getData('text/plain') ?? '';
        const pasted = html ? readBlocks(parseDocument(html), true, this.schema) : null;
        if (pasted?.length === 0 || (!pasted && !text)) return;
        const { from, to } = this.range();
        const marks = this.storedMarks ?? this.marksForReplacing(from, to);
        const link = this.linkForReplacing(from, to);
        const paragraphs = pasted ?? text.split(/\r\n?|\n/)
            .map(line => paragraph(normalize([{ text: line.replace(/[\t\f\xa0]/g, ' '), marks, link }])));
        if (this.transformsPaste) this.dispatchInParts('tb-rich-text-paste', toHtml(paragraphs));
        else this.edit(() => this.insertParagraphs(paragraphs));
    }

    // What the app's paste transform returned goes in at the caret.
    insertPasted(html) {
        const pasted = readBlocks(parseDocument(html), true, this.schema);
        if (this.readOnly || !pasted.length) return;
        this.readBack();
        this.edit(() => this.insertParagraphs(pasted));
    }

    // Model edits.

    edit(change) {
        this.checkpoint('edit', '');
        change();
        this.doc = normalizeLevels(this.doc);
        this.render();
        this.writeSelection();
        this.changed();
    }

    deleteRange(from, to) {
        const merged = normalize([
            ...slice(this.doc[from.block].runs, 0, from.offset), ...slice(this.doc[to.block].runs, to.offset)]);
        // A range that starts on a rule takes the rule with it: what's left takes the kind of the block it ends in, or
        // is plain.
        const kept = this.doc[from.block].kind !== 'rule' ? this.doc[from.block]
            : this.doc[to.block].kind !== 'rule' ? this.doc[to.block] : paragraph([]);
        this.doc.splice(from.block, to.block - from.block + 1, { ...kept, runs: merged });
        this.selection = { anchor: from, head: from };
    }

    paragraphsIn(from, to) {
        const paragraphs = [];
        for (let block = from.block; block <= to.block; block++) {
            paragraphs.push({ ...this.doc[block], runs: slice(this.doc[block].runs,
                block === from.block ? from.offset : 0, block === to.block ? to.offset : Infinity) });
        }
        return paragraphs;
    }

    insertText(text) {
        const { from, to } = this.range();
        const marks = this.storedMarks ?? this.marksForReplacing(from, to);
        const link = this.linkForReplacing(from, to);
        this.deleteRange(from, to);
        const target = this.doc[from.block];
        this.doc[from.block] = {
            ...target, runs: replace(target.runs, from.offset, from.offset, [{ text, marks, link }]),
        };
        const caret = { block: from.block, offset: from.offset + text.length };
        this.selection = { anchor: caret, head: caret };
        this.storedMarks = null;
    }

    // Pasted plain paragraphs take the kind of the one at the caret, and so does the first unless the caret starts it.
    // A pasted rule leaves the text around it in paragraphs of their own. A rule pasted last gets one after it for the
    // caret (plain at a heading's end), and one pasted first at the document's start gets one before it.
    insertParagraphs(paragraphs) {
        const { from, to } = this.range();
        this.deleteRange(from, to);
        const target = this.doc[from.block];
        const before = slice(target.runs, 0, from.offset);
        const after = slice(target.runs, from.offset);
        const last = paragraphs.length - 1;
        const blocks = paragraphs.flatMap((pasted, i) => {
            const head = i === 0 ? before : [];
            const tail = i === last ? after : [];
            if (pasted.kind === 'rule') {
                return [...(head.length || (i === 0 && from.block === 0) ? [{ ...target, runs: head }] : []), pasted,
                    ...(i === last ? [HEADING_TAGS[target.kind] && !tail.length ? paragraph([])
                        : { ...target, runs: tail }] : [])];
            }
            const kind = (i === 0 && from.offset > 0) || pasted.kind === 'paragraph' ? target : pasted;
            return [{ kind: kind.kind, level: kind.level, runs: normalize([...head, ...pasted.runs, ...tail]) }];
        });
        this.doc.splice(from.block, 1, ...blocks);
        const end = from.block + blocks.length - 1;
        const caret = { block: end, offset: length(blocks.at(-1).runs) - length(after) };
        this.selection = { anchor: caret, head: caret };
        this.storedMarks = null;
    }

    // Enter moves an empty list item or quote out a level, and a heading split at its end goes on as a paragraph.
    splitBlock() {
        const { from, to } = this.range();
        const marks = this.storedMarks ?? this.marksForReplacing(from, to);
        this.deleteRange(from, to);
        const target = this.doc[from.block];
        if (!target.runs.length && target.kind !== 'paragraph' && !HEADING_TAGS[target.kind]) {
            this.doc[from.block] = outdented(target);
            return;
        }

        // A plain paragraph of "---" becomes a rule, as in Markdown.
        if (target.kind === 'paragraph' && textOf(target.runs) === '---' && this.schema.blocks.has('rule')) {
            this.doc[from.block] = paragraph([]);
            this.selection = { anchor: { block: from.block, offset: 0 }, head: { block: from.block, offset: 0 } };
            this.insertParagraphs([paragraph([], 'rule')]);
            return;
        }

        const after = slice(target.runs, from.offset);
        const next = HEADING_TAGS[target.kind] && !after.length ? paragraph([]) : { ...target, runs: after };
        this.doc.splice(from.block, 1, { ...target, runs: slice(target.runs, 0, from.offset) }, next);
        const caret = { block: from.block + 1, offset: 0 };
        this.selection = { anchor: caret, head: caret };
        // A new empty paragraph keeps the formatting the caret had, as word processors do.
        this.storedMarks = after.length ? null : marks;
    }

    // Backspace at the start of a list item or quote takes its list or quote away first, as word processors do.
    backspaceAtStart(block) {
        const target = this.doc[block];
        if (target.kind === 'quote' || isListItem(target.kind)) {
            this.edit(() => this.doc[block] = outdented(target));
        } else if (block > 0 && this.doc[block - 1].kind === 'rule') {
            this.edit(() => {
                this.doc.splice(block - 1, 1);
                this.selection = { anchor: { block: block - 1, offset: 0 }, head: { block: block - 1, offset: 0 } };
            });
        } else if (block > 0) {
            this.edit(() => this.join(block - 1));
        }
    }

    join(block) {
        const offset = length(this.doc[block].runs);
        const runs = normalize([...this.doc[block].runs, ...this.doc[block + 1].runs]);
        this.doc.splice(block, 2, { ...this.doc[block], runs });
        this.selection = { anchor: { block, offset }, head: { block, offset } };
    }

    toggleMark(mark) {
        if (this.readOnly || !this.schema.formats.has(mark)) return;
        this.readBack();
        const { from, to } = this.range();
        if (compare(from, to) === 0) {
            const current = this.storedMarks ?? marksAt(this.doc[from.block].runs, from.offset);
            this.storedMarks = current.includes(mark) ? current.filter(m => m !== mark) : withMark(current, mark);
            this.updateToolbar();
            return;
        }
        const add = this.markState(mark) !== 'on';
        this.edit(() => {
            for (let block = from.block; block <= to.block; block++) {
                const start = block === from.block ? from.offset : 0;
                const end = block === to.block ? to.offset : Infinity;
                this.doc[block] = { ...this.doc[block], runs: mapRuns(this.doc[block].runs, start, end,
                    run => ({ ...run, marks: add ? withMark(run.marks, mark) : run.marks.filter(m => m !== mark) })) };
            }
        });
    }

    markState(mark) {
        const { from, to } = this.range();
        if (compare(from, to) === 0) {
            return (this.storedMarks ?? marksAt(this.doc[from.block].runs, from.offset)).includes(mark) ? 'on' : 'off';
        }
        let some = false;
        let all = true;
        for (let block = from.block; block <= to.block; block++) {
            const start = block === from.block ? from.offset : 0;
            const end = block === to.block ? to.offset : Infinity;
            for (const run of slice(this.doc[block].runs, start, end)) {
                if (/^\n*$/.test(run.text)) continue;
                if (run.marks.includes(mark)) some = true;
                else all = false;
            }
        }
        return some ? (all ? 'on' : 'mixed') : 'off';
    }

    // Turns the selected paragraphs into this kind of block, or back into plain paragraphs when all of them are.
    setBlock(kind) {
        if (this.readOnly || (kind !== 'paragraph' && !this.schema.blocks.has(kind))) return;
        this.readBack();
        const { from, to } = this.range();
        const next = this.blockState(kind) === 'on' ? 'paragraph' : kind;
        if (this.doc.slice(from.block, to.block + 1).every(target => [next, 'rule'].includes(target.kind))) return;
        this.edit(() => {
            for (let block = from.block; block <= to.block; block++) {
                const target = this.doc[block];
                if (target.kind === 'rule') continue;
                const level = isListItem(next) && isListItem(target.kind) ? target.level : 0;
                this.doc[block] = { ...target, kind: next, level };
            }
        });
    }

    // The kind of block the text before the caret starts, as heading1 for '#'; null outside a plain paragraph, or for a
    // kind the editor doesn't allow.
    markdownStart({ block, offset }) {
        const target = this.doc[block];
        const kind = target.kind === 'paragraph' ? MARKDOWN_STARTS.get(textOf(target.runs).slice(0, offset)) : null;
        return kind && this.schema.blocks.has(kind) ? kind : null;
    }

    // The space goes in as its own step, so undo brings back the typed text.
    startBlock(from, kind) {
        this.edit(() => this.insertText(' '));
        this.edit(() => {
            this.doc[from.block] = { kind, level: 0, runs: slice(this.doc[from.block].runs, from.offset + 1) };
            const caret = { block: from.block, offset: 0 };
            this.selection = { anchor: caret, head: caret };
        });
    }

    blockState(kind) {
        const { from, to } = this.range();
        const kinds = this.doc.slice(from.block, to.block + 1).map(target => target.kind)
            .filter(each => each !== 'rule');
        if (!kinds.length) return 'off';
        return kinds.every(each => each === kind) ? 'on' : kinds.some(each => each === kind) ? 'mixed' : 'off';
    }

    // A rule goes in at the caret, splitting its paragraph, with the caret in the paragraph after it.
    insertRule() {
        if (this.readOnly || !this.schema.blocks.has('rule')) return;
        this.readBack();
        this.edit(() => this.insertParagraphs([paragraph([], 'rule')]));
    }

    // Takes every format off the selection, or at a caret off the text typed next; links and blocks stay.
    clearFormatting() {
        if (this.readOnly) return;
        this.readBack();
        const { from, to } = this.range();
        if (compare(from, to) === 0) {
            this.storedMarks = [];
            this.updateToolbar();
            return;
        }
        if (!this.paragraphsIn(from, to).some(target => target.runs.some(run => run.marks.length))) return;
        this.edit(() => {
            for (let block = from.block; block <= to.block; block++) {
                const start = block === from.block ? from.offset : 0;
                const end = block === to.block ? to.offset : Infinity;
                this.doc[block] = { ...this.doc[block],
                    runs: mapRuns(this.doc[block].runs, start, end, run => ({ ...run, marks: [] })) };
            }
        });
    }

    // Moves a caret just before the given block off a rule:
    // to the next text's start, or back to the previous text's end.
    outOfRule(position, backward) {
        const after = this.doc.findIndex((target, i) => i >= position.block && target.kind !== 'rule');
        const before = this.doc.findLastIndex((target, i) => i < position.block && target.kind !== 'rule');
        if (before >= 0 && (backward || after < 0)) return { block: before, offset: length(this.doc[before].runs) };
        return after >= 0 ? { block: after, offset: 0 } : position;
    }

    // Moves the selected list items a level in or out; a list item moved out of its outermost list leaves the list.
    indent(change) {
        if (this.readOnly) return;
        this.readBack();
        const { from, to } = this.range();
        const blocks = this.doc.slice(from.block, to.block + 1).map(target => !isListItem(target.kind) ? target
            : change > 0 ? { ...target, level: target.level + 1 } : outdented(target));
        if (normalizeLevels([...this.doc.slice(0, from.block), ...blocks]).slice(from.block)
            .every((target, i) => target.level === this.doc[from.block + i].level
                && target.kind === this.doc[from.block + i].kind)) {
            return;
        }

        this.edit(() => this.doc.splice(from.block, blocks.length, ...blocks));
    }

    // Typing over a selection keeps the formatting of its first character.
    marksForReplacing(from, to) {
        const runs = this.doc[from.block].runs;
        return compare(from, to) < 0 && from.offset < length(runs)
            ? slice(runs, from.offset, from.offset + 1)[0].marks
            : marksAt(runs, from.offset);
    }

    // Typing over a selection keeps its first character's link too, and at a caret, only a link the caret is inside.
    linkForReplacing(from, to) {
        const runs = this.doc[from.block].runs;
        return compare(from, to) < 0 && from.offset < length(runs)
            ? linkAt(runs, from.offset)
            : linkInside(runs, from.offset);
    }

    // Links: the editor asks .NET for its link dialog, and puts in what the dialog returns.

    requestLink() {
        if (this.readOnly || this.schema.links === null) return;
        this.readBack();
        this.selection = this.readSelection() ?? this.selection;
        const target = this.linkRange();
        this.linkTarget = target;
        this.restoreSelection = true;
        this.root.dispatchEvent(new CustomEvent('tb-rich-text-link', {
            bubbles: true, detail: { link: this.linkIn(target), insert: compare(target.from, target.to) === 0 },
        }));
    }

    // The selection, or at a caret in or next to a link, the whole link.
    linkRange() {
        const { from, to } = this.range();
        const runs = this.doc[from.block].runs;
        const link = compare(from, to) !== 0 ? null
            : (from.offset > 0 ? linkAt(runs, from.offset - 1) : null) ?? linkAt(runs, from.offset);
        if (link === null) return { from, to };
        let start = from.offset;
        while (start > 0 && linkAt(runs, start - 1) === link) start--;
        let end = from.offset;
        while (end < length(runs) && linkAt(runs, end) === link) end++;
        return { from: { block: from.block, offset: start }, to: { block: from.block, offset: end } };
    }

    // The link every character of the range has, or null when they differ or have none.
    linkIn({ from, to }) {
        let link;
        for (let block = from.block; block <= to.block; block++) {
            const start = block === from.block ? from.offset : 0;
            const end = block === to.block ? to.offset : Infinity;
            for (const run of slice(this.doc[block].runs, start, end)) {
                if (link === undefined) link = run.link;
                else if (run.link !== link) return null;
            }
        }
        return link ?? null;
    }

    // Puts the dialog's URL on the range it was opened on, or takes the link off with null; at a caret, it inserts the
    // text, or else the URL, as a link.
    setLink(url, text) {
        const target = this.linkTarget;
        this.linkTarget = null;
        this.restoreSelection = false;
        if (!target || this.readOnly || target.to.block >= this.doc.length) return;
        const { from, to } = target;
        const collapsed = compare(from, to) === 0;
        if (collapsed && url === null) return;
        this.root.focus();
        this.edit(() => {
            if (collapsed) {
                const inserted = (text || url).replace(/[\t\n\r\f\xa0]/g, ' ');
                const runs = this.doc[from.block].runs;
                const marks = this.storedMarks ?? marksAt(runs, from.offset);
                this.doc[from.block] = { ...this.doc[from.block],
                    runs: replace(runs, from.offset, from.offset, [{ text: inserted, marks, link: url }]) };
                const caret = { block: from.block, offset: from.offset + inserted.length };
                this.selection = { anchor: caret, head: caret };
                this.storedMarks = null;
                return;
            }
            for (let block = from.block; block <= to.block; block++) {
                const start = block === from.block ? from.offset : 0;
                const end = block === to.block ? to.offset : Infinity;
                this.doc[block] = { ...this.doc[block],
                    runs: mapRuns(this.doc[block].runs, start, end, run => ({ ...run, link: url })) };
            }
            this.selection = { anchor: to, head: to };
        });
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
        if (!this.doc.length) this.doc = [paragraph([])];
        to.push(entry);
        this.selection = back ? entry.selectionBefore : entry.selectionAfter;
        this.storedMarks = null;
        this.lastEdit = null;
        this.render();
        this.writeSelection();
        this.changed(false);
    }

    // Rendering and selection: the editor holds the same lists and quotes as the value, with spaces as typed.

    render() {
        const html = documentHtml(this.doc, false);
        if (this.root.innerHTML !== html) {
            const elements = this.blockElements();
            if (elements.length === this.doc.length) this.doc.forEach((target, i) => updateBlock(elements[i], target));
            // Changed lists or quotes, or anything else the browser left in the editor, mean rewriting it whole.
            // Safe: documentHtml escapes all text and each href, and its only tags are the blocks', marks' and links'.
            if (this.root.innerHTML !== html) this.root.innerHTML = html;
        }
        this.root.toggleAttribute('data-empty',
            this.doc.length === 1 && this.doc[0].kind === 'paragraph' && !this.doc[0].runs.length);
        this.observer.takeRecords();
    }

    blockElements() {
        return [...this.root.querySelectorAll(BLOCK_ELEMENTS)];
    }

    blockOf(node) {
        const element = (node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement)?.closest(BLOCK_ELEMENTS);
        return element && element !== this.root && this.root.contains(element)
            ? this.blockElements().indexOf(element) : -1;
    }

    readSelection() {
        const selection = document.getSelection();
        if (!selection.rangeCount || !this.root.contains(selection.anchorNode)) return null;
        const anchor = this.positionOf(selection.anchorNode, selection.anchorOffset);
        const head = this.positionOf(selection.focusNode, selection.focusOffset);
        if (!selection.isCollapsed || !this.besideRule(selection.focusNode, selection.focusOffset)) {
            return { anchor, head };
        }
        // A caret can't rest on a rule: a key moves it on past the rule, and a point goes to the side it's on.
        const backward = this.caretMove !== 0 ? this.caretMove < 0 : this.doc[head.block]?.kind === 'rule';
        const caret = this.outOfRule(head, backward);
        return { anchor: caret, head: caret };
    }

    positionOf(node, offset) {
        const elements = this.blockElements();
        const block = this.blockOf(node);
        if (block >= 0) return { block, offset: offsetAt(elements[block], node, offset) };
        // A point between paragraphs, in the editor or a list or quote around them, is the start of the next one.
        const point = document.createRange();
        point.setStart(node, Math.min(offset, node.nodeType === Node.TEXT_NODE ? node.data.length
            : node.childNodes.length));
        const next = elements.findIndex(element => point.comparePoint(element, 0) >= 0);
        if (next >= 0) return { block: next, offset: 0 };
        const last = elements.length - 1;
        if (last < 0) return { block: 0, offset: 0 };
        return { block: last, offset: length(this.readOwn(elements[last])[0]?.runs ?? []) };
    }

    writeSelection() {
        if (!this.selection || document.activeElement !== this.root) return;
        const elements = this.blockElements();
        const { anchor, head } = this.selection;
        const [anchorNode, anchorOffset] = pointAt(elements[anchor.block], anchor.offset);
        const [focusNode, focusOffset] = pointAt(elements[head.block], head.offset);
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
        const end = { block: this.doc.length - 1, offset: length(this.doc.at(-1).runs) };
        return { anchor: end, head: end };
    }

    updateToolbar() {
        for (const button of this.toolbar?.querySelectorAll('[data-format], [data-block]') ?? []) {
            const state = button.dataset.format ? this.markState(button.dataset.format)
                : this.blockState(button.dataset.block);
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
        this.dispatchInParts('tb-rich-text-change', html);
    }

    dispatchInParts(name, html) {
        for (let start = 0; ;) {
            let end = Math.min(start + MAX_PART, html.length);
            // A part never ends between the two halves of a surrogate pair, which JSON can't carry apart.
            if (end < html.length && /[\ud800-\udbff]/.test(html[end - 1])) end--;
            const continues = end < html.length;
            this.root.dispatchEvent(new CustomEvent(name,
                { bubbles: true, detail: { html: html.slice(start, end), continues } }));
            if (!continues) return;
            start = end;
        }
    }
}

// Paragraph helpers.

const paragraph = (runs, kind = 'paragraph', level = 0) => ({ kind, level, runs });
const isListItem = kind => kind === 'bullet' || kind === 'number';
const outdented = target => isListItem(target.kind) && target.level > 0
    ? { ...target, level: target.level - 1 } : { ...target, kind: 'paragraph', level: 0 };
const sameMarks = (a, b) => a.length === b.length && a.every((mark, i) => mark === b[i]);
const sameStyle = (a, b) => sameMarks(a.marks, b.marks) && a.link === b.link;
const sameRuns = (a, b) => a.length === b.length && a.every((run, i) => run.text === b[i].text && sameStyle(run, b[i]));
const length = runs => runs.reduce((sum, run) => sum + run.text.length, 0);
const textOf = runs => runs.map(run => run.text).join('');
const compare = (a, b) => a.block - b.block || a.offset - b.offset;

// As RichTextDocument does: a list item nests at most one level deeper than the list item before it, and other
// paragraphs have level 0. Paragraphs that don't change keep their identity, which history compares.
function normalizeLevels(doc) {
    let previous = -1;
    return doc.map(target => {
        const level = isListItem(target.kind) ? Math.min(target.level, previous + 1) : 0;
        previous = isListItem(target.kind) ? level : -1;
        return level === target.level ? target : { ...target, level };
    });
}

function normalize(runs) {
    const result = [];
    for (const run of runs) {
        if (!run.text) continue;
        const last = result.at(-1);
        if (last && sameStyle(last, run)) {
            result[result.length - 1] = { text: last.text + run.text, marks: last.marks, link: last.link };
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
            const text = run.text.slice(sliceStart - start, sliceEnd - start);
            result.push({ text, marks: run.marks, link: run.link });
        }
        start = end;
    }
    return result;
}

const replace = (runs, from, to, inserted) => normalize([...slice(runs, 0, from), ...inserted, ...slice(runs, to)]);

const mapRuns = (runs, from, to, change) => normalize([...slice(runs, 0, from), ...slice(runs, from, to).map(change),
    ...slice(runs, to)]);

// Text typed at an offset takes the marks of the character before it, or of the first at a paragraph's start.
function marksAt(runs, offset) {
    const at = offset > 0 ? offset - 1 : 0;
    return slice(runs, at, at + 1)[0]?.marks ?? [];
}

const linkAt = (runs, offset) => slice(runs, offset, offset + 1)[0]?.link ?? null;

// Text typed at either end of a link stays outside it, as in word processors.
function linkInside(runs, offset) {
    const before = offset > 0 ? linkAt(runs, offset - 1) : null;
    return before !== null && before === linkAt(runs, offset) ? before : null;
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

// HTML out, the same as RichTextDocument.ToHtml writes it. The value and the editor's DOM differ only in spaces: the
// value writes the ones HTML would collapse as &nbsp;.

function toHtml(doc) {
    if (doc.length === 1 && doc[0].kind === 'paragraph' && !doc[0].runs.length) return '';
    return documentHtml(doc, true);
}

// Each list item's <li> stays open for the list nested in it, and neighboring quote paragraphs share a <blockquote>.
function documentHtml(doc, forValue) {
    let html = '';
    const lists = [];
    let inQuote = false;
    const closeLists = depth => {
        while (lists.length > depth) html += `</li></${lists.pop()}>`;
    };
    for (const target of doc) {
        const listItem = isListItem(target.kind);
        closeLists(listItem ? target.level + 1 : 0);
        if (inQuote && target.kind !== 'quote') {
            html += '</blockquote>';
            inQuote = false;
        }
        if (listItem) {
            const tag = target.kind === 'number' ? 'ol' : 'ul';
            if (lists.length === target.level + 1 && lists.at(-1) !== tag) closeLists(target.level);
            if (lists.length === target.level + 1) {
                html += '</li>';
            } else {
                html += `<${tag}>`;
                lists.push(tag);
            }
            html += `<li>${paragraphHtml(target.runs, forValue)}`;
            continue;
        }
        if (target.kind === 'rule') {
            html += '<hr>';
            continue;
        }
        if (target.kind === 'quote' && !inQuote) {
            html += '<blockquote>';
            inQuote = true;
        }
        const tag = HEADING_TAGS[target.kind] ?? 'p';
        html += `<${tag}>${paragraphHtml(target.runs, forValue)}</${tag}>`;
    }
    closeLists(0);
    return inQuote ? html + '</blockquote>' : html;
}

function paragraphHtml(runs, forValue) {
    const text = textOf(runs);
    let html = '';
    let offset = 0;
    let link = null;
    for (const run of runs) {
        // One link stays one <a> across its differently formatted runs, as RichTextDocument writes it.
        if (run.link !== link) {
            if (link !== null) html += '</a>';
            if (run.link !== null) html += `<a href="${run.link.replace(/&/g, '&amp;').replace(/"/g, '&quot;')}">`;
            link = run.link;
        }
        let inner = '';
        for (let i = 0; i < run.text.length; i++) inner += characterHtml(text, offset + i, forValue);
        offset += run.text.length;
        html += run.marks.reduceRight((content, mark) => `<${MARK_TAGS[mark]}>${content}</${MARK_TAGS[mark]}>`, inner);
    }
    if (link !== null) html += '</a>';
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

// Puts a paragraph's new text into its element in place, leaving the lists nested in a list item where they are.
function updateBlock(element, target) {
    if (target.kind === 'rule') return;
    const html = paragraphHtml(target.runs, false);
    const nested = [...element.childNodes].filter(node => LISTS.has(node.localName));
    const own = [...element.childNodes].filter(node => !nested.includes(node));
    const ownHtml = own.map(node => node.nodeType === Node.TEXT_NODE ? escapeText(node.data) : node.outerHTML ?? '')
        .join('');
    if (ownHtml === html && !element.attributes.length) return;
    for (const node of own) node.remove();
    const template = document.createElement('template');
    // Safe: paragraphHtml escapes every character of text and each href, and its only tags are the marks' and links'.
    template.innerHTML = html;
    element.insertBefore(template.content, nested[0] ?? null);
    for (const attribute of [...element.attributes]) element.removeAttribute(attribute.name);
}

const escapeText = text => text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

// HTML in: as RichTextDocument reads it, with the browser's own parser building the tree. The allowed blocks and marks
// survive, other blocks become paragraphs, and everything else becomes plain text.

// DOMParser's documents run no script and load nothing.
const parseDocument = html => new DOMParser().parseFromString(html, 'text/html').body;

function parseHtml(html, schema) {
    return withText(normalizeLevels(readBlocks(parseDocument(html ?? ''), true, schema)));
}

// The caret needs a paragraph on each side of a rule, so a document that starts or ends with one, or is empty, gets
// an empty paragraph there.
function withText(doc) {
    if (!doc.length) return [paragraph([])];
    const head = doc[0].kind === 'rule' ? [paragraph([])] : [];
    const tail = doc.at(-1).kind === 'rule' ? [paragraph([])] : [];
    return head.length || tail.length ? [...head, ...doc, ...tail] : doc;
}

// A block context is what an element puts its text in: its own allowed heading, quote, list item or Word list
// paragraph, else its parent's, with the lists around it counted.
function readBlocks(container, collapse, { formats, blocks, links }) {
    const paragraphs = [];
    let runs = null;
    let block = null;
    // Whether the last Word list marker was a number or letters, which makes its list numbered.
    let numbered = false;
    const resolve = context => {
        if (!context.word) return { kind: context.kind, level: context.level };
        const kind = numbered ? 'number' : 'bullet';
        return blocks.has(kind) ? { kind, level: context.level } : { kind: 'paragraph', level: 0 };
    };
    const start = context => {
        if (runs) return;
        runs = [];
        block = resolve(context);
        numbered = false;
    };
    const finish = () => {
        if (runs) {
            const hasBreak = runs.some(run => run.text.includes('\n'));
            const finished = finishParagraph(runs, collapse);
            // A line break shows a line even with nothing on it, such as a <br> between two paragraphs.
            if (!collapse || finished.length || hasBreak) paragraphs.push(paragraph(finished, block.kind, block.level));
        }
        runs = null;
    };
    const contextOf = (element, parent) => {
        const name = element.localName;
        const context = LISTS.has(name) ? { ...parent, lists: parent.lists + 1, ordered: name === 'ol' } : parent;
        const level = /mso-list:\s*l\d+\s+level(\d{1,2})/i.exec(element.getAttribute('style') ?? '');
        const own = name === 'h1' ? ['heading1', 0, false] : name === 'h2' ? ['heading2', 0, false]
            : /^h[3-6]$/.test(name) ? ['heading3', 0, false]
            : name === 'blockquote' ? ['quote', 0, false]
            : name === 'li' ? [parent.ordered ? 'number' : 'bullet', Math.max(parent.lists - 1, 0), false]
            : level ? ['bullet', Math.max(Number(level[1]) - 1, 0), true] : null;
        if (!own || !(own[2] ? blocks.has('bullet') || blocks.has('number') : blocks.has(own[0]))) return context;
        return { ...context, kind: own[0], level: own[1], word: own[2] };
    };
    const walk = (node, marks, pre, link, context) => {
        for (const child of node.childNodes) {
            if (child.nodeType === Node.TEXT_NODE) {
                const text = collapse && !pre ? child.data.replace(/[\t\n\r\f ]+/g, ' ') : child.data;
                start(context);
                runs.push({ text, marks: marks.filter(mark => formats.has(mark)), link: link.kept, pre });
            } else if (child.nodeType === Node.ELEMENT_NODE && isWordListMarker(child)) {
                numbered = /^\(?([0-9]+|[a-z]+|[A-Z]+)[.)]$/.test(child.textContent.trim());
            } else if (child.nodeType === Node.ELEMENT_NODE && !SKIPPED.has(child.localName)) {
                if (child.localName === 'br') {
                    start(context);
                    runs.push({ text: '\n', marks: marks.filter(mark => formats.has(mark)), link: link.kept, pre });
                    continue;
                }
                // A rule between two lines leaves them separate paragraphs, with the rule between if it's allowed.
                if (child.localName === 'hr') {
                    finish();
                    if (blocks.has('rule')) paragraphs.push(paragraph([], 'rule'));
                    continue;
                }
                const childLink = child.localName === 'a' ? linkStateOf(child, links) : link;
                const childMarks = marksOf(child, marks, childLink.target !== null);
                const childPre = pre || PREFORMATTED.has(child.localName);
                const childContext = contextOf(child, context);
                if (!BLOCKS.has(child.localName)) {
                    walk(child, childMarks, childPre, childLink, childContext);
                    continue;
                }
                finish();
                const first = paragraphs.length;
                walk(child, childMarks, childPre, childLink, childContext);
                finish();
                if (paragraphs.length === first) {
                    const empty = resolve(childContext);
                    numbered = false;
                    paragraphs.push(paragraph([], empty.kind, empty.level));
                }
            }
        }
    };
    walk(container, [], false, { target: null, kept: null },
        { kind: 'paragraph', level: 0, word: false, lists: 0, ordered: false });
    finish();
    return paragraphs;
}

// An <a> starts a link whose target is its href as a browser reads it: tabs and line breaks inside are dropped, as are
// spaces and controls around it. The text inside keeps the link only if the schema allows it, as in RichTextSchema.
// Text inside an <a> without an href has no link.
function linkStateOf(element, links) {
    const href = (element.getAttribute('href') ?? '').replace(/[\t\n\r]/g, '');
    const target = href.replace(/^[\x00-\x20]+|[\x00-\x20]+$/g, '') || null;
    return { target, kept: target !== null && links !== null && allowsLink(target, links) ? target : null };
}

// A relative URL, which has no scheme, is always allowed; a listed scheme is, unless following the link runs script.
function allowsLink(url, schemes) {
    const scheme = schemeOf(url);
    return scheme === null || (schemes.includes(scheme) && scheme !== 'javascript' && scheme !== 'vbscript');
}

// Null for a relative URL (no ':' before any '/', '?' or '#'); '' when what precedes the ':' isn't a valid scheme.
function schemeOf(url) {
    const colon = url.indexOf(':');
    const path = url.search(/[/?#]/);
    if (colon < 0 || (path >= 0 && path < colon)) return null;
    const scheme = url.slice(0, colon);
    return /^[a-z][a-z0-9+.-]*$/i.test(scheme) ? scheme.toLowerCase() : '';
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
    return normalize(trimmed.map(run => ({ text: run.text.replace(/[\t\f\r\xa0]/g, ' '), marks: run.marks,
        link: run.link })));
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
        return { text: kept, marks: run.marks, link: run.link, pre: run.pre };
    });
}

// Word writes a list item's bullet or number as text, in a span styled mso-list: Ignore.
const isWordListMarker = element => /mso-list:\s*ignore/i.test(element.getAttribute('style') ?? '');

// Google Docs and Word have no code format; people set code in a monospace font there, so that reads as code.
const isMonospace = families => families.split(',')
    .map(family => family.trim().replace(/^["']|["']$/g, '').toLowerCase())
    .some(family => ['monospace', 'courier new', 'courier', 'consolas', 'menlo', 'monaco'].includes(family));

// Inside a link, an underline from a style is the link's own look, as Google Docs writes it, not a format.
function marksOf(element, inherited, inLink) {
    let marks = inherited;
    const add = mark => marks = withMark(marks, mark);
    const remove = (...names) => marks = marks.filter(mark => !names.includes(mark));
    if (TAG_MARKS[element.localName]) add(TAG_MARKS[element.localName]);
    const style = element.style;
    if (style) {
        const weight = style.fontWeight;
        if (weight === 'bold' || weight === 'bolder' || Number(weight) >= 600) add('bold');
        else if (weight === 'normal' || weight === 'lighter' || (weight && Number(weight) < 600)) remove('bold');
        if (style.fontStyle === 'italic' || style.fontStyle.startsWith('oblique')) add('italic');
        else if (style.fontStyle === 'normal') remove('italic');
        const decoration = `${style.textDecorationLine} ${style.textDecoration}`;
        if (!inLink && decoration.includes('underline')) add('underline');
        if (decoration.includes('line-through')) add('strikethrough');
        if (style.verticalAlign === 'super') add('superscript');
        else if (style.verticalAlign === 'sub') add('subscript');
        else if (style.verticalAlign === 'baseline') remove('subscript', 'superscript');
        if (style.fontFamily && isMonospace(style.fontFamily)) add('code');
    }
    return marks;
}

// A paragraph's last line break shows nothing, so it counts for nothing in offsets. A list item's own text leaves out
// the lists nested in it.

function leaves(block) {
    const walker = document.createTreeWalker(block, NodeFilter.SHOW_ELEMENT | NodeFilter.SHOW_TEXT, {
        acceptNode: node => node !== block && LISTS.has(node.localName) ? NodeFilter.FILTER_REJECT
            : node.nodeType === Node.TEXT_NODE || node.localName === 'br' ? NodeFilter.FILTER_ACCEPT
            : NodeFilter.FILTER_SKIP,
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
    const nested = [...block.childNodes].findIndex(node => LISTS.has(node.localName));
    return [block, nested >= 0 ? nested : block.childNodes.length];
}
