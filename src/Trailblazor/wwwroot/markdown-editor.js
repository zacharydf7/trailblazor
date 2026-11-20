import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { isApple } from '../Trailblazor.Primitives/platform.js';

// A markdown editor's toolbar and shortcuts. Each edit replaces the text area's selection through insertText, so the
// browser's undo history keeps it and the text area raises input as it does for typing.
export function createMarkdownEditor(editor) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!editor) {
        return null;
    }

    const area = () => editor.querySelector('[data-part="input"]');

    // The server lists the commands whose marks the preview keeps, and only those run, from the toolbar or the keys.
    const offers = command => (editor.dataset.commands ?? '').split(' ').includes(command);
    const apply = command => {
        const input = area();
        if (!input || input.disabled || input.readOnly || !edits[command]) {
            return;
        }

        const { from, to, text, select } = edits[command](input.value, input.selectionStart, input.selectionEnd);
        input.focus();
        input.setSelectionRange(from, to);
        if (!input.ownerDocument.execCommand('insertText', false, text)) {
            input.setRangeText(text, from, to);
            input.dispatchEvent(new Event('input', { bubbles: true }));
        }

        input.setSelectionRange(select[0], select[1]);
    };

    const onClick = event => {
        const button = event.target.closest('[data-command]');
        if (button && editor.contains(button) && !button.matches(':disabled, [aria-disabled="true"]')) {
            apply(button.dataset.command);
        }
    };
    const onKeyDown = event => {
        if (event.target !== area() || isComposing(event) || !(isApple() ? event.metaKey : event.ctrlKey)) {
            return;
        }

        const command = shortcut(event);
        if (command && offers(command)) {
            event.preventDefault();
            apply(command);
        }
    };

    editor.addEventListener('click', onClick);
    editor.addEventListener('keydown', onKeyDown);

    return {
        dispose() {
            editor.removeEventListener('click', onClick);
            editor.removeEventListener('keydown', onKeyDown);
        },
    };
}

// The same shortcuts as the rich text editor; the digits come from event.code, since Shift changes the key they
// type. Many keyboards type characters with AltGr, which Windows reports as Ctrl+Alt, so those are left alone.
function shortcut(event) {
    const key = event.key.toLowerCase();
    if (event.altKey) {
        const altGraph = !isApple() && event.getModifierState('AltGraph');
        return altGraph ? undefined : { Digit1: 'heading1', Digit2: 'heading2', Digit3: 'heading3' }[event.code];
    }

    if (event.shiftKey) {
        return key === 'x' ? 'strikethrough' : { Digit9: 'quote', Digit8: 'bullet', Digit7: 'number' }[event.code];
    }

    return { b: 'bold', i: 'italic', e: 'code', k: 'link' }[key];
}

// Each edit takes the text and selection, and gives the range to replace, its new text and what to select after.
const edits = {
    bold: (value, start, end) => wrap(value, start, end, '**'),
    italic: (value, start, end) => wrap(value, start, end, '*'),
    strikethrough: (value, start, end) => wrap(value, start, end, '~~'),
    code: (value, start, end) => value.slice(start, end).includes('\n')
        ? fence(value, start, end) : wrap(value, start, end, '`'),
    link: (value, start, end) => {
        const { lead, core, trail } = edges(value.slice(start, end));
        const label = core.replace(/[\\[\]]/g, '\\$&');
        const text = `${lead}[${label}](https://)${trail}`;
        // With a label, its address is selected, ready to be typed over; without one, the caret waits for the label.
        const open = start + lead.length + 1;
        const address = open + label.length + 2;
        return { from: start, to: end, text, select: label ? [address, address + 'https://'.length] : [open, open] };
    },
    heading1: (value, start, end) => prefixLines(value, start, end, /^# /, () => '# '),
    heading2: (value, start, end) => prefixLines(value, start, end, /^## /, () => '## '),
    heading3: (value, start, end) => prefixLines(value, start, end, /^### /, () => '### '),
    quote: (value, start, end) => prefixLines(value, start, end, /^> /, () => '> '),
    bullet: (value, start, end) => prefixLines(value, start, end, /^[-*+] /, () => '- '),
    number: (value, start, end) => prefixLines(value, start, end, /^\d+[.)] /, index => `${index + 1}. `),
    rule: (value, start, end) => {
        const before = value.slice(0, start);
        const gap = before === '' || before.endsWith('\n\n') ? '' : before.endsWith('\n') ? '\n' : '\n\n';
        const text = `${gap}---\n\n`;
        return { from: start, to: end, text, select: [start + text.length, start + text.length] };
    },
};

// Puts marks around the selection, or takes them off when they're already there, inside or just around it. Spaces at
// the selection's ends stay outside the marks, since markdown doesn't read a mark with a space on its inner side.
function wrap(value, start, end, mark) {
    const selected = value.slice(start, end);
    const width = mark.length;
    if (selected.length >= 2 * width && marked(selected, 0, mark) && marked(selected, selected.length - width, mark)) {
        const inner = selected.slice(width, -width);
        return { from: start, to: end, text: inner, select: [start, start + inner.length] };
    }

    if (marked(value, start - width, mark) && marked(value, end, mark)) {
        return { from: start - width, to: end + width, text: selected, select: [start - width, end - width] };
    }

    const { lead, core, trail } = edges(selected);
    const text = lead + mark + core + mark + trail;
    const inner = start + lead.length + width;
    return { from: start, to: end, text, select: [inner, inner + core.length] };
}

// The spaces at a selection's ends, which stay outside the marks, and the text between them.
function edges(selected) {
    const core = selected.trim();
    const lead = selected.slice(0, selected.length - selected.trimStart().length);
    return { lead, core, trail: selected.slice(lead.length + core.length) };
}

// Whether the mark is at this place on its own: a single * that's part of ** is bold's, not italic's.
function marked(text, at, mark) {
    if (at < 0 || text.slice(at, at + mark.length) !== mark) {
        return false;
    }

    const character = mark[0];
    return mark.length > 1 || (text[at - 1] !== character && text[at + 1] !== character);
}

// The fences go on lines of their own, or the closing one would open a fence that runs to the end.
function fence(value, start, end) {
    end = end > start && value[end - 1] === '\n' ? end - 1 : end;
    const selected = value.slice(start, end);
    const before = start === 0 || value[start - 1] === '\n' ? '' : '\n';
    const after = end === value.length || value[end] === '\n' ? '' : '\n';
    const text = `${before}\`\`\`\n${selected}\n\`\`\`${after}`;
    const inner = start + before.length + 4;
    return { from: start, to: end, text, select: [inner, inner + selected.length] };
}

// Gives each selected line the prefix, replacing any other block's, or takes it off when every line has it. A line's
// indentation stays, so a nested list item stays nested.
function prefixLines(value, start, end, has, prefix) {
    const from = value.lastIndexOf('\n', start - 1) + 1;
    const last = end > start && value[end - 1] === '\n' ? end - 1 : end;
    const lineEnd = value.indexOf('\n', last);
    const to = lineEnd < 0 ? value.length : lineEnd;
    const lines = value.slice(from, to).split('\n');
    const filled = lines.filter(line => line.trim() !== '');
    const indent = line => line.match(/^\s*/)[0];
    const off = filled.length > 0 && filled.every(line => has.test(line.trimStart()));
    // Each indentation level counts from 1, and starts over when a shallower line ends its list.
    const counts = new Map();
    const text = lines.map(line => {
        if (line.trim() === '') {
            return line;
        }

        const depth = indent(line);
        for (const deeper of [...counts.keys()].filter(key => key.length > depth.length)) {
            counts.delete(deeper);
        }

        const index = counts.get(depth) ?? 0;
        counts.set(depth, index + 1);
        const bare = line.trimStart().replace(/^(#{1,6} |> |[-*+] |\d+[.)] )/, '');
        return depth + (off ? bare : prefix(index) + bare);
    }).join('\n');
    return { from, to, text, select: [from, from + text.length] };
}
