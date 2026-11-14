// Blazor's router would swap in a forced link's page but skips a click that's already handled, so this handles a plain
// click on one and loads the address itself. A click Blazor would leave alone, such as Ctrl-click or one on a link that
// opens elsewhere or downloads, stays with the browser.
document.addEventListener('click', event => {
    const link = event.target.closest?.('a[data-tb-force-load][href]');
    if (!link || event.defaultPrevented || event.button !== 0
        || event.ctrlKey || event.shiftKey || event.altKey || event.metaKey
        || (link.target && link.target !== '_self') || link.hasAttribute('download')) {
        return;
    }

    event.preventDefault();
    location.assign(link.href);
}, true);
