// A chip with a remove button handles Backspace, which WebKit's Windows and Linux ports still take to mean going back a
// page, and Delete. Both are prevented, so an app's shortcut that skips prevented keys leaves them alone. The JS
// initializer imports this at startup, so no chip makes a call of its own.
document.addEventListener('keydown', event => {
    if ((event.key === 'Backspace' || event.key === 'Delete')
        && /** @type {Element} */ (event.target).closest?.('.tb-chip')?.querySelector(':scope > [data-part=remove]')) {
        event.preventDefault();
    }
});
