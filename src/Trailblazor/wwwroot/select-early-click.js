// A select's script attaches some time after the select renders, longer on a slow link. A click that lands first
// marks the select, and the script opens the list when it attaches, unless the user has since moved on.
const pending = () => document.querySelectorAll('.tb-select__trigger[data-open-when-ready]');

document.addEventListener('click', event => {
    const control = event.target.closest?.('[data-part="control"]');
    const combobox = control?.querySelector(':scope > .tb-select__trigger');
    // Skip what the attached select ignores too: its buttons, its popup and a chip's remove mark.
    const wanted = combobox && !('ready' in combobox.dataset)
        && !event.target.closest('button, [popover], [data-part="chip-remove"]') ? combobox : null;
    // A second click on the same select cancels the first, just as it would close an attached select's list.
    const again = wanted && 'openWhenReady' in wanted.dataset;
    for (const marked of pending()) {
        delete marked.dataset.openWhenReady;
    }

    if (wanted && !again) {
        wanted.dataset.openWhenReady = '';
    }
}, { capture: true });

// Focus moving to anything outside the select, as by Tab, means the user has moved on.
document.addEventListener('focusin', event => {
    for (const marked of pending()) {
        if (!marked.parentElement.contains(event.target)) {
            delete marked.dataset.openWhenReady;
        }
    }
}, { capture: true });
