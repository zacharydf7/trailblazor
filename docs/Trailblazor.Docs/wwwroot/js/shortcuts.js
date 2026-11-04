// "/" outside a field, or Ctrl+K (Cmd+K on Apple devices) anywhere, asks the layout to open and focus the search. On
// Apple devices Ctrl+K is left to text fields, where it deletes to the end of the line.
const apple = () => /Mac|iPhone|iPad/.test(navigator.platform);

export function listen(layout) {
    const onKeyDown = event => {
        const typing = event.target instanceof Element
            && event.target.closest('input, textarea, select, [contenteditable]:not([contenteditable="false"])');
        const slash = event.key === '/' && !typing && !event.ctrlKey && !event.metaKey && !event.altKey;
        // Autofill sends keydown events without a key.
        const k = event.key?.toLowerCase() === 'k' && (apple() ? event.metaKey : event.ctrlKey) && !event.altKey
            && !event.shiftKey;
        if (slash || k) {
            event.preventDefault();
            layout.invokeMethodAsync('OpenSearch');
        }
    };
    document.addEventListener('keydown', onKeyDown);
    // The search names only the keys this device takes, so screen readers say the ones that work.
    document.querySelector('#docs-search input')
        ?.setAttribute('aria-keyshortcuts', apple() ? '/ Meta+K' : '/ Control+K');
    // Marks that the shortcuts work, which tests wait for.
    document.documentElement.dataset.shortcuts = '';
    return { dispose: () => document.removeEventListener('keydown', onKeyDown) };
}
