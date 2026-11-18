// A grip is a menu button on a line that also drags the line: a click or a tap opens its menu, and a drag doesn't.
// The caller moves the line; this tells a drag from a tap and keeps a drag's release from opening the menu.
export function createGripDrag(root) {
    // The grip a drag was released on, whose click would open its menu. A touch drag sends no click, so the next
    // press clears it.
    let dragged = null;
    // A drag's release would open the grip's menu, so that click goes nowhere.
    const onClick = event => {
        if (dragged?.contains(event.target)) {
            event.preventDefault();
            event.stopPropagation();
        }

        dragged = null;
    };

    root.addEventListener('click', onClick, { capture: true });
    return {
        pressed() {
            dragged = null;
        },
        // A tap on the grip wanders a few pixels, more with a finger, and still opens its menu without resizing.
        isDrag: (moved, event) => Math.abs(moved) >= (event.pointerType === 'mouse' ? 4 : 10),
        // Takes the grip a drag ended on, or null after a press that never became a drag.
        released(grip) {
            dragged = grip;
        },
        dispose() {
            root.removeEventListener('click', onClick, { capture: true });
        },
    };
}
