import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';

const focusableSelector = 'a[href], button, input, select, textarea, [tabindex]';
const showDelay = 300;
// The CSS bridges the gap onto the tooltip; this covers a path that strays off the bridge (WCAG 1.4.13 hoverable).
const hideDelay = 300;

// Hover and focus stay in the browser, so showing a tooltip sends nothing to the server.
export function createTooltip(anchor, tooltip, options) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!anchor || !tooltip) {
        return null;
    }

    let trigger = describe(anchor, tooltip);
    let position = null;
    let timer = 0;

    // Blazor rewrites aria-describedby when the trigger re-renders, so the tooltip's id is put back.
    const observer = new MutationObserver(() => {
        trigger = describe(anchor, tooltip);
    });
    observer.observe(anchor, { subtree: true, childList: true, attributeFilter: ['aria-describedby'] });

    const show = () => {
        clearTimeout(timer);
        if (!tooltip.matches(':popover-open')) {
            tooltip.showPopover();
            position = createAnchoredPosition(tooltip, anchor, options);
        }
    };
    const hide = () => {
        clearTimeout(timer);
        if (tooltip.matches(':popover-open')) {
            tooltip.hidePopover();
        }

        position?.dispose();
        position = null;
    };
    const showSoon = () => {
        clearTimeout(timer);
        timer = setTimeout(show, showDelay);
    };
    const hideSoon = () => {
        clearTimeout(timer);
        timer = setTimeout(hide, hideDelay);
    };
    const onKeyDown = event => {
        if (event.key === 'Escape') {
            hide();
        }
    };

    // The tooltip is inside the anchor, so moving onto it keeps it open; Escape always dismisses it (WCAG 1.4.13).
    anchor.addEventListener('pointerenter', showSoon);
    anchor.addEventListener('pointerleave', hideSoon);
    anchor.addEventListener('focusin', show);
    anchor.addEventListener('focusout', hide);
    document.addEventListener('keydown', onKeyDown);

    // Setup finishes after the first render, so focus or a pointer that arrived earlier still counts.
    if (anchor.contains(document.activeElement)) {
        show();
    } else if (anchor.matches(':hover')) {
        showSoon();
    }

    return {
        dispose() {
            observer.disconnect();
            hide();
            anchor.removeEventListener('pointerenter', showSoon);
            anchor.removeEventListener('pointerleave', hideSoon);
            anchor.removeEventListener('focusin', show);
            anchor.removeEventListener('focusout', hide);
            document.removeEventListener('keydown', onKeyDown);
            const ids = describedBy(trigger).filter(id => id !== tooltip.id);
            if (ids.length > 0) {
                trigger.setAttribute('aria-describedby', ids.join(' '));
            } else {
                trigger.removeAttribute('aria-describedby');
            }
        },
    };
}

// Adds the tooltip's id to the trigger's description, unless it is already there.
function describe(anchor, tooltip) {
    const trigger = anchor.querySelector(focusableSelector) ?? anchor;
    const ids = describedBy(trigger);
    if (!ids.includes(tooltip.id)) {
        trigger.setAttribute('aria-describedby', [...ids, tooltip.id].join(' '));
    }

    return trigger;
}

function describedBy(element) {
    return (element.getAttribute('aria-describedby') ?? '').split(' ').filter(Boolean);
}
