// Inputs that act on Enter themselves, as a button does, so the grid leaves their Enter alone.
const actOnEnter = new Set(['button', 'color', 'file', 'image', 'reset', 'submit']);

// Inputs whose value Enter doesn't commit, since it isn't typed.
const untyped = new Set(['checkbox', 'radio', 'range']);

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
    // variables its style reads, numbered from that side. Scroll padding as wide as the pinned cells keeps a focused
    // cell from scrolling in under them.
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
    };
    const resizes = new ResizeObserver(measurePins);
    const watchPins = () => {
        resizes.disconnect();
        [...headers.cells].filter(cell => cell.dataset.pinned).forEach(cell => resizes.observe(cell));
        // With nothing left pinned, no cell reports a size, so the padding is cleared here.
        measurePins();
    };
    // Changes inside a header, such as its filter menu opening, leave the pins alone. Blazor rewrites the scroll
    // box's style when the grid's height changes, which drops its padding.
    const columns = new MutationObserver(changes => {
        if (changes.some(change => change.target === headers || change.attributeName === 'data-pinned')) {
            watchPins();
        } else if (changes.some(change => change.target === scroll)) {
            measurePins();
        }
    });
    columns.observe(headers, { childList: true, subtree: true, attributeFilter: ['data-pinned'] });
    columns.observe(scroll, { attributeFilter: ['style'] });
    watchPins();

    // Enter in a filter's input mustn't also submit a page's form around the grid. Canceling the key also cancels the
    // change event that commits typed text, so the handler sends one itself. An Enter a widget already canceled, such
    // as an autocomplete choosing an option, is left to the widget.
    const keepPageFormUnsubmitted = event => {
        const input = event.target;
        if (event.key !== 'Enter' || event.isComposing || event.defaultPrevented
            || !(input instanceof HTMLInputElement) || actOnEnter.has(input.type)
            || !input.closest('.tb-data-grid__filters, .tb-data-grid__filter-form')) {
            return;
        }

        event.preventDefault();
        if (!untyped.has(input.type)) {
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }
    };
    table.addEventListener('keydown', keepPageFormUnsubmitted);
    return {
        dispose() {
            observer.disconnect();
            resizes.disconnect();
            columns.disconnect();
            table.removeEventListener('keydown', keepPageFormUnsubmitted);
        },
    };
}
