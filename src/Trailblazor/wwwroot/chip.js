// The chip removes itself on Backspace, which WebKit's Windows and Linux ports still take to mean going back a page.
export function claimBackspace(chip) {
    chip.addEventListener('keydown', event => {
        if (event.key === 'Backspace') {
            event.preventDefault();
        }
    });
}
