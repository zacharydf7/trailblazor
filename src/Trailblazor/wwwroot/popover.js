import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { firstTabbable } from '../Trailblazor.Primitives/focus-scope.js';

const showDelay = 300;
// The pointer crosses the gap between the trigger and the panel; this keeps the panel open on the way (WCAG 1.4.13).
const hideDelay = 300;

// Opening, closing and focus stay in the browser; only a bound Open reaches the server.
export function createPopover(root, panel, options, hover, dotNetReference) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root || !panel) {
        return null;
    }

    const trigger = root.querySelector(`[aria-controls="${panel.id}"]`);
    if (!trigger) {
        throw new Error('A custom TbPopover trigger needs context.Attributes on its button.');
    }

    let position = null;
    // A panel opened by hovering closes when the pointer leaves, until the user starts working in it.
    let openedByHover = false;
    let returnFocus = false;
    let timer = 0;

    const takeFocus = () => (firstTabbable(panel) ?? panel).focus();
    const onBeforeToggle = event => {
        // The browser gives focus back to what had it when the panel opened, which after a hover isn't the trigger.
        if (event.newState === 'closed') {
            returnFocus = panel.contains(document.activeElement);
            return;
        }

        // toggle fires a task later, possibly after a paint, so the panel is placed in the next frame.
        requestAnimationFrame(() => {
            if (panel.matches(':popover-open') && !position) {
                position = createAnchoredPosition(panel, trigger, options);
            }
        });
    };
    const onToggle = event => {
        const open = event.newState === 'open';
        trigger.setAttribute('aria-expanded', String(open));
        if (open) {
            if (!openedByHover) {
                takeFocus();
            }
        } else {
            position?.dispose();
            position = null;
            openedByHover = false;
            if (returnFocus) {
                trigger.focus();
            }
        }

        dotNetReference?.invokeMethodAsync('OnToggle', open);
    };
    // A click on the trigger of a panel the pointer opened keeps it open on purpose, instead of toggling it shut.
    const onTriggerClick = event => {
        if (openedByHover && panel.matches(':popover-open')) {
            event.preventDefault();
            openedByHover = false;
            takeFocus();
        }
    };
    const onPanelFocusIn = () => {
        openedByHover = false;
    };
    // Tabbing out of the panel closes it, so it doesn't cover what focus moves to (WCAG 2.4.11).
    const onFocusOut = event => {
        if (event.relatedTarget && !root.contains(event.relatedTarget) && panel.matches(':popover-open')) {
            panel.hidePopover();
        }
    };
    const show = () => {
        clearTimeout(timer);
        // Opening an auto popover closes the others, except ones it sits in, so a hover mustn't close what the user
        // opened elsewhere.
        const elsewhere = [...document.querySelectorAll('[popover="auto"]:popover-open')]
            .some(open => !open.contains(panel));
        if (!panel.matches(':popover-open') && !elsewhere) {
            openedByHover = true;
            panel.showPopover();
        }
    };
    const hide = () => {
        clearTimeout(timer);
        if (openedByHover && panel.matches(':popover-open')) {
            panel.hidePopover();
        }
    };
    const showSoon = () => {
        clearTimeout(timer);
        timer = setTimeout(show, showDelay);
    };
    const hideSoon = () => {
        clearTimeout(timer);
        timer = setTimeout(hide, hideDelay);
    };
    const stayOpen = () => clearTimeout(timer);

    trigger.setAttribute('aria-expanded', String(panel.matches(':popover-open')));
    panel.addEventListener('beforetoggle', onBeforeToggle);
    panel.addEventListener('toggle', onToggle);
    panel.addEventListener('focusin', onPanelFocusIn);
    root.addEventListener('focusout', onFocusOut);
    // The panel sits inside the root, so moving the pointer onto it keeps the popover open.
    if (hover) {
        trigger.addEventListener('click', onTriggerClick);
        trigger.addEventListener('pointerenter', showSoon);
        root.addEventListener('pointerleave', hideSoon);
        panel.addEventListener('pointerenter', stayOpen);
    }

    // Tests wait for this before using the keyboard, which needs the handlers above.
    trigger.dataset.ready = '';
    return {
        setOpen(open) {
            openedByHover = false;
            if (open && !panel.matches(':popover-open')) {
                panel.showPopover();
            } else if (!open && panel.matches(':popover-open')) {
                panel.hidePopover();
            }
        },
        dispose() {
            clearTimeout(timer);
            position?.dispose();
            panel.removeEventListener('beforetoggle', onBeforeToggle);
            panel.removeEventListener('toggle', onToggle);
            panel.removeEventListener('focusin', onPanelFocusIn);
            root.removeEventListener('focusout', onFocusOut);
            trigger.removeEventListener('click', onTriggerClick);
            trigger.removeEventListener('pointerenter', showSoon);
            root.removeEventListener('pointerleave', hideSoon);
            panel.removeEventListener('pointerenter', stayOpen);
        },
    };
}
