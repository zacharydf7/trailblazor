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

    const usable = [...alwaysUsable, ...ownParts, ...outsidePopups].join(', ');
    const top = contained ? overlay.offsetParent ?? overlay.parentElement : document.body;
    const focused = document.activeElement;
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
            if (document.activeElement === document.body && focused?.isConnected && !focused.closest('[inert]')) {
                focused.focus({ preventScroll: true });
            }
        },
    };
}
