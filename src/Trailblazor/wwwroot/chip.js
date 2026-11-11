// A chip with a remove button handles Backspace, which WebKit's Windows and Linux ports still take to mean going back a
// page. The JS initializer imports this at startup, so no chip makes a call of its own.
document.addEventListener('keydown', event => {
    if (event.key === 'Backspace' && event.target.closest?.('.tb-chip')?.querySelector(':scope > [data-part=remove]')) {
        event.preventDefault();
    }
});
