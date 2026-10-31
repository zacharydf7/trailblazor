// A formatted number field is a text box acting as a spin button, so Up and Down step it rather than move the caret.
// Text the user edited commits first, so the step starts from it; unedited text matches the rendered aria-valuetext,
// and committing that would round the value to what the format shows. The JS initializer imports this at startup.
document.addEventListener('keydown', event => {
    const input = event.target;
    if ((event.key === 'ArrowUp' || event.key === 'ArrowDown')
        && input.matches?.('input[type=text][role=spinbutton][data-part=input]')) {
        if (!input.readOnly && input.value !== input.getAttribute('aria-valuetext')) {
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }

        event.preventDefault();
    }
}, { capture: true });
