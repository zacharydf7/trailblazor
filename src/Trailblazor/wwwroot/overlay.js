import { alwaysUsable, hold, release } from './inert.js';

// Besides Blazor's own parts and the popups the app names, the live regions, toasts and dialogs stay usable whatever
// an overlay covers. An inert live region says nothing.
const ownParts = ['[data-part="announcer"]', '[data-part="toaster"]', 'dialog'];

// An element that holds something usable stays usable itself, with each of its other children made inert.
function collect(element, usable, elements) {
    if (element.matches(usable)) {
        return;
    }

    if (element.querySelector(usable)) {
        for (const child of element.children) {
            collect(child, usable, elements);
        }

        return;
    }

    elements.push(element);
}

// Makes everything beside the overlay, and beside each of its ancestors, inert, up to the element a contained overlay
// covers or to the page's body, so Tab and screen readers can't reach what it hides. Disposing the result undoes it
// and gives focus back if it was behind the overlay.
export function blockBehind(overlay, contained, outsidePopups) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!overlay) {
        return null;
    }

    // Overlays opened after this one and drawn above it stay usable, as their own block may have run first or not yet.
    // A contained overlay is never above a page overlay.
    const order = Number(overlay.dataset.openOrder);
    const page = overlay.ownerDocument;
    const later = [...page.querySelectorAll(contained ? '[data-open-order]' : '.tb-overlay--fixed[data-open-order]')]
        .filter(other => Number(other.dataset.openOrder) > order)
        .map(other => `[data-open-order="${other.dataset.openOrder}"]`);
    const usable = [...alwaysUsable, ...ownParts, ...outsidePopups, ...later].join(', ');
    const top = contained ? overlay.offsetParent ?? overlay.parentElement : page.body;
    const focused = page.activeElement;
    const elements = [];
    for (let node = overlay; node && node !== top; node = node.parentElement) {
        for (const sibling of node.parentElement?.children ?? []) {
            if (sibling !== node) {
                collect(sibling, usable, elements);
            }
        }
    }

    hold(elements);
    return {
        dispose() {
            release(elements);
            const now = overlay.ownerDocument;
            if (now.activeElement === now.body && focused?.isConnected && !focused.closest('[inert]')) {
                focused.focus({ preventScroll: true });
            }
        },
    };
}
