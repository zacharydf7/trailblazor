// The whole box of a text field acts as its text box, as a native input's own padding does. The JS initializer imports
// this at startup, so static pages are covered too.
const interactive = 'a[href], button, input, select, textarea, label, summary, [tabindex], [popover]';

document.addEventListener('mousedown', event => {
    const box = event.target.closest?.('[data-part="control"]');
    if (!box || event.button !== 0 || event.target.closest(interactive)) {
        return;
    }

    const input = box.querySelector('input.tb-field__input, textarea.tb-field__input');
    if (input && !input.matches(':disabled')) {
        // The press's default action runs after this handler and would take focus back off the input.
        event.preventDefault();
        input.focus();
    }
});
