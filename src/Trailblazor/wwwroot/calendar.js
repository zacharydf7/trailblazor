import { addMonths, browserToday, dayTarget, toDate, toIso } from './date-grid.js';

// Months and years sit in rows of four.
const columns = 4;

// The cell a key moves to in the months or years view: the arrows move along and between rows, Home and End to the ends
// of the row, and Page Up and Page Down a year, or a decade among the years.
function cellTarget(key, date, view) {
    const years = view === 'years';
    const index = years ? date.getUTCFullYear() % 10 : date.getUTCMonth();
    const last = years ? 9 : 11;
    const rowStart = index - index % columns;
    const step = {
        ArrowLeft: -1,
        ArrowRight: 1,
        ArrowUp: -columns,
        ArrowDown: columns,
        Home: rowStart - index,
        End: Math.min(rowStart + columns - 1, last) - index,
        PageUp: years ? -10 : -12,
        PageDown: years ? 10 : 12,
    }[key];
    if (step === undefined) {
        return null;
    }

    return years ? addMonths(date, step * 12) : addMonths(date, step);
}

// Moves focus to the grid's tab stop, which is rendered from the start, so it works before the script attaches.
export function focusGrid(grid, preventScroll) {
    grid?.querySelector('[data-date][tabindex="0"]')?.focus({ preventScroll });
}

// Focus moves here among the cells on show (days, months or years); the server is told, and shows another month, year
// or decade when needed.
export function createCalendar(grid, events, firstDayOfWeek) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!grid) {
        return null;
    }

    // The cell focus last moved to; keys start here, since a render can reuse the focused button for another cell.
    let intended = null;
    // Whether focus is in the grid, or was until a render removed the cell holding it.
    let inGrid = false;
    // Set while a cell stands in for one not yet shown, so taking focus doesn't change where the keys are heading.
    let standingIn = false;
    let view = grid.dataset.view;
    // Changes when the server shows the chosen date again, such as when a date picker's popup closes.
    let showing = grid.dataset.shown;

    const cell = iso => grid.querySelector(`[data-date="${iso}"]`);
    const current = () => grid.querySelector('[data-date][tabindex="0"]');
    const focusCell = button => {
        grid.querySelectorAll('[data-date][tabindex="0"]').forEach(other => { other.tabIndex = -1; });
        button.tabIndex = 0;
        button.focus();
    };
    const rtl = () => getComputedStyle(grid).direction === 'rtl';

    const onKeyDown = event => {
        const button = event.target.closest('[data-date]');
        if (!button || event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        // The button's own click would pick the cell it now shows, so the server is told the cell the keys moved to.
        if ((event.key === 'Enter' || event.key === ' ') && intended && intended !== button.dataset.date) {
            event.preventDefault();
            events.invokeMethodAsync('OnPick', intended);
            return;
        }

        // Left and right follow the reading direction, so in right-to-left text left is the next cell.
        const key = rtl() && event.key.startsWith('Arrow')
            ? { ArrowLeft: 'ArrowRight', ArrowRight: 'ArrowLeft' }[event.key] ?? event.key
            : event.key;
        const from = toDate(intended ?? button.dataset.date);
        const next = view === 'days' ? dayTarget(key, event.shiftKey, from, firstDayOfWeek) : cellTarget(key, from, view);
        // A date can't have a year before 1 or after 9999.
        if (!next || next.getUTCFullYear() < 1 || next.getUTCFullYear() > 9999) {
            return;
        }

        event.preventDefault();
        const iso = toIso(next);
        intended = iso;
        const shown = cell(iso);
        if (shown) {
            focusCell(shown);
        }

        events.invokeMethodAsync('OnShowDate', iso);
    };
    // A render can remove or reuse the focused cell, or show another view, so focus goes to the cell it belongs on.
    const renders = new MutationObserver(() => {
        const viewChanged = grid.dataset.view !== view || grid.dataset.shown !== showing;
        if (viewChanged) {
            view = grid.dataset.view;
            showing = grid.dataset.shown;
            intended = null;
        }

        const focused = document.activeElement;
        const lost = inGrid && focused === document.body;
        const onACell = grid.contains(focused) && focused.matches('[data-date]');
        const shown = intended ? cell(intended) : null;
        if (shown && shown !== focused && (onACell || lost)) {
            focusCell(shown);
        } else if (!shown && (lost || (viewChanged && onACell))) {
            // Until the cell's month or decade shows, another cell holds focus so the keys still reach the grid.
            standingIn = true;
            current()?.focus();
            standingIn = false;
        }
    });
    renders.observe(grid, { subtree: true, childList: true, attributes: true, attributeFilter: ['data-date', 'data-view', 'data-shown'] });

    const onFocusIn = event => {
        const focused = event.target.closest('[data-date]');
        inGrid = Boolean(focused);
        if (!standingIn) {
            intended = focused?.dataset.date ?? intended;
        }
    };
    const onFocusOut = event => {
        if (event.relatedTarget && !grid.contains(event.relatedTarget)) {
            inGrid = false;
        }
    };
    // Safari doesn't focus a clicked button, so a click notes the picked cell.
    const onClick = event => {
        const clicked = event.target.closest('[data-date]');
        if (clicked) {
            intended = clicked.dataset.date;
            inGrid = true;
        }
    };
    // A press outside the grid moves the user on, so a render that follows leaves focus alone.
    const onPointerDown = event => {
        if (!grid.contains(event.target)) {
            inGrid = false;
        }
    };
    // A press on a blank part of the grid leaves focus where it is, so the keys go on working.
    const onGridPointerDown = event => {
        if (!event.target.closest('button')) {
            event.preventDefault();
        }
    };

    grid.addEventListener('keydown', onKeyDown);
    grid.addEventListener('focusin', onFocusIn);
    grid.addEventListener('focusout', onFocusOut);
    grid.addEventListener('click', onClick);
    grid.addEventListener('pointerdown', onGridPointerDown);
    document.addEventListener('pointerdown', onPointerDown, true);

    return {
        today: browserToday,
        dispose() {
            renders.disconnect();
            grid.removeEventListener('keydown', onKeyDown);
            grid.removeEventListener('focusin', onFocusIn);
            grid.removeEventListener('focusout', onFocusOut);
            grid.removeEventListener('click', onClick);
            grid.removeEventListener('pointerdown', onGridPointerDown);
            document.removeEventListener('pointerdown', onPointerDown, true);
        },
    };
}
