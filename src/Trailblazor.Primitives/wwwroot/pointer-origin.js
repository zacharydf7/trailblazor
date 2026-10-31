// Safari doesn't focus a button when it's clicked, so the control last pressed stands in for the focused one.
// Trailblazor's JS initializer imports this at startup, so presses before any overlay loads are still seen.
const pressableSelector = 'a[href], button, input, select, textarea, [tabindex]';
let lastPressed = null;

document.addEventListener('pointerdown', event => {
    lastPressed = event.target.closest?.(pressableSelector) ?? null;
}, { capture: true });

// The control the pointer last pressed when focus fell to something around it (the page, or a dialog that took
// the focus instead); otherwise the focused element.
export function focusOrigin() {
    const active = document.activeElement;
    return lastPressed?.isConnected && (!active || active.contains(lastPressed)) ? lastPressed : active;
}
