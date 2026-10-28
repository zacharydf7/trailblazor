export function showModal(dialog) {
    dialog.showModal();
}

// show() alone leaves a non-modal dialog out of the top layer; showPopover() puts it there.
export function showNonModal(dialog) {
    dialog.show();
    dialog.showPopover();
}

// Planned design for criterion 2: cancel the close request the browser makes from Escape.
export function blockCancel(dialog) {
    dialog.addEventListener('cancel', event => event.preventDefault());
}

// Fallback design for criterion 2, on the document so it also catches Escape while focus is on <body>.
export function blockEscape(dialog) {
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && dialog.matches(':modal')) {
            event.preventDefault();
        }
    }, { capture: true });
}

export function showPopover(element) {
    if (!element.matches(':popover-open')) {
        element.showPopover();
    }
}

let currentAnchor = null;

// Native path: only the trigger that opened the overlay carries the anchor name the overlay's CSS points at.
export function showAnchoredNative(popover, anchor) {
    currentAnchor?.classList.remove('anchor');
    anchor.classList.add('anchor');
    currentAnchor = anchor;
    showPopover(popover);
}
