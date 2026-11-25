import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { onEachPage } from '../Trailblazor.Primitives/pages.js';

// Hover, focus and Escape stay in the browser, and one set of listeners serves every tooltip, so a tooltip costs the
// server nothing, even to set up. The JS initializer imports this at startup; each bubble carries its own settings.
const focusableSelector = 'a[href], button, input, select, textarea, [tabindex]';
// A TbTooltip wraps its trigger, and cut-off text that shows its full text in a tooltip is its own trigger.
// A trigger that a press does nothing else to: no role of its own, and neither a control nor inside one.
const controls = 'a[href], button, input, select, textarea, label, summary, [contenteditable], [role=button], '
    + '[role=link], [role=option], [role=menuitem], [role=menuitemcheckbox], [role=menuitemradio], [role=tab], '
    + '[role=treeitem], [role=checkbox], [role=radio], [role=switch], [role=row], [role=gridcell]';
const isPlain = trigger => !trigger.matches('[role]') && !trigger.closest(controls);
const anchorSelector = '.tb-tooltip-anchor, .tb-text--full-text, .tb-button--reason';
const bubbleSelector = ':is(.tb-tooltip-anchor, .tb-text--full-text, .tb-button--reason) > [data-part="tooltip"]';

// The trigger each bubble describes, so its description can be taken back when the bubble goes.
const described = new WeakMap();
const timers = new WeakMap();
// One tooltip shows at a time, as in an operating system, so showing one hides any other, even one shown for focus.
let shown = null;
// The anchor the pointer last rested on, whose tooltip may still be waiting out its delay.
let waiting = null;
// Once the pointer shows a tooltip, the next one it reaches shows at once, as along a toolbar, until this long after
// the last one hides. A tooltip shown for keyboard focus doesn't count, so a pointer passing by leaves it alone.
const skipWindow = 500;
let hidAt = -Infinity;

const bubbleOf = anchor => anchor?.querySelector(':scope > [data-part="tooltip"]') ?? null;
const anchorOf = target => {
    const anchor = target?.closest?.(anchorSelector);
    return bubbleOf(anchor) ? anchor : null;
};

const hideShown = () => {
    if (shown) {
        const { tooltip, position } = shown;
        shown = null;
        position.dispose();
        if (tooltip.matches(':popover-open')) {
            tooltip.hidePopover();
        }
    }
};

const show = (anchor, byPointer = false) => {
    clearTimeout(timers.get(anchor));
    const tooltip = bubbleOf(anchor);
    if (tooltip && !tooltip.matches(':popover-open')) {
        hideShown();
        tooltip.showPopover();
        const { placement, align, offset } = tooltip.dataset;
        const position = createAnchoredPosition(tooltip, anchor, { side: placement, align, offset: +offset });
        shown = { anchor, tooltip, position, byPointer };
    }
};

const hide = anchor => {
    clearTimeout(timers.get(anchor));
    if (shown?.anchor === anchor) {
        hideShown();
    }
};

const later = (anchor, action, delay) => {
    clearTimeout(timers.get(anchor));
    timers.set(anchor, setTimeout(() => action(anchor), +delay));
};

const showSoon = anchor => {
    waiting = anchor;
    if (shown?.byPointer || performance.now() - hidAt < skipWindow) {
        show(anchor, true);
        return;
    }

    later(anchor, waited => show(waited, true), bubbleOf(anchor).dataset.showDelay);
};

// The tooltip is inside the anchor, so moving onto it keeps it open, unless it's forced over the anchor and lets
// presses through. The CSS bridges the gap onto the tooltip, and the hide delay covers a path that strays off the
// bridge (WCAG 1.4.13 hoverable).
// A touch has no hover: its user gets a tooltip by focusing a text field or by pressing a trigger that does nothing
// else, so a touch neither starts a tooltip's show delay nor hides one when it leaves the trigger.
document.addEventListener('pointerover', event => {
    const anchor = anchorOf(event.target);
    if (anchor && event.pointerType !== 'touch' && !anchor.contains(event.relatedTarget)) {
        showSoon(anchor);
    }
});
document.addEventListener('pointerout', event => {
    const anchor = anchorOf(event.target);
    if (anchor && event.pointerType !== 'touch' && !anchor.contains(event.relatedTarget)) {
        later(anchor, leaving => {
            if (shown?.anchor === leaving && shown.byPointer) {
                hidAt = performance.now();
            }

            hide(leaving);
        }, bubbleOf(anchor).dataset.hideDelay);
    }
});

// Focus a click or tap leaves, or that a dialog closed by one hands back, isn't :focus-visible: its user wants the
// control, not its description. A text field is, since its user is about to type.
document.addEventListener('focusin', event => {
    const anchor = anchorOf(event.target);
    if (anchor && event.target.matches(':focus-visible')) {
        // Focus a closing popup hands back arrives mid-hide, when showPopover() throws, so the show waits a task.
        later(anchor, show, 0);
    }
});
document.addEventListener('focusout', event => hide(anchorOf(event.target)));
// A press on a trigger that's off, or on one that isn't a control, such as a relative timestamp, does nothing else,
// so it shows the tooltip, on touch too; the next press anywhere else hides it. A tooltip shown for keyboard focus stays, as focus
// does.
document.addEventListener('pointerdown', event => {
    const anchor = anchorOf(event.target);
    if (shown?.byPointer && !shown.anchor.contains(event.target)) {
        hideShown();
    }

    const trigger = anchor && described.get(bubbleOf(anchor))?.trigger;
    const pressShows = trigger && (trigger.getAttribute('aria-disabled') === 'true' || isPlain(trigger));
    if (pressShows && trigger.contains(event.target)) {
        show(anchor, true);
        return;
    }

    hide(anchor);
});

// Escape always dismisses a tooltip (WCAG 1.4.13), even one still waiting to show. The Escape that hides a shown one
// is used up, so a dialog around it stays open.
document.addEventListener('keydown', event => {
    if (isComposing(event)) {
        return;
    }

    if (event.key === 'Escape') {
        clearTimeout(timers.get(waiting));
    }

    if (event.key === 'Escape' && shown) {
        if (shown.tooltip.matches(':popover-open')) {
            event.preventDefault();
        }

        hideShown();
    }
});

// Adds the tooltip's id to the trigger's description unless it's already there, and takes it back while the
// tooltip only repeats the trigger's name (which a screen reader would then read twice) or is hidden from screen
// readers. Then it sets a TbTooltip's data-ready, which tests wait for before hovering; text marks its own once it
// is measured.
function describe(tooltip) {
    const anchor = tooltip.parentElement;
    const trigger = anchor.querySelector(focusableSelector) ?? anchor;
    const ids = describedBy(trigger);
    const repeatsName = trigger.getAttribute('aria-label')?.trim() === tooltip.textContent.trim()
        || tooltip.getAttribute('aria-hidden') === 'true';
    if (!ids.includes(tooltip.id) && !repeatsName) {
        trigger.setAttribute('aria-describedby', [...ids, tooltip.id].join(' '));
    } else if (ids.includes(tooltip.id) && repeatsName) {
        setDescribedBy(trigger, ids.filter(id => id !== tooltip.id));
    }

    const isNew = !described.has(tooltip);
    described.set(tooltip, { anchor, trigger });
    if (anchor.matches('.tb-tooltip-anchor')) {
        anchor.dataset.ready = '';
    }

    // A bubble that arrives under focus or the pointer, as when its text appears, shows as if the focus or pointer had
    // just arrived.
    const focused = anchor.ownerDocument.activeElement;
    if (isNew && anchor.contains(focused) && focused.matches(':focus-visible')) {
        show(anchor);
    } else if (isNew && anchor.matches(':hover')) {
        showSoon(anchor);
    }
}

function forget(tooltip) {
    const { anchor, trigger } = described.get(tooltip);
    described.delete(tooltip);
    clearTimeout(timers.get(anchor));
    if (shown?.tooltip === tooltip) {
        hideShown();
    }

    setDescribedBy(trigger, describedBy(trigger).filter(id => id !== tooltip.id));
    if (!bubbleOf(anchor) && anchor.matches('.tb-tooltip-anchor')) {
        delete anchor.dataset.ready;
    }
}

function describedBy(element) {
    return (element.getAttribute('aria-describedby') ?? '').split(' ').filter(Boolean);
}

function setDescribedBy(element, ids) {
    if (ids.length > 0) {
        element.setAttribute('aria-describedby', ids.join(' '));
    } else {
        element.removeAttribute('aria-describedby');
    }
}

// Bubbles come and go as tooltips are turned on and off, and Blazor rewrites aria-describedby when a trigger
// re-renders, so the page is watched for both, and so is each other window that shows a part of it.
const onChanges = records => {
    const anchors = new Set();
    for (const record of records) {
        for (const node of record.removedNodes) {
            if (node.nodeType === Node.ELEMENT_NODE) {
                [node, ...node.querySelectorAll('[data-part="tooltip"]')].filter(bubble => described.has(bubble))
                    .forEach(forget);
            }
        }

        for (const node of record.addedNodes) {
            if (node.nodeType === Node.ELEMENT_NODE) {
                [node, ...node.querySelectorAll(bubbleSelector)].filter(bubble => bubble.matches(bubbleSelector))
                    .forEach(bubble => anchors.add(bubble.parentElement));
            }
        }

        anchors.add(anchorOf(record.target));
    }

    for (const anchor of anchors) {
        const tooltip = bubbleOf(anchor);
        if (tooltip?.isConnected) {
            describe(tooltip);
        }
    }
};
onEachPage(page => {
    const observer = new MutationObserver(onChanges);
    observer.observe(page,
        { childList: true, subtree: true, attributes: true, attributeFilter: ['aria-describedby', 'aria-hidden'] });
    page.querySelectorAll(bubbleSelector).forEach(describe);
});
