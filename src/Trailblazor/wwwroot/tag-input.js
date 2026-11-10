// A tag input's keys. Enter or a separator adds what was typed, pasted text is split into tags, Backspace in an empty
// input removes the last tag, and the arrow keys move between the tags' remove buttons. Each change reaches the server
// as an event on the input, which a test can raise the same way.
const add = (input, text) =>
    input.dispatchEvent(new CustomEvent('tb-tag-add', { bubbles: true, detail: { text } }));
const remove = (input, index, focus) =>
    input.dispatchEvent(new CustomEvent('tb-tag-remove', { bubbles: true, detail: { index, focus } }));

export function createTagInput(input) {
    const field = input.closest('[data-part="control"]');
    if (!field) {
        return null;
    }

    const buttons = () => [...field.querySelectorAll('[data-part="remove"]')];
    // A removal moves focus once the server has rendered it, unless a key pressed in the field since moved focus to the
    // input or out of the field. The tags aren't keyed, so after the render the button a key moved to shows another
    // tag or is gone, leaving focus on the page itself; either way the removal moves focus as it would have.
    let keyAfterRemoval = false;
    const startRemoval = () => keyAfterRemoval = false;
    const separators = () => input.dataset.separators ?? '';
    const backKey = () => getComputedStyle(field).direction === 'rtl' ? 'ArrowRight' : 'ArrowLeft';
    const forwardKey = () => getComputedStyle(field).direction === 'rtl' ? 'ArrowLeft' : 'ArrowRight';

    // The typed text leaves the input as it's sent; the server puts back any it refuses.
    const commit = () => {
        const text = input.value;
        input.value = '';
        add(input, text);
    };

    const onInputKey = event => {
        if (event.isComposing || event.ctrlKey || event.metaKey || event.altKey) {
            return;
        }

        // Enter in an empty input is left to the form, so it still submits.
        if (event.key === 'Enter' && input.value.trim() !== '') {
            event.preventDefault();
            commit();
        } else if (event.key.length === 1 && separators().includes(event.key)) {
            event.preventDefault();
            if (input.value.trim() !== '') {
                commit();
            }
        } else if (event.key === 'Backspace' && input.value === '' && buttons().length > 0) {
            // WebKit's Windows and Linux ports would otherwise go back a page.
            event.preventDefault();
            startRemoval();
            remove(input, buttons().length - 1, 'input');
        } else if (event.key === backKey() && input.selectionStart === 0 && input.selectionEnd === 0
            && buttons().length > 0) {
            event.preventDefault();
            buttons().at(-1).focus();
        }
    };

    const onTagKey = event => {
        const button = event.target.closest?.('[data-part="remove"]');
        if (!button || event.ctrlKey || event.metaKey || event.altKey) {
            return;
        }

        const all = buttons();
        const index = all.indexOf(button);
        const moves = {
            [backKey()]: () => (all[index - 1] ?? button).focus(),
            [forwardKey()]: () => (all[index + 1] ?? input).focus(),
            Home: () => all[0].focus(),
            End: () => input.focus(),
            Backspace: () => {
                startRemoval();
                remove(input, index, 'previous');
            },
            Delete: () => {
                startRemoval();
                remove(input, index, 'next');
            },
        };
        if (moves[event.key]) {
            event.preventDefault();
            moves[event.key]();
        }
    };

    // Pasted text with a separator or a line break becomes a tag for each part, which the server splits. A text box
    // drops line breaks, even ones a script writes, so the text goes straight to the server.
    const onPaste = event => {
        const pasted = event.clipboardData?.getData('text') ?? '';
        if (!/[\r\n]/.test(pasted) && ![...separators()].some(separator => pasted.includes(separator))) {
            return;
        }

        event.preventDefault();
        const value = input.value;
        const text = value.slice(0, input.selectionStart) + pasted + value.slice(input.selectionEnd);
        input.value = '';
        add(input, text);
    };

    // Leaving the field adds what was typed. With nothing typed, the event still goes, to take back an old message.
    // Switching to another window blurs the input too, but leaves it the active element, and isn't leaving.
    const onLeave = event => {
        if (!field.contains(event.relatedTarget) && document.activeElement !== input) {
            commit();
        }
    };

    const onAnyKey = () => keyAfterRemoval = true;
    const onClick = event => {
        if (event.target.closest?.('[data-part="remove"]')) {
            startRemoval();
        }
    };

    field.addEventListener('keydown', onAnyKey, true);
    field.addEventListener('click', onClick);
    input.addEventListener('keydown', onInputKey);
    input.addEventListener('paste', onPaste);
    input.addEventListener('blur', onLeave);
    field.addEventListener('keydown', onTagKey);

    return {
        // Text the server refused goes back in front of anything typed since, joined by a separator so it splits the
        // same way when it's added again. With no separator to join them, only the first comes back, not a merged tag.
        restore(texts) {
            const shown = separators() === '' ? texts.slice(0, 1) : texts;
            input.value = [...shown, input.value].filter(text => text !== '').join(`${separators()[0] ?? ''} `);
        },
        // A tag's remove button by its position, or the input for -1 or a position past the last tag.
        focusTag(index) {
            const active = document.activeElement;
            if (!keyAfterRemoval || active === document.body || buttons().includes(active)) {
                (buttons()[index] ?? input).focus();
            }
        },
        dispose() {
            input.removeEventListener('keydown', onInputKey);
            input.removeEventListener('paste', onPaste);
            input.removeEventListener('blur', onLeave);
            field.removeEventListener('keydown', onTagKey);
            field.removeEventListener('keydown', onAnyKey, true);
            field.removeEventListener('click', onClick);
        },
    };
}
