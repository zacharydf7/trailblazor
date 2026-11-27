// Native checkboxes, range inputs and selects ignore readonly, so a field marks them aria-readonly and they are held
// still here. The JS initializer imports this at startup, so static pages are covered too.
const steppingKeys = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'PageUp', 'PageDown', 'Home', 'End']);

document.addEventListener('click', event => {
    if (/** @type {Element} */ (event.target).matches?.('input[type=checkbox][aria-readonly=true]')) {
        event.preventDefault();
    }
}, { capture: true });

// A list can change by the pointer, the keys or assistive technology, so whatever changed it, the choice it was
// rendered with comes back.
document.addEventListener('input', event => {
    const select = /** @type {HTMLSelectElement} */ (event.target);
    if (select.matches?.('select[aria-readonly=true]')) {
        for (const option of select.options) {
            option.selected = option.defaultSelected;
        }
    }
}, { capture: true });

document.addEventListener('keydown', event => {
    if (steppingKeys.has(event.key)
        && /** @type {Element} */ (event.target).matches?.('input[type=range][aria-readonly=true]')) {
        event.preventDefault();
    }
}, { capture: true });
