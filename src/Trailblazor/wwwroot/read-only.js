// Native checkboxes and range inputs ignore readonly, so a field marks them aria-readonly and they are held still
// here. The JS initializer imports this at startup, so static pages are covered too.
const steppingKeys = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'PageUp', 'PageDown', 'Home', 'End']);

document.addEventListener('click', event => {
    if (event.target.matches?.('input[type=checkbox][aria-readonly=true]')) {
        event.preventDefault();
    }
}, { capture: true });

document.addEventListener('keydown', event => {
    if (steppingKeys.has(event.key) && event.target.matches?.('input[type=range][aria-readonly=true]')) {
        event.preventDefault();
    }
}, { capture: true });
