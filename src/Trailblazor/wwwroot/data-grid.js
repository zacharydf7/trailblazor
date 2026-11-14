import { createPointerDrag } from '../Trailblazor.Primitives/pointer-drag.js';
import { isComposing } from '../Trailblazor.Primitives/composing.js';

// Inputs that act on Enter themselves, as a button does, so the grid leaves their Enter alone.
const actOnEnter = new Set(['button', 'color', 'file', 'image', 'reset', 'submit']);

// Inputs whose value Enter doesn't commit, since it isn't typed.
const untyped = new Set(['checkbox', 'radio', 'range']);

// Canceling a key also cancels the change event that commits a field's typed text, so this sends one itself.
const cancelAndCommit = (event, input) => {
    event.preventDefault();
    event.tbGridGuarded = true;
    if (input instanceof HTMLTextAreaElement || input instanceof HTMLInputElement && !untyped.has(input.type)) {
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }
};

const focusable = 'a[href], button, input, select, textarea, [tabindex], [contenteditable="true"]';

// Controls that Enter or Space operates without the arrow keys; a cell holding just one hands focus to it. A menu's
// button and a row's move handle use the arrow keys, so their cells keep focus.
const takesCellFocus = element => !element.hasAttribute('aria-haspopup') && !element.closest('[data-part=handle]')
    && (element instanceof HTMLButtonElement || element instanceof HTMLAnchorElement
        || element instanceof HTMLInputElement && ['checkbox', 'radio', 'button'].includes(element.type));

// The row height to assume when no rows are drawn to measure.
const fallbackRowHeight = 40;

// The row of column headers, under any rows of group headings, even before the grid has any columns.
const headerRowOf = table => [...table.tHead.rows].find(row => row.dataset.part !== 'bands');

// A table's rows that hold cells, leaving out a virtualized grid's spacers and the rows it hides.
const linesOf = table => [...table.rows].filter(row => row.cells.length > 0 && !row.hasAttribute('aria-hidden'));
const isGridRole = table => ['grid', 'treegrid'].includes(table.getAttribute('role'));
const selectsMany = table => table.dataset.selection === 'multiple';
const extendTo = (row, from) => row.dispatchEvent(new CustomEvent('tb-row-extend', { bubbles: true, detail: from }));

// The column a cell starts at, counting the cells before it that span several.
const startOf = cell => [...cell.parentElement.cells].slice(0, cell.cellIndex)
    .reduce((sum, other) => sum + other.colSpan, 0);

const cellAt = (row, column) => [...row.cells].find(cell => startOf(cell) + cell.colSpan > column)
    ?? row.cells[row.cells.length - 1];

// A grid that draws only the columns in view keeps its unpinned columns' keys and widths on the table. They start where
// the first spacer does, after the columns pinned at the start.
const drawsColumnsInView = table => table.hasAttribute('data-virtual-columns');
const unpinnedKeys = table => JSON.parse(table.dataset.columnKeys ?? '[]');
const unpinnedWidths = table => (table.dataset.columnWidths ?? '').split(',').filter(Boolean).map(Number);
// Measured from the inline start, which is the right edge in a right-to-left grid.
const unpinnedStart = table => {
    const spacer = headerRowOf(table).querySelector(':scope > [data-part=column-spacer]');
    if (!spacer) {
        return 0;
    }

    return getComputedStyle(table).direction === 'rtl' ? table.offsetWidth - spacer.offsetLeft - spacer.offsetWidth
        : spacer.offsetLeft;
};
const isSpacer = cell => cell?.dataset.part === 'column-spacer';

// The first and last unpinned columns the box shows, by place among them.
const columnsInView = (table, scroll) => {
    const widths = unpinnedWidths(table);
    // A right-to-left box scrolls into negative values, so the distance from the inline start is what counts.
    const left = Math.abs(scroll.scrollLeft);
    const right = left + scroll.clientWidth;
    let [x, first, last] = [unpinnedStart(table), -1, widths.length - 1];
    for (let at = 0; at < widths.length; at++) {
        if (first < 0 && x + widths[at] > left) {
            first = at;
        }

        if (x >= right) {
            last = Math.max(at - 1, 0);
            break;
        }

        x += widths[at];
    }

    return [Math.max(first, 0), last];
};

// A spreadsheet pastes either tab-separated lines, with a field that holds a tab, line break or quote put in quotes, or
// an HTML table.
const tsvField = text => /[\t\r\n"]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
const escapeHtml = text => text.replace(/[&<>"]/g, letter =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[letter]);
const copyRows = rows => {
    const text = rows.map(row => row.map(tsvField).join('\t')).join('\r\n');
    const html = `<table>${rows.map(row => `<tr>${row.map(cell => `<td>${escapeHtml(cell)}</td>`).join('')}</tr>`)
        .join('')}</table>`;
    const fill = event => {
        event.clipboardData.setData('text/plain', text);
        event.clipboardData.setData('text/html', html);
        event.preventDefault();
    };
    // Some browsers copy only with something selected, so a hidden node holds the text while the copy runs.
    const holder = document.createElement('span');
    holder.className = 'tb-visually-hidden';
    holder.textContent = text || ' ';
    document.body.append(holder);
    const selection = document.getSelection();
    selection.selectAllChildren(holder);
    document.addEventListener('copy', fill, { capture: true });
    try {
        document.execCommand('copy');
    } finally {
        document.removeEventListener('copy', fill, { capture: true });
        selection.removeAllRanges();
        holder.remove();
    }
};

// With rows that move, the grid is ready only once their reordering is wired too, and the server says so through
// setReady.
export function createDataGrid(table, waitForReorder) {
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
    // a header and totals row that stay in view, keeps a focused cell or a row scrolled to from landing under them.
    const headers = headerRowOf(table);
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

        const scrolls = scroll.parentElement.classList.contains('tb-data-grid--scrolls');
        const top = scrolls ? `${table.tHead.getBoundingClientRect().height}px` : '';
        if (scroll.style.scrollPaddingBlockStart !== top) {
            scroll.style.scrollPaddingBlockStart = top;
        }

        // A totals row kept in view needs padding too; adding one resizes the watched table, so this runs again.
        const bottom = scrolls && table.tFoot ? `${table.tFoot.getBoundingClientRect().height}px` : '';
        if (scroll.style.scrollPaddingBlockEnd !== bottom) {
            scroll.style.scrollPaddingBlockEnd = bottom;
        }
    };
    // The box is a stop for Tab only when keys have something to scroll and cell navigation doesn't scroll it for them.
    const syncScrollStop = () => {
        const stop = !isGridRole(table)
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

    const navigation = createCellNavigation(table, scroll, rowHeight, waitFor, () => reportAfterReveal());

    // The server draws only the unpinned columns the box shows, so tell it which those are whenever they differ from
    // the ones it last drew.
    let sentWindow = '';
    let sentRem = 0;
    const reportColumnWindow = () => {
        if (!drawsColumnsInView(table) || unpinnedWidths(table).length === 0 || navigation.revealing()) {
            return;
        }

        const [first, last] = columnsInView(table, scroll);
        const shown = `${first},${last}`;
        const remPixels = parseFloat(getComputedStyle(document.documentElement).fontSize);
        if ((shown !== table.dataset.columnWindow && shown !== sentWindow) || remPixels !== sentRem) {
            sentWindow = shown;
            sentRem = remPixels;
            table.dispatchEvent(new CustomEvent('tb-column-window', { bubbles: true,
                detail: { first, last, remPixels } }));
        }
    };
    let windowFrame = 0;
    const onHorizontalScroll = () => {
        cancelAnimationFrame(windowFrame);
        windowFrame = requestAnimationFrame(reportColumnWindow);
    };
    // A reveal draws only the columns around its own, so once it ends the box's window is sent again, even when it's
    // the one sent last.
    const reportAfterReveal = () => {
        sentWindow = '';
        onHorizontalScroll();
    };
    scroll.addEventListener('scroll', onHorizontalScroll, { passive: true });
    const windowWidths = new MutationObserver(reportColumnWindow);
    windowWidths.observe(table, { attributes: true, attributeFilter: ['data-column-widths', 'data-virtual-columns'] });
    const windowResizes = new ResizeObserver(onHorizontalScroll);
    windowResizes.observe(scroll);

    // A resize reports every column's width, so the columns the user didn't touch keep the widths they have.
    const columnHeaders = () => [...headers.cells].filter(cell => cell.dataset.column);
    const minWidth = cell => parseFloat(getComputedStyle(cell).minInlineSize) || 0;
    const reportWidths = (changed, width) => {
        const widths = Object.fromEntries(columnHeaders().map(cell => [cell.dataset.column,
            cell === changed ? Math.max(width, minWidth(cell)) : cell.getBoundingClientRect().width]));
        table.dispatchEvent(new CustomEvent('tb-column-widths', { bubbles: true, detail: { widths } }));
    };

    // A grid with group headings sizes its columns through its <col>s too, since a fixed table reads its first row.
    const setWidth = (header, width) => {
        header.style.width = width;
        const column = [...table.querySelectorAll(':scope > colgroup > col')]
            .find(col => col.dataset.column === header.dataset.column);
        column?.style.setProperty('width', width);
    };

    // A drag fixes every column at its width first, so only the dragged edge moves while it lasts.
    let drag = null;
    const startResize = event => {
        const edge = event.target.closest?.('[data-part=resize]');
        if (!edge || event.button !== 0 || edge.closest('table') !== table) {
            return;
        }

        event.preventDefault();
        const cell = edge.closest('th');
        const widths = columnHeaders().map(other => [other, other.getBoundingClientRect().width]);
        widths.forEach(([other, width]) => setWidth(other, `${width}px`));
        table.classList.add('tb-data-grid__table--fixed');
        const rtl = getComputedStyle(table).direction === 'rtl';
        drag = { cell, edge, x: event.clientX, width: cell.getBoundingClientRect().width, rtl };
        edge.setPointerCapture(event.pointerId);
        edge.setAttribute('data-dragging', '');
    };
    const moveResize = event => {
        if (drag) {
            const delta = (event.clientX - drag.x) * (drag.rtl ? -1 : 1);
            setWidth(drag.cell, `${Math.max(drag.width + delta, minWidth(drag.cell))}px`);
        }
    };
    const endResize = () => {
        if (drag) {
            const { cell, edge } = drag;
            drag = null;
            edge.removeAttribute('data-dragging');
            reportWidths(cell, cell.getBoundingClientRect().width);
        }
    };
    table.addEventListener('pointerdown', startResize);
    table.addEventListener('pointermove', moveResize);
    table.addEventListener('pointerup', endResize);
    table.addEventListener('pointercancel', endResize);

    // A dragged header moves its column among those pinned to its side, and Escape puts it back.
    let carried = null;
    let carriedX = 0;
    const sideOf = cell => columnHeaders().filter(other => other.dataset.pinned === cell.dataset.pinned);
    const passed = (x, cell) => {
        const box = cell.getBoundingClientRect();
        const middle = box.left + box.width / 2;
        return getComputedStyle(table).direction === 'rtl' ? x < middle : x > middle;
    };
    const clearDrop = () =>
        table.querySelectorAll('thead th[data-drop]').forEach(cell => cell.removeAttribute('data-drop'));
    const others = () => sideOf(carried).filter(other => other !== carried);
    const markDrop = () => {
        clearDrop();
        const rest = others();
        const at = rest.filter(other => passed(carriedX, other)).length;
        if (rest.length > 0) {
            (rest[at] ?? rest.at(-1)).setAttribute('data-drop', at < rest.length ? 'before' : 'after');
        }
    };
    const putDown = () => {
        clearDrop();
        carried?.removeAttribute('data-moving');
    };
    const reportOrder = () => {
        const side = sideOf(carried);
        const rest = others();
        rest.splice(rest.filter(other => passed(carriedX, other)).length, 0, carried);
        const keys = columnHeaders().map(other => other.dataset.column);
        const order = keys.map(key => side.some(other => other.dataset.column === key)
            ? rest.shift().dataset.column : key);
        if (order.some((key, at) => key !== keys[at])) {
            table.dispatchEvent(new CustomEvent('tb-column-order', { bubbles: true, detail: { order } }));
        }
    };
    const columnDrag = createPointerDrag(table, {
        itemSelector: 'thead th[data-column]',
        ignoreSelector: '[data-part=resize], [data-part=column-menu], [data-part=filter-menu], input',
        ownsPress: target => target.closest('table') === table && table.hasAttribute('data-reorder-columns') && !drag,
        scrolls: false,
        onStart: cell => {
            carried = cell;
            cell.setAttribute('data-moving', '');
        },
        onMove: event => {
            carriedX = event.clientX;
            markDrop();
        },
        onDrop: () => {
            putDown();
            reportOrder();
            carried = null;
        },
        onCancel: () => {
            putDown();
            carried = null;
        },
    });

    // Virtualize can measure rows after a jump and redraw them elsewhere, so a row scrolled to is kept in view until
    // the rows settle, the person scrolls, clicks or presses a key, or another row is asked for, at most five seconds.
    let stopKeeping = () => {};
    let scrolls = 0;
    const keepInView = (find, index) => {
        const place = () => {
            const row = find();
            if (row) {
                row.scrollIntoView({ block: 'nearest' });
                return;
            }

            const anchor = [...table.tBodies[0].rows].find(other => other.hasAttribute('aria-rowindex'));
            if (anchor) {
                const away = table.tHead.rows.length + index + 1 - Number(anchor.getAttribute('aria-rowindex'));
                scroll.scrollTop += anchor.getBoundingClientRect().top - scroll.getBoundingClientRect().top
                    - table.tHead.offsetHeight + away * rowHeight();
            }
        };
        const inputs = ['wheel', 'touchstart', 'pointerdown', 'keydown'];
        let quiet;
        const stop = () => {
            watch.disconnect();
            clearTimeout(quiet);
            clearTimeout(limit);
            inputs.forEach(name => scroll.removeEventListener(name, stop));
            if (stopKeeping === stop) {
                stopKeeping = () => {};
            }
        };
        const limit = setTimeout(stop, 5000);
        const settle = () => {
            clearTimeout(quiet);
            quiet = setTimeout(stop, 1000);
        };
        const watch = new MutationObserver(() => {
            place();
            settle();
        });
        watch.observe(table.tBodies[0], { childList: true, subtree: true, attributeFilter: ['data-key', 'style'] });
        inputs.forEach(name => scroll.addEventListener(name, stop, { passive: true }));
        stopKeeping = stop;
        settle();
    };

    // Enter in a filter's input mustn't also submit a page's form around the grid. An Enter a widget already canceled,
    // such as an autocomplete choosing an option, is left to the widget.
    const keepPageFormUnsubmitted = event => {
        const input = event.target;
        if (event.key !== 'Enter' || isComposing(event) || event.defaultPrevented
            || !(input instanceof HTMLInputElement) || actOnEnter.has(input.type)
            || !input.closest('.tb-data-grid__filters, .tb-data-grid__filter-form, .tb-data-grid__editor')) {
            return;
        }

        cancelAndCommit(event, input);
    };
    table.addEventListener('keydown', keepPageFormUnsubmitted);

    // Escape that ends a cell's edit is marked used, so a dialog around the grid stays open; one a field has already
    // used, such as a select closing its list, is already marked.
    const keepEscapeInEdit = event => {
        if (event.key === 'Escape' && !event.defaultPrevented && !isComposing(event)
            && event.target.closest?.('.tb-data-grid__editor, .tb-data-grid__row-actions')) {
            event.preventDefault();
            event.tbGridGuarded = true;
        }
    };
    table.addEventListener('keydown', keepEscapeInEdit);

    // Only Enter and Escape reach a cell's edit, saying whether its field used the key; the grid's own guards don't
    // count. Blazor listens on the document, so the window hears the key after a field's handlers have run.
    const sendEditKey = event => {
        const edit = event.target.closest?.('.tb-data-grid__editor, .tb-data-grid__row-actions');
        if ((event.key === 'Enter' || event.key === 'Escape') && edit?.closest('table') === table) {
            edit.dispatchEvent(new CustomEvent('tb-edit-key', {
                bubbles: true,
                detail: {
                    key: event.key,
                    isComposing: isComposing(event),
                    handled: event.defaultPrevented && !event.tbGridGuarded,
                },
            }));
        }
    };
    window.addEventListener('keydown', sendEditKey);

    // Pressing an option in a cell's field, or a row's Save or Cancel in browsers where a click doesn't focus a
    // button, would take focus from the field, which saves the edit before the click arrives.
    const keepEditFocused = event => {
        if (event.target.closest?.('[data-part=editor] [role=listbox], [data-part=row-actions] button')) {
            event.preventDefault();
        }
    };
    table.addEventListener('pointerdown', keepEditFocused);

    // Shift on a row selects a range, not the text it crosses. Canceling the selection, not the press, leaves focus to
    // move as usual, and editable text keeps its own.
    let pressedWithShift = false;
    const notePress = event => pressedWithShift = event.shiftKey;
    const endPress = () => pressedWithShift = false;
    const keepTextUnselected = event => {
        const target = event.target.nodeType === Node.ELEMENT_NODE ? event.target : event.target.parentElement;
        if (pressedWithShift && table.hasAttribute('data-row-click-selects') && target.closest('table') === table
            && target.closest('tr')?.dataset.part === 'row' && !target.closest('[contenteditable]')) {
            event.preventDefault();
        }
    };
    // A Shift+click on a box goes through the range, since its own toggle would turn off a box already in the range.
    // The range waits for the click to finish, since canceling it would undo a render that came during it.
    const extendFromBox = event => {
        if (event.shiftKey && selectsMany(table) && event.target.matches?.('tr[data-part=row] [data-part=select] input')
            && event.target.closest('table') === table) {
            event.preventDefault();
            setTimeout(() => extendTo(event.target.closest('tr'), null));
        }
    };
    table.addEventListener('mousedown', notePress);
    document.addEventListener('mouseup', endPress);
    table.addEventListener('selectstart', keepTextUnselected);
    table.addEventListener('click', extendFromBox);
    const setReady = ready => table.closest('[data-part="grid"]')?.toggleAttribute('data-ready', ready);
    setReady(!waitForReorder);
    return {
        setReady,
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
            const scrolling = ++scrolls;
            stopKeeping();
            const find = () => [...table.tBodies[0].rows].find(row => row.dataset.key === id);
            if (!find()) {
                scroll.scrollTop = index * rowHeight();
            }

            const row = await waitFor(find);
            if (scrolling !== scrolls) {
                return;
            }

            row?.scrollIntoView({ block: 'nearest' });
            if (row) {
                keepInView(find, index);
            }
        },
        focusColumnMenu(key) {
            columnHeaders().find(cell => cell.dataset.column === key)
                ?.querySelector('[data-part=column-menu] button')?.focus();
        },
        focusColumnChooser() {
            table.closest('[data-part=grid]')?.querySelector('[data-part=column-chooser] [data-part=trigger]')?.focus();
        },
        resizeColumn(key, delta) {
            const cell = columnHeaders().find(other => other.dataset.column === key);
            if (cell) {
                reportWidths(cell, cell.getBoundingClientRect().width + delta);
            }
        },
        dispose() {
            scrolls++;
            table.removeEventListener('pointerdown', startResize);
            table.removeEventListener('pointermove', moveResize);
            table.removeEventListener('pointerup', endResize);
            table.removeEventListener('pointercancel', endResize);
            columnDrag.dispose();
            stopKeeping();
            observer.disconnect();
            resizes.disconnect();
            names.disconnect();
            columns.disconnect();
            scroll.removeEventListener('scroll', onHorizontalScroll);
            cancelAnimationFrame(windowFrame);
            windowWidths.disconnect();
            windowResizes.disconnect();
            navigation.dispose();
            table.removeEventListener('keydown', keepPageFormUnsubmitted);
            table.removeEventListener('keydown', keepEscapeInEdit);
            window.removeEventListener('keydown', sendEditKey);
            table.removeEventListener('pointerdown', keepEditFocused);
            table.removeEventListener('mousedown', notePress);
            document.removeEventListener('mouseup', endPress);
            table.removeEventListener('selectstart', keepTextUnselected);
            table.removeEventListener('click', extendFromBox);
        },
    };
}

// With cell navigation the grid is one stop for Tab; fields in popups, such as a filter menu, keep their usual keys and
// Tab order.
function createCellNavigation(table, scroll, rowHeight, waitFor, revealed) {
    const isGrid = () => isGridRole(table);
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
    // A row's details can hold anything, even another grid, so they keep their own Tab stops.
    const isDetail = cell => cell.parentElement.dataset.part === 'detail';
    const firstStopIn = cell => [...cell.querySelectorAll(focusable)].find(element => !element.disabled
        && !element.closest('[popover]') && element.tabIndex >= 0);
    const stopOf = cell => isDetail(cell) ? cell : loneControl(cell) ?? cell;
    // A virtualized grid numbers its rows, since the rows drawn are only some of them.
    const indexOf = (row, rows) => isVirtual() ? Number(row.getAttribute('aria-rowindex')) : rows.indexOf(row) + 1;
    const rowAt = (index, rows) => isVirtual()
        ? rows.find(row => Number(row.getAttribute('aria-rowindex')) === index) : rows[index - 1];
    const rowCount = rows => isVirtual() ? Number(table.getAttribute('aria-rowcount')) : rows.length;
    let stop = null;
    // Until a cell takes focus, the stop is on the column headers, found each time since columns can come later.
    let place = { key: null, index: null, column: 0 };
    const placeIndex = rows => place.index ?? (indexOf(headerRowOf(table), rows) || 1);
    // While the grid moves focus itself, a move up or down keeps its column, even through a group's heading.
    let steering = false;
    let moves = 0;
    let holdsFocus = false;
    // While a column is scrolled into view, the cell that had focus can leave with the columns behind it; focus waits
    // for the column rather than falling back to the place it left.
    let revealing = false;

    // A grid that selects cells keeps the cell its range started at; the server marks the cells from it to focus.
    const selectsCells = () => table.hasAttribute('data-cell-selection');
    let anchor = null;
    let end = null;
    let ranged = false;
    let extending = false;
    const bodyCellOf = target => {
        const cell = target?.closest?.('td');
        return cell?.closest('table') === table && cell.parentElement.dataset.part === 'row' && cell.dataset.column
            ? cell : null;
    };
    const spotOf = cell => ({ row: cell.parentElement.dataset.key, column: cell.dataset.column });
    // When a filter, a page change or a removal takes away the row the range started on, or hiding takes away its
    // column, the range starts again where focus is. In a virtualized grid that still shows the range, the row has only
    // scrolled out of view.
    const startsAgain = () => !ranged
        || ![...headerRowOf(table).cells].some(cell => cell.dataset.column === anchor.column)
        || !linesOf(table).some(row => row.dataset.key === anchor.row)
            && (!isVirtual() || !table.hasAttribute('data-cell-range'));
    // A cell drawn by a template copies its field's text, as a range does.
    const copyTextOf = cell => cell.dataset.copy ?? cell.textContent.trim();
    const reportRange = to => {
        end = to;
        ranged = to.row !== anchor.row || to.column !== anchor.column;
        table.dispatchEvent(new CustomEvent('tb-cell-range', { bubbles: true, detail: { fromRow: anchor.row,
            fromColumn: anchor.column, toRow: to.row, toColumn: to.column } }));
    };
    const clearRange = () => {
        if (ranged) {
            ranged = false;
            table.dispatchEvent(new CustomEvent('tb-cell-range', { bubbles: true, detail: {} }));
        }
    };
    // The server's copy holds rows a virtualized grid hasn't drawn, but until it has caught up with the range last
    // reported, the drawn cells of that range are copied instead, since the copy can't wait.
    const nameOf = spot => `${spot.row}:${spot.column}`;
    const drawnRange = () => {
        const rows = linesOf(table).filter(row => row.dataset.part === 'row');
        const [top, bottom] = [anchor, end].map(spot => rows.findIndex(row => row.dataset.key === spot.row));
        const columns = [...headerRowOf(table).cells].map(cell => cell.dataset.column).filter(Boolean);
        const [start, stop] = [anchor, end].map(spot => columns.indexOf(spot.column));
        if (top < 0 || bottom < 0 || start < 0 || stop < 0) {
            return null;
        }

        const keys = columns.slice(Math.min(start, stop), Math.max(start, stop) + 1);
        return rows.slice(Math.min(top, bottom), Math.max(top, bottom) + 1).map(row => keys.map(key =>
            [...row.cells].find(one => one.dataset.column === key)).map(one => one ? copyTextOf(one) : ''));
    };
    const copyCells = cell => {
        const copy = table.closest('[data-part=grid]')?.querySelector(':scope > [data-part=copy]');
        const current = copy?.dataset.from === nameOf(anchor ?? {}) && copy?.dataset.to === nameOf(end ?? {});
        const range = !ranged ? null
            : current ? [...copy.rows].map(row => [...row.cells].map(one => one.textContent)) : drawnRange();
        copyRows(range ?? [[copyTextOf(cell)]]);
        table.dispatchEvent(new CustomEvent('tb-cells-copy', { bubbles: true }));
    };

    // A press on a cell starts a range there and a drag across cells grows it; Shift with a press grows it from where it
    // started. Focus ends on the cell the drag reached, so Shift and the arrow keys carry on from there.
    let dragged = null;
    const startCellDrag = event => {
        const cell = bodyCellOf(event.target);
        table.removeAttribute('data-cell-gesture');
        const control = event.target.closest?.(focusable);
        if (!selectsCells() || event.button !== 0 || !cell || event.target.closest('[data-part=editor]')
            || control && control !== cell && cell.contains(control)) {
            return;
        }

        extending = false;
        if (event.shiftKey) {
            const focused = bodyCellOf(document.activeElement);
            if (startsAgain()) {
                anchor = spotOf(focused ?? cell);
            }

            table.setAttribute('data-cell-gesture', '');
            reportRange(spotOf(cell));
            return;
        }

        clearRange();
        anchor = spotOf(cell);
        dragged = { from: cell, to: cell };
    };
    const moveCellDrag = event => {
        const cell = dragged && bodyCellOf(event.target);
        if (cell && cell !== dragged.to) {
            dragged.to = cell;
            reportRange(spotOf(cell));
        }
    };
    const endCellDrag = () => {
        if (dragged && dragged.to !== dragged.from) {
            table.setAttribute('data-cell-gesture', '');
            focusCell(dragged.to);
        }

        dragged = null;
    };
    const keepCellsUnselected = event => {
        const target = event.target.nodeType === Node.ELEMENT_NODE ? event.target : event.target.parentElement;
        if (selectsCells() && (dragged || ranged) && bodyCellOf(target) && !target.closest('[data-part=editor]')) {
            event.preventDefault();
        }
    };

    const makeStop = (element, cell) => {
        if (stop && stop !== element) {
            stop.tabIndex = -1;
        }

        stop = element;
        element.tabIndex = 0;
        place = { ...place, key: cell.parentElement.dataset.key ?? null,
            index: indexOf(cell.parentElement, linesOf(table)) };
    };

    // A grid inside a row's details keeps the stops its own script gave it.
    const own = element => element.closest('table') === table;
    const restoreTabStops = () => {
        table.querySelectorAll('[data-tb-tab]').forEach(element => {
            if (!own(element)) {
                return;
            }

            const before = element.dataset.tbTab;
            delete element.dataset.tbTab;
            if (before === '') {
                element.removeAttribute('tabindex');
            } else {
                element.setAttribute('tabindex', before);
            }
        });
        table.querySelectorAll('td[tabindex], th[tabindex]').forEach(cell => {
            if (own(cell)) {
                cell.removeAttribute('tabindex');
            }
        });
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
            if (row.dataset.part === 'detail') {
                row.cells[0].tabIndex = -1;
                continue;
            }

            for (const cell of row.cells) {
                if (isSpacer(cell)) {
                    continue;
                }

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
                ?? rowAt(placeIndex(rows), rows) ?? rows.at(-1);
            // A grid that draws only the columns in view finds the column by key, since its cells' places shift as it
            // scrolls, and a spacer is never a stop.
            const keyed = row && drawsColumnsInView(table) && place.columnKey != null
                ? [...row.cells].find(one => one.dataset.column === place.columnKey) : null;
            // A column the box scrolled away from mustn't pull focus to whichever column now holds its place.
            const columnScrolledAway = drawsColumnsInView(table) && place.columnKey != null && !keyed
                && unpinnedKeys(table).includes(place.columnKey);
            const placed = row ? keyed ?? cellAt(row, place.column) : null;
            stop = placed ? stopOf(isSpacer(placed) ? [...row.cells].find(one => !isSpacer(one)) : placed) : null;
            // Focus that fell to the page with its row goes to what takes the row's place. A virtualized grid also drops
            // rows as it scrolls, which leaves nothing in that place and mustn't pull focus back.
            if (document.activeElement === document.body) {
                refocus = holdsFocus && !revealing && !columnScrolledAway
                    && (!isVirtual() || !!rowAt(placeIndex(rows), rows));
                // A window sent before focus moved can be drawn after it and drop the cell just focused; while that
                // cell's column is still in view, it's drawn again and takes focus back.
                const at = columnScrolledAway ? unpinnedKeys(table).indexOf(place.columnKey) : -1;
                const [first, last] = at >= 0 ? columnsInView(table, scroll) : [0, -1];
                if (holdsFocus && !revealing && row && at >= first && at <= last) {
                    revealColumn(row, place.columnKey);
                }
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

    const focusCell = cell => {
        ++moves;
        const element = stopOf(cell);
        makeStop(element, cell);
        steering = true;
        element.focus();
        steering = false;
    };

    // A column not drawn yet is drawn first; focusing it then scrolls it into view, and the window follows the box.
    const columnCell = (row, key) => key == null ? null : [...row.cells].find(one => one.dataset.column === key);
    const revealColumn = async (row, key) => {
        if (key === undefined) {
            return;
        }

        const move = ++moves;
        // Windows sent before this one can still be drawn after it, so the cell counts only once the server has drawn
        // this window, and in its own row, since a header or filter row can change while no body row does.
        let drawn = () => columnCell(row, key);
        if (!drawn()) {
            const at = unpinnedKeys(table).indexOf(key);
            table.dispatchEvent(new CustomEvent('tb-column-window', { bubbles: true, detail: { first: at, last: at } }));
            drawn = () => table.dataset.columnWindow === `${at},${at}` ? columnCell(row, key) : null;
        }

        revealing = true;
        const target = drawn() ?? await new Promise(resolve => {
            const done = () => {
                watch.disconnect();
                clearTimeout(timer);
                resolve(row.isConnected ? drawn() : null);
            };
            const watch = new MutationObserver(() => drawn() && done());
            const timer = setTimeout(done, 5000);
            watch.observe(row, { childList: true });
            watch.observe(table, { attributes: true, attributeFilter: ['data-column-window'] });
        });
        revealing = false;
        // A later key, or focus moving out of the grid, wins over a column that was slow to come.
        const focused = document.activeElement;
        if (move === moves && (!focused || focused === document.body || table.contains(focused)) && target) {
            place.column = startOf(target);
            place.columnKey = key;
            focusCell(target);
        }

        revealed();
    };
    updateTabStops();
    // A move onto a spacer goes on to the unpinned column beside the cell it left, or from a pinned or leading cell to
    // the nearest unpinned column; Home and End go to the first or last.
    const beyondSpacer = (from, step, edge) => {
        const keys = unpinnedKeys(table);
        const at = keys.indexOf(from.dataset.column);
        return edge ? (step > 0 ? keys.at(-1) : keys[0]) : at >= 0 ? keys[at + step] : step > 0 ? keys[0] : keys.at(-1);
    };
    // The cell a move up or down lands on keeps the column, found by its key since the drawn columns may have moved.
    const landingCell = row => {
        const keyed = drawsColumnsInView(table) ? columnCell(row, place.columnKey) : null;
        if (keyed) {
            return keyed;
        }

        const cell = cellAt(row, place.column);
        if (isSpacer(cell)) {
            revealColumn(row, place.column === 0 ? unpinnedKeys(table)[0] : unpinnedKeys(table).at(-1));
            return null;
        }

        return cell;
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

        const cell = row && landingCell(row);
        if (cell) {
            focusCell(cell);
        }

        return row;
    };

    const startEdit = cell => cell.dispatchEvent(new CustomEvent('tb-edit-start', { bubbles: true }));

    // What's typed on a cell replaces its field's value once the server has drawn it. It goes in as typing does, since a
    // number box drops a lone minus sign set through its value. A select or a checkbox just opens.
    let typed = null;
    const takeTyped = field => {
        const { cell, text } = typed;
        typed = null;
        if (field.closest('td, th') === cell && field.closest('[data-part=editor]')
            && field.matches('input, textarea') && !field.readOnly) {
            field.select();
            document.execCommand('insertText', false, text);
        }
    };

    const onKeyDown = event => {
        // Escape anywhere in a row's details, even in a grid inside them, comes back to the details' cell.
        const detail = event.target.closest?.('tr[data-part=detail]');
        if (isGrid() && event.key === 'Escape' && !event.defaultPrevented && detail?.closest('table') === table
            && event.target !== detail.cells[0] && !event.target.closest('[popover]')) {
            event.preventDefault();
            focusCell(detail.cells[0]);
            return;
        }

        const cell = event.target.closest?.('td, th');
        if (!isGrid() || event.defaultPrevented || event.altKey || isComposing(event) || !cell
            || cell.closest('table') !== table || event.target.closest('[popover]')
            || isDetail(cell) && event.target !== cell) {
            return;
        }

        // A grid inside a row's details can hold an edit of its own, which isn't this grid's.
        const inDetail = isDetail(cell);

        // A row's edit keeps Tab among its fields and its Save and Cancel buttons.
        if (event.key === 'Tab' && !inDetail && cell.parentElement.querySelector('[data-part=row-actions]')) {
            const stops = [...cell.parentElement.cells].flatMap(controlsOf)
                .filter(element => element.closest('[data-part=editor], [data-part=row-actions]'));
            const at = stops.indexOf(event.target);
            event.preventDefault();
            stops[(at + (event.shiftKey ? stops.length - 1 : 1)) % stops.length]?.focus();
            return;
        }

        // Tab in a cell's edit opens the next editable cell, which saves this one first; past the last it leaves the grid.
        if (event.key === 'Tab' && !inDetail && cell.querySelector('[data-part=editor]')) {
            const editable = linesOf(table).filter(line => line.dataset.part === 'row')
                .flatMap(line => [...line.cells]).filter(one => one.hasAttribute('data-editable'));
            const next = editable[editable.indexOf(cell) + (event.shiftKey ? -1 : 1)];
            if (next) {
                cancelAndCommit(event, event.target);
                startEdit(next);
            }

            return;
        }

        // A cell being edited leaves Enter and Escape to its edit, and Enter or F2 on an editable cell starts one.
        const editing = !inDetail && cell.querySelector('[data-part=editor], [data-part=row-actions]');
        if (editing && ['Enter', 'Escape'].includes(event.key)) {
            return;
        }

        // Enter on a tree row's button opens or closes the row, so only F2 edits the cell around it.
        if (cell.hasAttribute('data-editable') && ['Enter', 'F2'].includes(event.key)
            && !(event.key === 'Enter' && event.target.closest('[data-part=tree-toggle]'))) {
            if (!editing) {
                startEdit(cell);
            }

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

        // A typed character starts the edit, but Space selects the row unless it's part of an edit already starting.
        // A tree row's button holds its first cell's focus, so typing there edits the cell too.
        const onCell = event.target === cell || event.target.closest('[data-part=tree-toggle]');
        if (onCell && cell.hasAttribute('data-editable') && event.key.length === 1
            && !event.ctrlKey && !event.metaKey && (event.key !== ' ' || typed?.cell === cell)) {
            event.preventDefault();
            typed = { cell, text: (typed?.cell === cell ? typed.text : '') + event.key };
            startEdit(cell);
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
        const across = (cellIndex, edge = false) => {
            const next = row.cells[Math.min(Math.max(cellIndex, 0), row.cells.length - 1)];
            if (isSpacer(next)) {
                const step = cellIndex > cell.cellIndex ? 1 : -1;
                const key = beyondSpacer(cell, step, edge);
                const over = row.cells[next.cellIndex + step];
                // Past the first or last unpinned column, the move steps over the spacer to a pinned or leading cell.
                if (key === undefined && over && !isSpacer(over)) {
                    place.column = startOf(over);
                    place.columnKey = over.dataset.column ?? null;
                    focusCell(over);
                } else {
                    revealColumn(row, key);
                }

                return;
            }

            place.column = startOf(next);
            place.columnKey = next.dataset.column ?? null;
            focusCell(next);
        };
        const page = () => Math.max(1, Math.floor(Math.min(scroll.clientHeight, window.innerHeight)
            / (rowHeight() || fallbackRowHeight)) - 1);
        const control = event.ctrlKey || event.metaKey;
        if (selectsCells() && control && !event.shiftKey && event.key.toLowerCase() === 'c') {
            event.preventDefault();
            copyCells(cell);
            return;
        }

        // Shift with a key that moves focus grows the range to the cell focus reaches; the key alone clears it.
        extending = false;
        if (selectsCells() && ['ArrowRight', 'ArrowLeft', 'ArrowDown', 'ArrowUp', 'PageDown', 'PageUp', 'Home', 'End']
            .includes(event.key)) {
            if (!event.shiftKey) {
                clearRange();
            } else if (bodyCellOf(cell)) {
                if (startsAgain()) {
                    anchor = spotOf(cell);
                }

                extending = true;
            }
        }

        if (selectsCells() && event.key === 'Escape' && ranged) {
            event.preventDefault();
            clearRange();
            return;
        }

        if (control && event.key.toLowerCase() === 'a') {
            if (table.hasAttribute('data-select-all')) {
                event.preventDefault();
                table.dispatchEvent(new CustomEvent('tb-select-all', { bubbles: true }));
            }

            return;
        }

        // A tree row's aria-expanded changes as soon as a key toggles it, but its new rows arrive only once the server
        // redraws its button to match.
        const toggling = () => row.hasAttribute('aria-expanded') && row.getAttribute('aria-expanded')
            !== row.querySelector('[data-part=tree-toggle]')?.getAttribute('aria-expanded');
        // Shift with an arrow extends the range to the row that takes focus, once a virtualized grid has drawn it.
        // A group's heading isn't a row to select, so the range holds at the row just left and picks up at the next row.
        const moveVertically = async to => {
            if (toggling()) {
                // A cell chosen while the toggled row's children arrive wins over the move, as it does for a row still on its way.
                const before = moves;
                await waitFor(() => !toggling() || null);
                if (moves !== before) {
                    return;
                }
            }

            const reached = await moveToRow(to, linesOf(table));
            const target = reached?.dataset.part === 'row' ? reached : row;
            if (reached && event.shiftKey && selectsMany(table) && !selectsCells() && target.dataset.part === 'row') {
                extendTo(target, row.dataset.key);
            }
        };
        // On a tree row's first cell, the key toward the reading direction's end opens the row, and the key toward its
        // start closes it, or on a closed row goes to the row's parent.
        if (cell.dataset.part === 'tree-cell' && !control && !event.shiftKey
            && (event.key === 'ArrowRight' || event.key === 'ArrowLeft')) {
            const forward = (event.key === 'ArrowRight') !== rtl;
            const open = row.getAttribute('aria-expanded');
            // The row shows its new state before the server draws it, so a quick second press acts on that state.
            if (open === (forward ? 'false' : 'true')) {
                event.preventDefault();
                cell.querySelector('[data-part=tree-toggle]').click();
                row.setAttribute('aria-expanded', forward ? 'true' : 'false');
                return;
            }

            const level = Number(row.getAttribute('aria-level'));
            const parent = level > 1 ? rows.slice(0, rows.indexOf(row)).findLast(line =>
                Number(line.getAttribute('aria-level')) === level - 1) : null;
            const to = parent ? indexOf(parent, rows) : Number(row.dataset.parentRow ?? 0);
            if (!forward && to > 0) {
                event.preventDefault();
                moveToRow(to, rows);
                return;
            }
        }

        switch (event.key) {
            case 'ArrowRight':
                across(cell.cellIndex + (rtl ? -1 : 1));
                break;
            case 'ArrowLeft':
                across(cell.cellIndex + (rtl ? 1 : -1));
                break;
            case 'ArrowDown':
                moveVertically(index + 1);
                break;
            case 'ArrowUp':
                moveVertically(index - 1);
                break;
            case 'PageDown':
                moveToRow(index + page(), rows);
                break;
            case 'PageUp':
                moveToRow(index - page(), rows);
                break;
            case 'Home':
                place.column = 0;
                place.columnKey = null;
                if (control) {
                    moveToRow(1, rows);
                } else {
                    across(0, true);
                }
                break;
            case 'End':
                place.column = Number.MAX_SAFE_INTEGER;
                place.columnKey = null;
                if (control) {
                    moveToRow(rowCount(rows), rows);
                } else {
                    across(row.cells.length - 1, true);
                }
                break;
            case 'Enter':
            case 'F2':
                if (stopOf(cell) !== cell) {
                    return;
                }

                if (isDetail(cell)) {
                    firstStopIn(cell)?.focus();
                    break;
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
        if (typed) {
            takeTyped(event.target);
        }

        const cell = event.target.closest?.('td, th');
        if (isGrid() && cell?.closest('table') === table && !event.target.closest('[popover]')) {
            holdsFocus = true;
            makeStop(stopOf(cell), cell);
            // Focus the grid didn't move itself, such as a click, wins over a row still on its way. A column on its way
            // is left to land, since the window that draws it can drop a cell chosen meanwhile (#513).
            if (!steering) {
                if (!revealing) {
                    ++moves;
                }

                place.column = startOf(cell);
                place.columnKey = cell.dataset.column ?? null;
            }

            if (extending && bodyCellOf(cell)) {
                extending = false;
                reportRange(spotOf(cell));
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
    table.addEventListener('pointerdown', startCellDrag);
    table.addEventListener('pointerover', moveCellDrag);
    document.addEventListener('pointerup', endCellDrag);
    table.addEventListener('selectstart', keepCellsUnselected);
    return {
        focusCell,
        // Reporting the box's columns while a column is being drawn for a key press would undo it before the column
        // takes focus.
        revealing: () => revealing,
        focusPlace() {
            const rows = linesOf(table);
            const row = rowAt(placeIndex(rows), rows) ?? rows.at(-1);
            const cell = row && landingCell(row);
            if (cell) {
                focusCell(cell);
            }
        },
        dispose() {
            observer.disconnect();
            table.removeEventListener('keydown', onKeyDown);
            table.removeEventListener('focusin', followFocus);
            table.removeEventListener('focusout', onFocusOut);
            table.removeEventListener('pointerdown', startCellDrag);
            table.removeEventListener('pointerover', moveCellDrag);
            document.removeEventListener('pointerup', endCellDrag);
            table.removeEventListener('selectstart', keepCellsUnselected);
        },
    };
}
