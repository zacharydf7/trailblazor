// Safari doesn't focus a button when it's clicked, so the control last pressed stands in for the focused one.
// Trailblazor's JS initializer imports this at startup, so presses before any overlay loads are still seen.
const pressableSelector = 'a[href], button, input, select, textarea, [tabindex]';
let lastPressed = null;
let pointerLast = false;

document.addEventListener('pointerdown', event => {
    lastPressed = event.target.closest?.(pressableSelector) ?? null;
    pointerLast = true;
}, { capture: true });

document.addEventListener('keydown', () => {
    pointerLast = false;
}, { capture: true });

// Controls that take text.
export const typedInSelector = 'textarea, [contenteditable]:not([contenteditable="false"]), '
    + 'input:not([type=checkbox], [type=radio], [type=range], [type=button], [type=submit], [type=reset], '
    + '[type=image], [type=file], [type=color])';

// A control that a script focuses shows a focus ring after a key but not after a click. Safari leaves focus where it
// was on a click, so its own rule would carry a text field's ring onto the button, and Firefox leaves the ring off
// when focus goes back after Escape. A field you type in always shows its ring, and a tooltip on it relies on that.
export function focusForLastInput(element) {
    element.focus(element.matches(typedInSelector) ? undefined : { focusVisible: !pointerLast });
}

// The control the pointer last pressed when focus fell to something around it (the page, or a dialog that took
// the focus instead); otherwise the focused element.
export function focusOrigin() {
    const active = document.activeElement;
    return lastPressed?.isConnected && (!active || active.contains(lastPressed)) ? lastPressed : active;
}
