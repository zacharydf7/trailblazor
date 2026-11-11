import { ranges } from './slider-keys.js';

// Holds back a slider's input events and sends them on in turn: the first at once, then at most one every reportEvery
// milliseconds while it moves, and the last when it stops. A change sends any input still waiting in its slider first,
// a range slider's other thumb included, so the server has the last values before the commit. The JS initializer
// imports this at startup.
const reportEvery = 50;
const waiting = new WeakMap();
let sending = false;

const send = input => {
    const state = waiting.get(input);
    clearTimeout(state.timer);
    state.timer = 0;
    state.sentAt = performance.now();
    sending = true;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    sending = false;
};

// Blazor hears input on the document, so holding it back on the window keeps it from the server.
window.addEventListener('input', event => {
    const input = event.target;
    if (sending || !input.matches?.(ranges)) {
        return;
    }

    event.stopImmediatePropagation();
    const state = waiting.get(input) ?? { timer: 0, sentAt: -Infinity };
    waiting.set(input, state);
    state.timer ||= setTimeout(() => send(input), Math.max(0, state.sentAt + reportEvery - performance.now()));
}, { capture: true });

window.addEventListener('change', event => {
    const slider = event.target.closest?.('.tb-slider') ?? event.target;
    for (const input of [slider, ...slider.querySelectorAll('input')].filter(input => waiting.get(input)?.timer)) {
        send(input);
    }
}, { capture: true });
