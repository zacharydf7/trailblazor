// The whole box of a text field acts as its text box, as a native input's own padding does. The JS initializer imports
// this at startup, so static pages are covered too.
const interactive = 'a[href], button, input, select, textarea, label, summary, [tabindex], [popover]';

document.addEventListener('mousedown', event => {
    const box = event.target.closest?.('[data-part="control"]');
    // Only what is pressed inside the box counts: something around the field can take the focus too, as a tab's
    // panel does.
    const pressed = event.target.closest?.(interactive);
    if (!box || event.button !== 0 || (pressed && box.contains(pressed))) {
        return;
    }

    const input = box.querySelector('input.tb-field__input, textarea.tb-field__input');
    if (input && !input.matches(':disabled')) {
        // The press's default action runs after this handler and would take focus back off the input.
        event.preventDefault();
        input.focus();
    }
});
