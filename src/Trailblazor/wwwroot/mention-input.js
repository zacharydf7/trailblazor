import { createMentionSuggestions, longestMention } from './mention-suggestions.js';

// A mention input's suggestions, which mention-suggestions.js runs: this finds the trigger before the caret, places
// the mark the list opens under, and puts a chosen suggestion into the text. Focus never leaves the field, as with a
// combobox's input.
export function createMentionInput(input, listbox, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!input || !listbox) {
        return null;
    }

    const control = input.closest('[data-part="control"]') ?? input.parentElement;
    const caret = control.querySelector('[data-part="caret"]');

    // The trigger and text before the caret, when a trigger starts a word there and no space follows it.
    const find = () => {
        const end = input.selectionStart;
        if (end !== input.selectionEnd || input.readOnly || input.disabled) {
            return null;
        }

        const triggers = input.dataset.triggers ?? '@';
        for (let start = end - 1; start >= 0 && end - start <= longestMention; start--) {
            const character = input.value[start];
            if (/\s/.test(character)) {
                return null;
            }

            if (triggers.includes(character) && (start === 0 || /[\s([{"']/.test(input.value[start - 1]))) {
                return { trigger: character, place: start, text: input.value.slice(start + 1, end) };
            }
        }

        return null;
    };
    const placeCaret = found => {
        const point = caretPoint(input, found.place);
        caret.style.left = `${input.offsetLeft + point.x}px`;
        caret.style.top = `${input.offsetTop + point.y}px`;
        caret.style.height = `${point.height}px`;
    };
    const endOf = found => found.place + found.trigger.length + found.text.length;
    const holds = found => input.selectionStart === input.selectionEnd && input.selectionStart >= endOf(found)
        && input.value.slice(found.place, endOf(found)) === found.trigger + found.text;
    // The suggestion's text goes in as typing does, in place of the trigger and what was typed after it. Text typed
    // since Enter or Tab was pressed stays after it, with the caret still after that text; the space that follows the
    // suggestion stands in for a space typed first.
    const insert = (found, option) => {
        const typed = input.value.slice(endOf(found), input.selectionStart);
        const kept = typed.replace(/^ /, '');
        const end = endOf(found) + typed.length - kept.length;
        const text = `${option.dataset.insert} `;
        input.focus();
        input.setSelectionRange(found.place, end);
        if (!input.ownerDocument.execCommand('insertText', false, text)) {
            input.setRangeText(text, found.place, end, 'end');
            input.dispatchEvent(new Event('input', { bubbles: true }));
        }

        if (kept) {
            const caret = found.place + text.length + kept.length;
            input.setSelectionRange(caret, caret);
        }
    };
    // A one-line field is a combobox, which says whether its list is open; a text area has no such state.
    const onToggle = open => {
        if (input.getAttribute('role') === 'combobox') {
            input.setAttribute('aria-expanded', String(open));
        }
    };

    const suggestions = createMentionSuggestions(input, listbox, caret, events,
        { find, placeCaret, insert, holds, onToggle });
    const onInput = () => suggestions.update(true);
    const onSelectionChange = () => {
        if (input.ownerDocument.activeElement === input) {
            suggestions.update(false);
        }
    };
    const onKeyDown = event => suggestions.onKeyDown(event);

    input.addEventListener('input', onInput);
    input.addEventListener('keydown', onKeyDown);
    document.addEventListener('selectionchange', onSelectionChange);
    // Marks that the suggestions work, which tests wait for.
    input.dataset.ready = '';

    return {
        dispose() {
            suggestions.dispose();
            input.removeEventListener('input', onInput);
            input.removeEventListener('keydown', onKeyDown);
            document.removeEventListener('selectionchange', onSelectionChange);
        },
    };
}

// Where a character of the field's text sits, relative to the field's border box, measured in a hidden copy that wraps
// the text the same way.
function caretPoint(field, index) {
    const style = getComputedStyle(field);
    const copy = document.createElement('div');
    for (const name of ['boxSizing', 'width', 'fontFamily', 'fontSize', 'fontWeight', 'fontStyle', 'letterSpacing',
        'lineHeight', 'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft', 'borderTopWidth',
        'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth', 'textTransform', 'textIndent', 'tabSize',
        'wordSpacing', 'direction']) {
        copy.style[name] = style[name];
    }

    Object.assign(copy.style, {
        position: 'absolute', visibility: 'hidden', top: '0', left: '-9999px', borderStyle: 'solid',
        whiteSpace: field.localName === 'textarea' ? 'pre-wrap' : 'pre', overflowWrap: 'break-word',
    });
    copy.textContent = field.value.slice(0, index);
    const mark = document.createElement('span');
    mark.textContent = field.value.slice(index) || '.';
    copy.append(mark);
    field.ownerDocument.body.append(copy);
    const point = {
        x: mark.offsetLeft - field.scrollLeft,
        y: mark.offsetTop - field.scrollTop,
        height: parseFloat(style.lineHeight) || mark.offsetHeight,
    };
    copy.remove();
    return point;
}
