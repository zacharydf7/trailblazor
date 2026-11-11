import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { browserToday, dayTarget, toDate, toIso } from './date-grid.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';

// The calendar's focus moves here among the days on show; the server is told, and shows another month when needed.
export function createDateRangePicker(panel, events, firstDayOfWeek) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!panel) {
        return null;
    }

    const control = panel.closest('[data-part="control"]');
    const trigger = control.querySelector(`[aria-controls="${panel.id}"]`);
    let position = null;
    let returnFocus = false;
    // The replay at attach and a toggle still queued from before it can both report one opening.
    let opened = false;
    // Set from a press on the calendar button until the calendar next opens or closes.
    let pressedTrigger = false;
    // The day focus last moved to; keys start here, since a render can reuse the focused button for another day.
    let intended = null;
    // The day the pointer or focus last rested on, previewed again when the start arrives from the server.
    let over = null;
    let standingIn = false;

    const day = iso => panel.querySelector(`[data-part="day"][data-date="${iso}"]`);
    const focusDay = button => {
        panel.querySelectorAll('[data-part="day"][tabindex="0"]').forEach(other => { other.tabIndex = -1; });
        button.tabIndex = 0;
        button.focus();
    };
    const rtl = () => getComputedStyle(panel).direction === 'rtl';
    const clearPreview = () =>
        panel.querySelectorAll('[data-preview]').forEach(day => day.removeAttribute('data-preview'));
    const dropPreview = () => {
        over = null;
        clearPreview();
    };

    const onBeforeToggle = event => {
        if (event.newState === 'closed') {
            returnFocus = panel.contains(document.activeElement);
        }
    };
    const onToggle = event => {
        const open = event.newState === 'open';
        if (open === opened) {
            return;
        }

        opened = open;
        pressedTrigger = false;
        trigger.setAttribute('aria-expanded', String(open));
        if (open) {
            position = createAnchoredPosition(panel, control, { side: 'Bottom', align: 'Start', offset: 4 });
            const current = panel.querySelector('[data-part="day"][tabindex="0"]');
            current?.focus();
        } else {
            position?.dispose();
            position = null;
            intended = null;
            dropPreview();
            if (returnFocus) {
                trigger.focus();
            }
        }

        callDotNet(events, 'OnToggle', open);
    };
    const onKeyDown = event => {
        const button = event.target.closest('[data-part="day"]');
        if (!button || event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        // The button's own click would pick the day it now shows, so the server is told the day the keys moved to.
        if ((event.key === 'Enter' || event.key === ' ') && intended && intended !== button.dataset.date) {
            event.preventDefault();
            callDotNet(events, 'OnPick', intended);
            return;
        }

        // Left and right follow the reading direction, so in right-to-left text left is the next day.
        const key = rtl() && event.key.startsWith('Arrow')
            ? { ArrowLeft: 'ArrowRight', ArrowRight: 'ArrowLeft' }[event.key] ?? event.key
            : event.key;
        const next = dayTarget(key, event.shiftKey, toDate(intended ?? button.dataset.date), firstDayOfWeek);
        if (!next) {
            return;
        }

        event.preventDefault();
        const iso = toIso(next);
        intended = iso;
        const shown = day(iso);
        if (shown) {
            focusDay(shown);
        }

        callDotNet(events, 'OnShowDate', iso);
    };
    // Tabbing out of the calendar closes it, so it doesn't cover what focus moves to (WCAG 2.4.11). A press on the
    // calendar button doesn't close it here, since the button's click would then open it again.
    const onFocusOut = event => {
        if (event.relatedTarget && !panel.contains(event.relatedTarget) && !pressedTrigger
            && panel.matches(':popover-open')) {
            panel.hidePopover();
        }
    };
    // While only the start is picked, the days up to the one under the pointer or focus show as the range.
    const previewTo = to => {
        const anchor = panel.dataset.anchor;
        over = to;
        clearPreview();
        if (!anchor || !to || to.dataset.date < anchor) {
            return;
        }

        panel.querySelectorAll('[data-part="day"]').forEach(other => {
            if (other.dataset.date > anchor && other.dataset.date <= to.dataset.date) {
                other.dataset.preview = '';
            }
        });
    };
    // A render can remove or reuse the focused button, or set the range's start, so focus and the preview are fixed.
    const renders = new MutationObserver(() => {
        const focused = document.activeElement;
        const shown = intended && day(intended);
        const onADay = panel.contains(focused) && focused.matches('[data-part="day"]');
        const lost = intended && focused === document.body && panel.matches(':popover-open');
        if (shown && shown !== focused && (onADay || lost)) {
            focusDay(shown);
        } else if (!shown && lost) {
            // Until the day's month shows, another day holds focus so the keys still reach the calendar.
            standingIn = true;
            panel.querySelector('[data-part="day"][tabindex="0"]')?.focus();
            standingIn = false;
        }

        previewTo(over);
    });
    renders.observe(panel, { subtree: true, childList: true, attributes: true,
        attributeFilter: ['data-date', 'data-anchor'] });

    const onPreviewMove = event => previewTo(event.target.closest('[data-part="day"]'));
    const onFocusIn = event => {
        if (standingIn) {
            return;
        }

        const focused = event.target.closest('[data-part="day"]');
        intended = focused?.dataset.date ?? intended;
        previewTo(focused);
    };
    // Safari doesn't focus a clicked button, so a click notes the picked day, or that focus left the days.
    const onClick = event => {
        if (event.target.closest('button')) {
            intended = event.target.closest('[data-part="day"]')?.dataset.date ?? null;
        }
    };
    // A press on a blank part of the calendar leaves focus where it is, so the keys go on working.
    const onPointerDown = event => {
        if (!event.target.closest('button')) {
            event.preventDefault();
        }
    };
    const onTriggerPointerDown = () => {
        pressedTrigger = true;
    };

    panel.addEventListener('beforetoggle', onBeforeToggle);
    panel.addEventListener('toggle', onToggle);
    panel.addEventListener('keydown', onKeyDown);
    panel.addEventListener('focusout', onFocusOut);
    panel.addEventListener('pointerover', onPreviewMove);
    panel.addEventListener('focusin', onFocusIn);
    panel.addEventListener('click', onClick);
    panel.addEventListener('pointercancel', dropPreview);
    panel.addEventListener('pointerdown', onPointerDown);
    trigger.addEventListener('pointerdown', onTriggerPointerDown);
    // A calendar clicked open before this script attached missed its toggle, so it is replayed.
    if (panel.matches(':popover-open')) {
        onToggle({ newState: 'open' });
    }

    return {
        today: browserToday,
        // Opens or closes the calendar for the parent's Open.
        setOpen(open) {
            if (open !== panel.matches(':popover-open')) {
                panel.togglePopover(open);
            }
        },
        dispose() {
            position?.dispose();
            renders.disconnect();
            panel.removeEventListener('beforetoggle', onBeforeToggle);
            panel.removeEventListener('toggle', onToggle);
            panel.removeEventListener('keydown', onKeyDown);
            panel.removeEventListener('focusout', onFocusOut);
            panel.removeEventListener('pointerover', onPreviewMove);
            panel.removeEventListener('focusin', onFocusIn);
            panel.removeEventListener('click', onClick);
            panel.removeEventListener('pointercancel', dropPreview);
            panel.removeEventListener('pointerdown', onPointerDown);
            trigger.removeEventListener('pointerdown', onTriggerPointerDown);
        },
    };
}
