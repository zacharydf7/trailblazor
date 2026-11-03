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
            table.removeEventListener('keydown', keepPageFormUnsubmitted);
        },
    };
}
