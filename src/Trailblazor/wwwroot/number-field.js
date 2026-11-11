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

const spinner = 'input[data-part="input"]:is([type=number], [role=spinbutton])';
const step = (input, direction, how) => input.dispatchEvent(new CustomEvent('tb-number-step',
    { bubbles: true, detail: { direction, large: false, held: false, ...how } }));

// Page Up and Page Down take a large step, in a number box and a formatted field alike. Typed text commits first, so
// the step starts from it.
document.addEventListener('keydown', event => {
    const input = event.target;
    if ((event.key === 'PageUp' || event.key === 'PageDown') && !isComposing(event) && !event.altKey && !event.ctrlKey
        && !event.metaKey && input.matches?.(spinner)) {
        event.preventDefault();
        if (typed.has(input)) {
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }

        step(input, event.key === 'PageUp' ? 1 : -1, { large: true });
    }
}, { capture: true });

// Holding a step button steps again and again, faster the longer it's held. The field shows each step, and the value
// changes once, when the button is let go. A touch's mouse events come only once it lifts, too late for the mousedown
// handler above to commit typed text, so the first repeat commits it.
const holdDelay = 400;
const firstRepeat = 150;
const fastestRepeat = 40;
const speedUp = 0.85;
let holding = null;
// The click that ends a hold would step once more, so it's dropped. A touch's click can come a while after it lifts, so
// the next click on the button within a second is dropped, unless a new press comes first.
const dropWindow = 1000;
let dropping = null;

const release = () => {
    if (!holding) {
        return;
    }

    const { button, input, timer, repeated } = holding;
    clearTimeout(timer);
    holding = null;
    if (repeated) {
        step(input, 0);
        dropping = { button, until: performance.now() + dropWindow };
    }
};

document.addEventListener('pointerdown', event => {
    release();
    dropping = null;
    const button = event.target.closest?.('[data-part="increment"], [data-part="decrement"]');
    const input = button?.closest('[data-part="control"]')?.querySelector(spinner);
    if (!input || button.disabled || event.button !== 0) {
        return;
    }

    const direction = button.dataset.part === 'increment' ? 1 : -1;
    const hold = { button, input, repeated: false, timer: 0 };
    let wait = firstRepeat;
    const repeat = () => {
        if (!hold.repeated && typed.has(input)) {
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }

        hold.repeated = true;
        step(input, direction, { held: true });
        wait = Math.max(fastestRepeat, wait * speedUp);
        hold.timer = setTimeout(repeat, wait);
    };
    hold.timer = setTimeout(repeat, holdDelay);
    holding = hold;
}, { capture: true });
document.addEventListener('pointerup', release, { capture: true });
document.addEventListener('pointercancel', release, { capture: true });
window.addEventListener('click', event => {
    if (dropping && performance.now() < dropping.until && dropping.button.contains(event.target)) {
        event.stopPropagation();
        event.preventDefault();
    }

    dropping = null;
}, { capture: true });
// A long press on a touch screen would open a menu, on some systems only once it lifts.
document.addEventListener('contextmenu', event => {
    if (holding || dropping?.button.contains(event.target)) {
        event.preventDefault();
    }
}, { capture: true });
