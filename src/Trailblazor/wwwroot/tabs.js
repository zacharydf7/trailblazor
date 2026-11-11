import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
// The arrow keys, Home and End move focus between enabled tabs; with automatic activation they also select it, through
// `order` rather than a click, so the tab's OnClick hears only clicks. The headers follow the order in which tabs
// register, so the tabs' order in the markup is reported to `order` too.
export function attachTabs(list, order) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!list) {
        return null;
    }

    const onKeyDown = event => {
        const next = nextTab(list, event);
        if (!next) {
            return;
        }

        event.preventDefault();
        next.focus();
        if (list.dataset.activation === 'automatic') {
            callDotNet(order, 'OnSelect', next.id);
        }
    };

    // Scrolling only the list leaves the page where it is. A tab is revealed only as it becomes selected or moves, so a
    // row the user has since scrolled stays put. A hidden row can't be measured, so it waits until it's shown.
    let revealed = null;
    const revealSelected = () => {
        const tab = list.querySelector(':scope > [aria-selected="true"]');
        if (!tab) {
            revealed = null;
            return;
        }

        const place = `${tab.id} ${[...list.children].indexOf(tab)}`;
        if (list.clientWidth === 0 || place === revealed) {
            return;
        }

        revealed = place;
        const own = tab.getBoundingClientRect();
        const row = list.getBoundingClientRect();
        const start = own.left - row.left;
        const end = own.right - row.right;
        // A tab wider than the row shows the start of its label.
        const wider = own.width > row.width;
        const left = wider ? (getComputedStyle(list).direction === 'rtl' ? end : start)
            : start < 0 ? start : Math.max(end, 0);
        list.scrollBy({ left, behavior: 'instant' });
    };
    // Tabs arriving already selected change no attribute, and may not change the row's size.
    const selection = new MutationObserver(revealSelected);
    selection.observe(list, { childList: true, subtree: true, attributes: true, attributeFilter: ['aria-selected'] });
    // This covers what no selection change does: the first render and a hidden row being shown.
    const sizes = new ResizeObserver(revealSelected);
    sizes.observe(list);

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
            callDotNet(order, 'OnReorder', anchors);
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
    list.dataset.ready = '';
    const handle = DotNet.createJSObjectReference({
        dispose() {
            observer.disconnect();
            selection.disconnect();
            sizes.disconnect();
            list.removeEventListener('keydown', onKeyDown);
        },
    });
    return { handle };
}

// The enabled tab that an arrow key, Home or End moves to from the focused one, if the key moves at all.
function nextTab(list, event) {
    const tabs = [...list.querySelectorAll(':scope > [role="tab"]:not(:disabled)')];
    const index = tabs.indexOf(event.target);
    if (index < 0 || event.altKey || event.ctrlKey || event.metaKey) {
        return null;
    }

    const rtl = getComputedStyle(list).direction === 'rtl';
    const forward = list.getAttribute('aria-orientation') === 'vertical' ? 'ArrowDown'
        : rtl ? 'ArrowLeft' : 'ArrowRight';
    const back = { ArrowDown: 'ArrowUp', ArrowRight: 'ArrowLeft', ArrowLeft: 'ArrowRight' }[forward];
    return {
        [forward]: tabs[(index + 1) % tabs.length],
        [back]: tabs[(index - 1 + tabs.length) % tabs.length],
        Home: tabs[0],
        End: tabs.at(-1),
    }[event.key];
}

// Until the page is interactive, clicks and keys select tabs here, and find-in-page selects the tab whose panel holds a
// match. Blazor renders an interactive tab with data-ready and selects it from then on.
const staticTab = event => event.target.closest?.('.tb-tabs__list > [role="tab"]:not([data-ready], :disabled)');
document.addEventListener('click', event => {
    const tab = staticTab(event);
    if (tab) {
        select(tab);
    }
});
document.addEventListener('keydown', event => {
    const tab = staticTab(event);
    const next = tab && nextTab(tab.parentElement, event);
    if (next) {
        event.preventDefault();
        next.focus();
        if (tab.parentElement.dataset.activation === 'automatic') {
            select(next);
        }
    }
});
document.addEventListener('beforematch', event => {
    if (event.target.matches('.tb-tabs__panel')) {
        select(document.getElementById(event.target.getAttribute('aria-labelledby')));
    }
}, true);
// The browser may show a match before this script runs, so the tab whose panel it showed is selected now.
for (const tab of document.querySelectorAll('.tb-tabs__list > [role="tab"]:not([data-ready])[aria-selected="false"]')) {
    if (document.getElementById(tab.getAttribute('aria-controls'))?.hidden === false) {
        select(tab);
    }
}

// A tab with no panel switches a view rendered elsewhere, which only Blazor can do.
function select(tab) {
    if (!tab.hasAttribute('aria-controls')) {
        return;
    }

    for (const other of tab.parentElement.querySelectorAll(':scope > [role="tab"]')) {
        const selected = other === tab;
        other.setAttribute('aria-selected', selected);
        other.tabIndex = selected ? 0 : -1;
        const panel = document.getElementById(other.getAttribute('aria-controls'));
        if (panel && selected) {
            panel.removeAttribute('hidden');
            panel.tabIndex = 0;
        } else if (panel) {
            // Like the server's markup, a disabled tab's panel is plain hidden, so find skips it.
            panel.setAttribute('hidden', other.disabled ? '' : 'until-found');
            panel.removeAttribute('tabindex');
        }
    }
}
