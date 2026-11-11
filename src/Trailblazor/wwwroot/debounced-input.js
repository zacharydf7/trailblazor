// Holds back a text field's input events and sends the last one once typing has paused for the field's
// data-input-delay milliseconds. A change, as on Enter or leaving the field, sends it at once. Listeners on the field
// itself, such as its mask's, still hear every keystroke. The JS initializer imports this at startup.
const waiting = new WeakMap();
let sending = false;

const send = field => {
    clearTimeout(waiting.get(field));
    waiting.delete(field);
    sending = true;
    field.dispatchEvent(new Event('input', { bubbles: true }));
    sending = false;
};

// Blazor hears input on the document, so stopping it at the root keeps it from the server but not from the field.
document.documentElement.addEventListener('input', event => {
    const field = event.target;
    if (sending || !field.matches?.('[data-input-delay]')) {
        return;
    }

    event.stopPropagation();
    clearTimeout(waiting.get(field));
    waiting.set(field, setTimeout(() => send(field), +field.dataset.inputDelay));
});

window.addEventListener('change', event => {
    if (waiting.has(event.target)) {
        send(event.target);
    }
}, { capture: true });
