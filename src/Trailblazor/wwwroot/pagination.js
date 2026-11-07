// Enter in a compact pagination's box goes to the typed page, and mustn't also submit a form around it. Canceling the key
// also cancels the change event that commits typed text, so this sends one itself. The JS initializer imports this at
// startup, so static pages are covered too.
document.addEventListener('keydown', event => {
    if (event.key === 'Enter' && !event.isComposing && !event.defaultPrevented
        && event.target.closest?.('[data-part="page-box"]')) {
        event.preventDefault();
        event.target.dispatchEvent(new Event('change', { bubbles: true }));
    }
});
