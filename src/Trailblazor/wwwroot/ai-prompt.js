// An AI prompt's keys: Enter in the box generates, Shift+Enter starts a new line, and Escape stops a response while it
// streams. A suggestion puts focus back in the box, ready to change it.
export function createAiPrompt(root) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    const box = root.querySelector(':scope > [data-part=composer] [data-part=input]');

    function onKeyDown(event) {
        if (event.key === 'Escape') {
            if (root.hasAttribute('data-generating')) {
                event.preventDefault();
                root.querySelector(':scope > [data-part=composer] [data-part=generate]').click();
            }
            return;
        }

        // keyCode 229 is a key that an input method editor is still composing into a character.
        if (event.key === 'Enter' && event.target === box && !event.shiftKey && !event.isComposing
            && event.keyCode !== 229) {
            event.preventDefault();
            box.form.requestSubmit();
        }
    }

    function onClick(event) {
        if (event.target.closest('[data-part=suggestion]')) {
            box.focus();
        }
    }

    root.addEventListener('keydown', onKeyDown);
    root.addEventListener('click', onClick);

    return {
        dispose() {
            root.removeEventListener('keydown', onKeyDown);
            root.removeEventListener('click', onClick);
        },
    };
}
