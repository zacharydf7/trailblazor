// A static page has no circuit, so this switches the input's type, aria-pressed and the icon held in data-other-icon.
document.addEventListener('click', event => {
    const button = event.target.closest?.('button[data-part="reveal"][data-static]');
    const input = button?.closest('[data-part="control"]')?.querySelector('[data-part="input"]');
    if (!input || button.disabled) {
        return;
    }

    const shown = input.type === 'password';
    input.type = shown ? 'text' : 'password';
    button.setAttribute('aria-pressed', String(shown));
    const icon = button.querySelector('svg');
    [icon.innerHTML, button.dataset.otherIcon] = [button.dataset.otherIcon, icon.innerHTML];
});
