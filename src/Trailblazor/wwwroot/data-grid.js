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
    return {
        dispose() {
            observer.disconnect();
        },
    };
}
