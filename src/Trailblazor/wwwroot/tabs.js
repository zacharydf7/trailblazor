// The arrow keys, Home and End move focus between enabled tabs; with automatic activation they also select it.
// The headers follow the order in which tabs register, so the tabs' order in the markup is reported to `order`.
export function attachTabs(list, order) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!list) {
        return null;
    }

    const onKeyDown = event => {
        const tabs = [...list.querySelectorAll(':scope > [role="tab"]:not(:disabled)')];
        const index = tabs.indexOf(event.target);
        if (index < 0) {
            return;
        }

        const forward = getComputedStyle(list).direction === 'rtl' ? 'ArrowLeft' : 'ArrowRight';
        const back = forward === 'ArrowRight' ? 'ArrowLeft' : 'ArrowRight';
        const next = {
            [forward]: tabs[(index + 1) % tabs.length],
            [back]: tabs[(index - 1 + tabs.length) % tabs.length],
            Home: tabs[0],
            End: tabs.at(-1),
        }[event.key];
        if (!next) {
            return;
        }

        event.preventDefault();
        next.focus();
        if (list.dataset.activation === 'automatic') {
            next.click();
        }
    };

    let reported = null;
    const reportOrder = () => {
        const headers = [...list.querySelectorAll(':scope > [role="tab"]')].map(tab => tab.id);
        const known = new Set(headers);
        // Tabs nested in a panel leave anchors here too, but have no header in this list.
        const anchors = [...list.parentElement.querySelectorAll('[data-part="tab-anchor"]')]
            .map(anchor => anchor.dataset.tab)
            .filter(id => known.has(id));
        // A tab added in its place is reported too, so the server stops waiting for its order.
        const ids = anchors.join(' ');
        if (anchors.length === headers.length && ids !== reported) {
            reported = ids;
            order.invokeMethodAsync('OnReorder', anchors);
        }
    };

    const holdsAnchor = node => node.nodeType === Node.ELEMENT_NODE
        && (node.matches('[data-part="tab-anchor"]') || node.querySelector('[data-part="tab-anchor"]'));
    // Only a changed header or a moved anchor can change the order, so panel content updates are skipped.
    const observer = new MutationObserver(records => {
        if (records.some(record => record.target === list
            || [...record.addedNodes, ...record.removedNodes].some(holdsAnchor))) {
            reportOrder();
        }
    });
    observer.observe(list.parentElement, { childList: true, subtree: true });
    // A tab may have rendered between the first render and this call.
    reportOrder();
    list.addEventListener('keydown', onKeyDown);
    // Horizontal is already a tab list's default; setting it marks that the arrow keys are attached.
    list.setAttribute('aria-orientation', 'horizontal');
    return {
        dispose() {
            observer.disconnect();
            list.removeEventListener('keydown', onKeyDown);
        },
    };
}
