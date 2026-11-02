// Counts how many blockers (overlays and modal dialogs alike) want each element inert, so one that ends doesn't free
// what another still covers. An element the app made inert itself is left as the app set it.
const holds = new WeakMap();

// Blazor's reconnect dialog and error bar, which must work whatever is open.
export const alwaysUsable = ['#components-reconnect-modal', '#blazor-error-ui'];

export function hold(elements) {
    for (const element of elements) {
        const count = holds.get(element);
        if (count) {
            holds.set(element, count + 1);
        } else if (!element.inert) {
            holds.set(element, 1);
            element.inert = true;
        }
    }
}

export function release(elements) {
    for (const element of elements) {
        const count = holds.get(element);
        if (count === 1) {
            holds.delete(element);
            element.inert = false;
        } else if (count) {
            holds.set(element, count - 1);
        }
    }
}
