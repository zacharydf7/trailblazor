import { isComposing } from '../Trailblazor.Primitives/composing.js';
// A formatted number field is a text box acting as a spin button, so Up and Down step it rather than move the caret.
// Text the user edited commits first, so the step starts from it; unedited text matches the rendered aria-valuetext,
// and committing that would round the value to what the format shows. The JS initializer imports this at startup.
document.addEventListener('keydown', event => {
    const input = event.target;
    if ((event.key === 'ArrowUp' || event.key === 'ArrowDown') && !isComposing(event)
        && input.matches?.('input[type=text][role=spinbutton][data-part=input]')) {
        if (!input.readOnly && input.value !== input.getAttribute('aria-valuetext')) {
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }

        event.preventDefault();
    }
}, { capture: true });

let lastPointer = 'mouse';
document.addEventListener('pointerdown', event => lastPointer = event.pointerType, { capture: true });
const typed = new WeakSet();
document.addEventListener('input', event => typed.add(event.target), { capture: true });
document.addEventListener('change', event => typed.delete(event.target), { capture: true });

// A click on a step button puts focus in the input, as a number box's own spinner does, so the arrow keys keep working and
// focus isn't lost when the button turns off at a limit. Focus stays in the input, so it fires no change event and typed
// text commits here instead. Unedited text isn't committed: a second click before the first renders would send the old
// text and undo the first step. A touch doesn't move focus there, since that would open the on-screen keyboard.
document.addEventListener('mousedown', event => {
    const button = event.target.closest?.('[data-part="increment"], [data-part="decrement"]');
    const input = button?.closest('[data-part="control"]').querySelector('input[data-part="input"]');
    if (!input || event.button !== 0) {
        return;
    }

    event.preventDefault();
    if (typed.has(input)) {
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }

    if (lastPointer === 'mouse') {
        input.focus();
    }
});

// A mouse press on a step button that turned off at a limit passes through to the box, so this finds the button under the
// pointer and commits any typed text. It doesn't step, since the button looked off when it was pressed. A touch is left
// alone: its click could land on the button once the commit turns it back on, and step after all.
const offButton = '[data-part="increment"]:disabled, [data-part="decrement"]:disabled';
document.addEventListener('pointerdown', event => {
    const box = event.target.closest?.('[data-part="control"]');
    const input = box?.querySelector('input[data-part="input"]');
    const under = [...(box?.querySelectorAll(offButton) ?? [])].some(button => {
        const { left, right, top, bottom } = button.getBoundingClientRect();
        return event.clientX >= left && event.clientX < right && event.clientY >= top && event.clientY < bottom;
    });
    if (under && event.pointerType === 'mouse' && event.button === 0 && typed.has(input)) {
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }
}, { capture: true });
