// Inputs that act on Enter themselves, as a button does, so the grid leaves their Enter alone.
const actOnEnter = new Set(['button', 'color', 'file', 'image', 'reset', 'submit']);

// Inputs whose value Enter doesn't commit, since it isn't typed.
const untyped = new Set(['checkbox', 'radio', 'range']);

const focusable = 'a[href], button, input, select, textarea, [tabindex], [contenteditable="true"]';

// Controls that Enter or Space operates without the arrow keys; a cell holding just one hands focus to it. A menu's
// button and a row's move handle use the arrow keys, so their cells keep focus.
const takesCellFocus = element => !element.hasAttribute('aria-haspopup') && !element.closest('[data-part=handle]')
    && (element instanceof HTMLButtonElement || element instanceof HTMLAnchorElement
        || element instanceof HTMLInputElement && ['checkbox', 'radio', 'button'].includes(element.type));

// The row height to assume when no rows are drawn to measure.
const fallbackRowHeight = 40;

// A table's rows that hold cells, leaving out a virtualized grid's spacers and the rows it hides.
const linesOf = table => [...table.rows].filter(row => row.cells.length > 0 && !row.hasAttribute('aria-hidden'));

// The column a cell starts at, counting the cells before it that span several.
const startOf = cell => [...cell.parentElement.cells].slice(0, cell.cellIndex)
    .reduce((sum, other) => sum + other.colSpan, 0);

const cellAt = (row, column) => [...row.cells].find(cell => startOf(cell) + cell.colSpan > column)
    ?? row.cells[row.cells.length - 1];

export function createDataGrid(table) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!table) {
        return null;
    }

    // A checkbox can't show "some" from markup alone, so data-mixed sets it.
    const syncMixed = () => table.querySelectorAll('input[type="checkbox"][data-part="select-all"]')
        .forEach(box => { box.indeterminate = box.hasAttribute('data-mixed'); });
    const observer = new MutationObserver(syncMixed);
    observer.observe(table, { attributes: true, subtree: true, attributeFilter: ['data-mixed'] });
    syncMixed();

    // A pinned cell sticks just after the cells pinned before it on its side, so their widths are measured into the
    // variables its style reads, numbered from that side. Scroll padding as wide as the pinned cells, and as tall as
    // a header that stays in view, keeps a focused cell or a row scrolled to from landing under them.
    const headers = table.tHead.rows[0];
    const scroll = table.parentElement;
    const measurePins = () => {
        const place = (cells, name) => cells.reduce((offset, cell, slot) => {
            table.style.setProperty(`--tb-data-grid-pin-${name}${slot}`, `${offset}px`);
            return offset + cell.getBoundingClientRect().width;
        }, 0);
        const cells = [...headers.cells];
        const start = place(cells.filter(cell => cell.dataset.pinned === 'start'), '');
        const end = place(cells.filter(cell => cell.dataset.pinned === 'end').reverse(), 'end-');
        const padding = start || end ? `${start}px ${end}px` : '';
        if (scroll.style.scrollPaddingInline !== padding) {
            scroll.style.scrollPaddingInline = padding;
        }

        const top = scroll.parentElement.classList.contains('tb-data-grid--scrolls')
            ? `${table.tHead.getBoundingClientRect().height}px` : '';
        if (scroll.style.scrollPaddingBlockStart !== top) {
            scroll.style.scrollPaddingBlockStart = top;
        }
    };
    // The box is a stop for Tab only when keys have something to scroll and cell navigation doesn't scroll it for them.
    const syncScrollStop = () => {
        const stop = table.getAttribute('role') !== 'grid'
            && (scroll.scrollWidth > scroll.clientWidth || scroll.scrollHeight > scroll.clientHeight);
        if (stop) {
            scroll.setAttribute('tabindex', '0');
        } else {
            scroll.removeAttribute('tabindex');
        }

        // A region needs a name, and a grid's AriaLabel is optional.
        const name = stop ? table.getAttribute('aria-label') : null;
        if (name) {
            scroll.setAttribute('role', 'region');
            scroll.setAttribute('aria-label', name);
        } else {
            scroll.removeAttribute('role');
            scroll.removeAttribute('aria-label');
        }
    };
    const resizes = new ResizeObserver(() => {
        measurePins();
        syncScrollStop();
    });
    const watchPins = () => {
        resizes.disconnect();
        [...headers.cells].filter(cell => cell.dataset.pinned).forEach(cell => resizes.observe(cell));
        resizes.observe(scroll);
        resizes.observe(table);
        // The header always reports its size, so the padding is measured again even when nothing is left pinned.
        resizes.observe(table.tHead);
    };
    const names = new MutationObserver(syncScrollStop);
    names.observe(table, { attributes: true, attributeFilter: ['role', 'aria-label'] });
    // Changes inside a header, such as its filter menu opening, leave the pins alone. Blazor rewrites the scroll
    // box's style when the grid's height changes, which drops its padding.
    const columns = new MutationObserver(changes => {
        if (changes.some(change => change.target === headers || change.attributeName === 'data-pinned')) {
            watchPins();
        } else if (changes.some(change => change.target === scroll)) {
            measurePins();
            syncScrollStop();
        }
    });
    columns.observe(headers, { childList: true, subtree: true, attributeFilter: ['data-pinned'] });
    columns.observe(scroll, { attributeFilter: ['style'] });
    watchPins();

    const rowHeight = () => {
        const rows = [...table.tBodies[0].rows].filter(row => row.dataset.part === 'row');
        return rows.reduce((sum, row) => sum + row.getBoundingClientRect().height, 0) / rows.length || 0;
    };

    // Drawing a row can take a trip to the server, so this waits up to five seconds for it to appear.
    const waitFor = find => find() ?? new Promise(resolve => {
        const done = () => {
            watch.disconnect();
            clearTimeout(timer);
            resolve(find());
        };
        const watch = new MutationObserver(() => find() && done());
        const timer = setTimeout(done, 5000);
        watch.observe(table.tBodies[0], { childList: true, subtree: true, attributeFilter: ['data-key'] });
    });

    const navigation = createCellNavigation(table, scroll, rowHeight, waitFor);

    // Enter in a filter's input mustn't also submit a page's form around the grid. Canceling the key also cancels the
    // change event that commits typed text, so the handler sends one itself. An Enter a widget already canceled, such
    // as an autocomplete choosing an option, is left to the widget.
    const keepPageFormUnsubmitted = event => {
        const input = event.target;
        if (event.key !== 'Enter' || event.isComposing || event.defaultPrevented
            || !(input instanceof HTMLInputElement) || actOnEnter.has(input.type)
            || !input.closest('.tb-data-grid__filters, .tb-data-grid__filter-form, .tb-data-grid__editor')) {
            return;
        }

        event.preventDefault();
        event.tbGridGuarded = true;
        if (!untyped.has(input.type)) {
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }
    };
    table.addEventListener('keydown', keepPageFormUnsubmitted);

    // Escape that ends a cell's edit is marked used, so a dialog around the grid stays open; one a field has already
    // used, such as a select closing its list, is already marked.
    const keepEscapeInEdit = event => {
        if (event.key === 'Escape' && !event.defaultPrevented
            && event.target.closest?.('.tb-data-grid__editor, .tb-data-grid__row-actions')) {
            event.preventDefault();
            event.tbGridGuarded = true;
        }
    };
    table.addEventListener('keydown', keepEscapeInEdit);

    // Pressing an option in a cell's field, or a row's Save or Cancel in browsers where a click doesn't focus a
    // button, would take focus from the field, which saves the edit before the click arrives.
    const keepEditFocused = event => {
        if (event.target.closest?.('[data-part=editor] [role=listbox], [data-part=row-actions] button')) {
            event.preventDefault();
        }
    };
    table.addEventListener('pointerdown', keepEditFocused);
    return {
        // A cell whose edit ended takes focus back once its field is gone, or its place does when the edit moved
        // its row away, such as by its sort.
        focusCell(id, column) {
            const row = [...table.tBodies[0].rows].find(line => line.dataset.key === id);
            const cell = row && [...row.cells].find(other => other.dataset.column === column);
            if (cell) {
                navigation.focusCell(cell);
            } else {
                navigation.focusPlace();
            }
        },
        // A virtualized grid draws only the rows in view, so it scrolls near a row's place before the row is drawn.
        async scrollToRow(id, index) {
            const find = () => [...table.tBodies[0].rows].find(row => row.dataset.key === id);
            if (!find()) {
                scroll.scrollTop = index * rowHeight();
            }

            const row = await waitFor(find);
            row?.scrollIntoView({ block: 'nearest' });
        },
        dispose() {
            observer.disconnect();
            resizes.disconnect();
            names.disconnect();
            columns.disconnect();
            navigation.dispose();
            table.removeEventListener('keydown', keepPageFormUnsubmitted);
            table.removeEventListener('keydown', keepEscapeInEdit);
            table.removeEventListener('pointerdown', keepEditFocused);
        },
    };
}

// With cell navigation the grid is one stop for Tab; fields in popups, such as a filter menu, keep their usual keys and
// Tab order.
function createCellNavigation(table, scroll, rowHeight, waitFor) {
    const isGrid = () => table.getAttribute('role') === 'grid';
    const isVirtual = () => table.hasAttribute('aria-rowcount');
    const originalTabIndex = element => 'tbTab' in element.dataset ? element.dataset.tbTab
        : element.getAttribute('tabindex');
    const controlsOf = cell => [...cell.querySelectorAll(focusable)].filter(element =>
        element.closest('td, th') === cell && !element.closest('[popover]') && !element.disabled
        && originalTabIndex(element) !== '-1');
    const loneControl = cell => {
        const controls = controlsOf(cell);
        return controls.length === 1 && takesCellFocus(controls[0]) ? controls[0] : null;
    };
    const stopOf = cell => loneControl(cell) ?? cell;
    // A virtualized grid numbers its rows, since the rows drawn are only some of them.
    const indexOf = (row, rows) => isVirtual() ? Number(row.getAttribute('aria-rowindex')) : rows.indexOf(row) + 1;
    const rowAt = (index, rows) => isVirtual()
        ? rows.find(row => Number(row.getAttribute('aria-rowindex')) === index) : rows[index - 1];
    const rowCount = rows => isVirtual() ? Number(table.getAttribute('aria-rowcount')) : rows.length;
    let stop = null;
    let place = { key: null, index: 1, column: 0 };
    // While the grid moves focus itself, a move up or down keeps its column, even through a group's heading.
    let steering = false;
    let moves = 0;
    let holdsFocus = false;

    const makeStop = (element, cell) => {
        if (stop && stop !== element) {
            stop.tabIndex = -1;
        }

        stop = element;
        element.tabIndex = 0;
        place = { ...place, key: cell.parentElement.dataset.key ?? null,
            index: indexOf(cell.parentElement, linesOf(table)) };
    };

    const restoreTabStops = () => {
        table.querySelectorAll('[data-tb-tab]').forEach(element => {
            const before = element.dataset.tbTab;
            delete element.dataset.tbTab;
            if (before === '') {
                element.removeAttribute('tabindex');
            } else {
                element.setAttribute('tabindex', before);
            }
        });
        table.querySelectorAll('td[tabindex], th[tabindex]').forEach(cell => cell.removeAttribute('tabindex'));
        stop = null;
    };

    // Only one cell or control is a stop for Tab. A stop that is gone, disabled or no longer its cell's lone control
    // gives way to whatever now holds its place, found by its row's key or position.
    const updateTabStops = () => {
        if (!isGrid()) {
            restoreTabStops();
            return;
        }

        const rows = linesOf(table);
        let refocus = false;
        for (const row of rows) {
            for (const cell of row.cells) {
                const controls = controlsOf(cell);
                for (const control of controls) {
                    if (!('tbTab' in control.dataset)) {
                        control.dataset.tbTab = control.getAttribute('tabindex') ?? '';
                    }

                    control.tabIndex = -1;
                }

                if (controls.length === 1 && takesCellFocus(controls[0])) {
                    cell.removeAttribute('tabindex');
                } else {
                    cell.tabIndex = -1;
                }
            }
        }

        if (!stop?.isConnected || !table.contains(stop) || stop.disabled || stopOf(stop.closest('td, th')) !== stop) {
            if (stop?.isConnected) {
                stop.tabIndex = -1;
            }

            const row = rows.find(line => place.key !== null && line.dataset.key === place.key)
                ?? rowAt(place.index, rows) ?? rows.at(-1);
            stop = row ? stopOf(cellAt(row, place.column)) : null;
            // Focus that fell to the page with its row goes to what takes the row's place. A virtualized grid also drops
            // rows as it scrolls, which leaves nothing in that place and mustn't pull focus back.
            if (document.activeElement === document.body) {
                refocus = holdsFocus && (!isVirtual() || !!rowAt(place.index, rows));
                holdsFocus = refocus;
            }
        }

        if (stop) {
            stop.tabIndex = 0;
            if (refocus) {
                stop.focus();
            }
        }
    };

    // Newly drawn rows give up their tab stops before the next key can reach them; changes inside popups don't count.
    const observer = new MutationObserver(records => {
        if (records.some(record => !record.target.closest?.('[popover]'))) {
            updateTabStops();
        }
    });
    observer.observe(table, { childList: true, subtree: true, attributeFilter: ['role', 'disabled'] });
    updateTabStops();

    const focusCell = cell => {
        const element = stopOf(cell);
        makeStop(element, cell);
        steering = true;
        element.focus();
        steering = false;
    };

    // A virtualized grid scrolls to a row it hasn't drawn and waits for it; a later key, or focus moving elsewhere,
    // wins over a row that was slow to come. Focus that fell back to the page because its row scrolled out of view
    // doesn't count as moving elsewhere.
    const moveToRow = async (index, rows) => {
        const move = ++moves;
        index = Math.min(Math.max(index, 1), rowCount(rows));
        let row = rowAt(index, rows);
        if (!row && isVirtual()) {
            scroll.scrollTop = (index - table.tHead.rows.length - 1) * rowHeight();
            row = await waitFor(() => rowAt(index, linesOf(table)));
            const focused = document.activeElement;
            if (move !== moves || focused && focused !== document.body && !table.contains(focused)) {
                return;
            }
        }

        if (row) {
            focusCell(cellAt(row, place.column));
        }
    };

    const onKeyDown = event => {
        const cell = event.target.closest?.('td, th');
        if (!isGrid() || event.defaultPrevented || event.altKey || event.isComposing || !cell
            || cell.closest('table') !== table || event.target.closest('[popover]')) {
            return;
        }

        // A row's edit keeps Tab among its fields and its Save and Cancel buttons.
        if (event.key === 'Tab' && cell.parentElement.querySelector('[data-part=row-actions]')) {
            const stops = [...cell.parentElement.cells].flatMap(controlsOf)
                .filter(element => element.closest('[data-part=editor], [data-part=row-actions]'));
            const at = stops.indexOf(event.target);
            event.preventDefault();
            stops[(at + (event.shiftKey ? stops.length - 1 : 1)) % stops.length]?.focus();
            return;
        }

        // A cell being edited, or one Enter or F2 starts editing, leaves those keys to the grid.
        const editing = cell.querySelector('[data-part=editor], [data-part=row-actions]');
        if (editing && ['Enter', 'Escape', 'Tab'].includes(event.key)
            || cell.hasAttribute('data-editable') && ['Enter', 'F2'].includes(event.key)) {
            return;
        }

        if (event.target !== stopOf(cell)) {
            const controls = controlsOf(cell);
            if (event.key === 'Escape') {
                event.preventDefault();
                focusCell(cell);
            } else if (event.key === 'Tab' && controls.length > 0) {
                event.preventDefault();
                const at = controls.indexOf(event.target);
                controls[(at + (event.shiftKey ? controls.length - 1 : 1)) % controls.length].focus();
            }

            return;
        }

        // Held Enter acts once: it would click a row over and over, or delete each row as focus reaches its button.
        if (event.key === 'Enter' && event.repeat) {
            event.preventDefault();
            return;
        }

        const rows = linesOf(table);
        const row = cell.parentElement;
        const index = indexOf(row, rows);
        const rtl = getComputedStyle(table).direction === 'rtl';
        const across = cellIndex => {
            const next = row.cells[Math.min(Math.max(cellIndex, 0), row.cells.length - 1)];
            place.column = startOf(next);
            focusCell(next);
        };
        const page = () => Math.max(1, Math.floor(Math.min(scroll.clientHeight, window.innerHeight)
            / (rowHeight() || fallbackRowHeight)) - 1);
        const control = event.ctrlKey || event.metaKey;
        switch (event.key) {
            case 'ArrowRight':
                across(cell.cellIndex + (rtl ? -1 : 1));
                break;
            case 'ArrowLeft':
                across(cell.cellIndex + (rtl ? 1 : -1));
                break;
            case 'ArrowDown':
                moveToRow(index + 1, rows);
                break;
            case 'ArrowUp':
                moveToRow(index - 1, rows);
                break;
            case 'PageDown':
                moveToRow(index + page(), rows);
                break;
            case 'PageUp':
                moveToRow(index - page(), rows);
                break;
            case 'Home':
                place.column = 0;
                if (control) {
                    moveToRow(1, rows);
                } else {
                    across(0);
                }
                break;
            case 'End':
                place.column = Number.MAX_SAFE_INTEGER;
                if (control) {
                    moveToRow(rowCount(rows), rows);
                } else {
                    across(row.cells.length - 1);
                }
                break;
            case 'Enter':
            case 'F2':
                if (stopOf(cell) !== cell) {
                    return;
                }

                if (controlsOf(cell).length > 0) {
                    controlsOf(cell)[0].focus();
                    break;
                }

                if (event.key === 'F2' || row.dataset.part !== 'row') {
                    return;
                }

                // A cell with nothing to go into clicks its row.
                row.click();
                break;
            case ' ': {
                // Space clicks a body row's box to select or unselect it, but not the header's, which would select every
                // row; on any other cell it does nothing rather than scroll the page.
                if (stopOf(cell) !== cell) {
                    return;
                }

                const box = row.dataset.part === 'row' ? row.querySelector('[data-part=select] input') : null;
                box?.click();
                break;
            }
            default:
                return;
        }

        event.preventDefault();
    };

    // Focus that lands in a cell, by a click or from code, makes it the grid's stop.
    const followFocus = event => {
        const cell = event.target.closest?.('td, th');
        if (isGrid() && cell?.closest('table') === table && !event.target.closest('[popover]')) {
            holdsFocus = true;
            makeStop(stopOf(cell), cell);
            if (!steering) {
                place.column = startOf(cell);
            }
        }
    };

    // A focused element that is removed may report losing focus too, so what counts is focus that left a live element.
    const onFocusOut = event => queueMicrotask(() => {
        if (event.target.isConnected && !table.contains(document.activeElement)) {
            holdsFocus = false;
        }
    });

    table.addEventListener('keydown', onKeyDown);
    table.addEventListener('focusin', followFocus);
    table.addEventListener('focusout', onFocusOut);
    return {
        focusCell,
        focusPlace() {
            const rows = linesOf(table);
            const row = rowAt(place.index, rows) ?? rows.at(-1);
            if (row) {
                focusCell(cellAt(row, place.column));
            }
        },
        dispose() {
            observer.disconnect();
            table.removeEventListener('keydown', onKeyDown);
            table.removeEventListener('focusin', followFocus);
            table.removeEventListener('focusout', onFocusOut);
        },
    };
}
