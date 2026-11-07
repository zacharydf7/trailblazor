import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';

const dayMs = 24 * 60 * 60 * 1000;

const toDate = iso => new Date(`${iso}T00:00:00Z`);
const toIso = date => date.toISOString().slice(0, 10);

// Moves by months the way a calendar does: from January 31, one month on is the last day of February.
function addMonths(date, months) {
    const moved = new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + months, 1));
    const last = new Date(Date.UTC(moved.getUTCFullYear(), moved.getUTCMonth() + 1, 0)).getUTCDate();
    moved.setUTCDate(Math.min(date.getUTCDate(), last));
    return moved;
}

function target(key, shift, date, firstDayOfWeek) {
    const weekday = (date.getUTCDay() - firstDayOfWeek + 7) % 7;
    switch (key) {
        case 'ArrowLeft': return new Date(date - dayMs);
        case 'ArrowRight': return new Date(+date + dayMs);
        case 'ArrowUp': return new Date(date - 7 * dayMs);
        case 'ArrowDown': return new Date(+date + 7 * dayMs);
        case 'Home': return new Date(date - weekday * dayMs);
        case 'End': return new Date(+date + (6 - weekday) * dayMs);
        case 'PageUp': return addMonths(date, shift ? -12 : -1);
        case 'PageDown': return addMonths(date, shift ? 12 : 1);
        default: return null;
    }
}

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

    const day = iso => panel.querySelector(`[data-part="day"][data-date="${iso}"]`);
    const focusDay = button => {
        panel.querySelectorAll('[data-part="day"][tabindex="0"]').forEach(other => { other.tabIndex = -1; });
        button.tabIndex = 0;
        button.focus();
    };
    const rtl = () => getComputedStyle(panel).direction === 'rtl';
    const clearPreview = () =>
        panel.querySelectorAll('[data-preview]').forEach(day => day.removeAttribute('data-preview'));

    const onBeforeToggle = event => {
        if (event.newState === 'closed') {
            returnFocus = panel.contains(document.activeElement);
        }
    };
    const onToggle = event => {
        const open = event.newState === 'open';
        trigger.setAttribute('aria-expanded', String(open));
        if (open) {
            position = createAnchoredPosition(panel, control, { side: 'Bottom', align: 'Start', offset: 4 });
            const current = panel.querySelector('[data-part="day"][tabindex="0"]');
            current?.focus();
        } else {
            position?.dispose();
            position = null;
            clearPreview();
            if (returnFocus) {
                trigger.focus();
            }
        }

        events.invokeMethodAsync('OnToggle', open);
    };
    const onKeyDown = event => {
        const button = event.target.closest('[data-part="day"]');
        if (!button || event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        // Left and right follow the reading direction, so in right-to-left text left is the next day.
        const key = rtl() && event.key.startsWith('Arrow')
            ? { ArrowLeft: 'ArrowRight', ArrowRight: 'ArrowLeft' }[event.key] ?? event.key
            : event.key;
        const next = target(key, event.shiftKey, toDate(button.dataset.date), firstDayOfWeek);
        if (!next) {
            return;
        }

        event.preventDefault();
        const iso = toIso(next);
        const shown = day(iso);
        if (shown) {
            focusDay(shown);
        }

        events.invokeMethodAsync('OnShowDate', iso);
    };
    // Tabbing out of the calendar closes it, so it doesn't cover what focus moves to (WCAG 2.4.11).
    const onFocusOut = event => {
        if (event.relatedTarget && !panel.contains(event.relatedTarget) && panel.matches(':popover-open')) {
            panel.hidePopover();
        }
    };
    // While the start is picked and the end isn't, the days up to the one under the pointer show as the range.
    const onPointerOver = event => {
        const anchor = panel.dataset.anchor;
        const over = event.target.closest('[data-part="day"]');
        clearPreview();
        if (!anchor || !over || over.dataset.date < anchor) {
            return;
        }

        panel.querySelectorAll('[data-part="day"]').forEach(other => {
            if (other.dataset.date > anchor && other.dataset.date <= over.dataset.date) {
                other.dataset.preview = '';
            }
        });
    };

    panel.addEventListener('beforetoggle', onBeforeToggle);
    panel.addEventListener('toggle', onToggle);
    panel.addEventListener('keydown', onKeyDown);
    panel.addEventListener('focusout', onFocusOut);
    panel.addEventListener('pointerover', onPointerOver);
    return {
        today() {
            const now = new Date();
            return toIso(new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate())));
        },
        focusDate(iso) {
            const shown = day(iso);
            if (shown) {
                focusDay(shown);
            }
        },
        close() {
            if (panel.matches(':popover-open')) {
                panel.hidePopover();
            }
        },
        dispose() {
            position?.dispose();
            panel.removeEventListener('beforetoggle', onBeforeToggle);
            panel.removeEventListener('toggle', onToggle);
            panel.removeEventListener('keydown', onKeyDown);
            panel.removeEventListener('focusout', onFocusOut);
            panel.removeEventListener('pointerover', onPointerOver);
        },
    };
}
