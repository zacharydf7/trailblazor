import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';

const focusableSelector = 'a[href], button, input, select, textarea, [tabindex]';

// One tooltip shows at a time, as in an operating system, so showing one hides any other, even one shown for focus.
let hideShown = null;

// Hover and focus stay in the browser, so showing a tooltip sends nothing to the server. The CSS bridges the gap onto
// the tooltip, and the hide delay covers a path that strays off the bridge (WCAG 1.4.13 hoverable).
export function createTooltip(anchor, tooltip, options, delays) {
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
            hideShown?.();
            tooltip.showPopover();
            position = createAnchoredPosition(tooltip, anchor, options);
            hideShown = hide;
        }
    };
    const hide = () => {
        clearTimeout(timer);
        if (tooltip.matches(':popover-open')) {
            tooltip.hidePopover();
        }

        position?.dispose();
        position = null;
        if (hideShown === hide) {
            hideShown = null;
        }
    };
    const showSoon = () => {
        clearTimeout(timer);
        timer = setTimeout(show, delays.show);
    };
    const hideSoon = () => {
        clearTimeout(timer);
        timer = setTimeout(hide, delays.hide);
    };
    // Focus a click or tap leaves, or that a dialog closed by one hands back, isn't :focus-visible: its user wants the
    // control, not its description. A text field is, since its user is about to type.
    const onFocusIn = event => {
        if (event.target.matches(':focus-visible')) {
            show();
        }
    };
    // The Escape that hides a shown tooltip is used up, so a dialog around it stays open.
    const onKeyDown = event => {
        if (event.key !== 'Escape') {
            return;
        }

        if (tooltip.matches(':popover-open')) {
            event.preventDefault();
        }

        hide();
    };

    // The tooltip is inside the anchor, so moving onto it keeps it open, unless it's forced over the anchor and lets
    // presses through; Escape always dismisses it (WCAG 1.4.13).
    anchor.addEventListener('pointerenter', showSoon);
    anchor.addEventListener('pointerleave', hideSoon);
    anchor.addEventListener('focusin', onFocusIn);
    anchor.addEventListener('focusout', hide);
    anchor.addEventListener('pointerdown', hide);
    document.addEventListener('keydown', onKeyDown);

    // Setup finishes after the first render, so focus or a pointer that arrived earlier still counts.
    if (anchor.contains(document.activeElement) && document.activeElement.matches(':focus-visible')) {
        show();
    } else if (anchor.matches(':hover')) {
        showSoon();
    }

    anchor.dataset.ready = '';
    return {
        dispose() {
            delete anchor.dataset.ready;
            observer.disconnect();
            hide();
            anchor.removeEventListener('pointerenter', showSoon);
            anchor.removeEventListener('pointerleave', hideSoon);
            anchor.removeEventListener('focusin', onFocusIn);
            anchor.removeEventListener('focusout', hide);
            anchor.removeEventListener('pointerdown', hide);
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

// Adds the tooltip's id to the trigger's description, unless it is already there or the tooltip only repeats the
// trigger's name, which a screen reader would then read twice.
function describe(anchor, tooltip) {
    const trigger = anchor.querySelector(focusableSelector) ?? anchor;
    const ids = describedBy(trigger);
    const repeatsName = trigger.getAttribute('aria-label')?.trim() === tooltip.textContent.trim();
    if (!ids.includes(tooltip.id) && !repeatsName) {
        trigger.setAttribute('aria-describedby', [...ids, tooltip.id].join(' '));
    }

    return trigger;
}

function describedBy(element) {
    return (element.getAttribute('aria-describedby') ?? '').split(' ').filter(Boolean);
}
