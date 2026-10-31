// A click on a row goes to the server unless it landed on a control in the row, so a cell's buttons and fields keep
// their clicks.
const controls = 'a[href], button, input, select, textarea, label, summary, [contenteditable], [role="button"], '
    + '[role="checkbox"], [role="combobox"], [role="switch"], [role="link"]';

export function createDataGrid(table, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!table) {
        return null;
    }

    const onClick = event => {
        const row = event.target.closest('tbody > tr[data-key]');
        if (row && table.contains(row) && !event.target.closest(controls)) {
            events.invokeMethodAsync('OnRowClick', row.dataset.key);
        }
    };
    // A checkbox can't show "some" from markup alone, so data-mixed sets it.
    const syncMixed = () => table.querySelectorAll('input[type="checkbox"][data-part="select-all"]')
        .forEach(box => { box.indeterminate = box.hasAttribute('data-mixed'); });
    const observer = new MutationObserver(syncMixed);
    table.addEventListener('click', onClick);
    observer.observe(table, { attributes: true, subtree: true, attributeFilter: ['data-mixed'] });
    syncMixed();
    return {
        dispose() {
            observer.disconnect();
            table.removeEventListener('click', onClick);
        },
    };
}
