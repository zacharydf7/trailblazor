import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { isTabbable } from '../Trailblazor.Primitives/focus-scope.js';
import { watchSizes } from '../Trailblazor.Primitives/pages.js';
// The arrow keys, Home and End move focus between enabled tabs; with automatic activation they also select it, through
// `order` rather than a click, so the tab's OnClick hears only clicks. The headers follow the order in which tabs
// register, so the tabs' order in the markup is reported to `order` too. Delete or Backspace on a closable tab closes
// it, as on a chip.
export function attachTabs(list, order, followsSelection) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!list) {
        return null;
    }

    const onKeyDown = event => {
        const tab = event.target;
        // Prevented on every tab, closable or not, so an app's shortcut that skips prevented keys leaves these alone.
        if ((event.key === 'Delete' || event.key === 'Backspace') && !event.altKey && !event.ctrlKey && !event.metaKey
            && tab.parentElement === list) {
            event.preventDefault();
            // A held key repeats, and each repeat would ask to close the tab again while the first close runs.
            if (tab.querySelector('[data-part="close"]') && !event.repeat) {
                callDotNet(order, 'OnClose', tab.id);
            }

            return;
        }

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
        scrollTabIntoRow(list, tab);
    };
    // Scroll buttons show while the row overflows, each turned off at its own end; a click scrolls by most of the row.
    const scrollStep = 0.8;
    // Some zoom levels leave the row a fraction of a pixel from its end.
    const tolerance = 1;
    const root = list.parentElement;
    const scrollers = () => [...root.querySelectorAll(':scope > .tb-tabs__scroll')];
    const updateScrollers = () => {
        const overflow = list.scrollWidth - list.clientWidth;
        // Right-to-left rows scroll to negative positions.
        const along = Math.abs(list.scrollLeft);
        for (const cell of scrollers()) {
            const hide = overflow <= tolerance;
            if (cell.hidden !== hide) {
                cell.hidden = hide;
            }

            const toStart = cell.classList.contains('tb-tabs__scroll--start');
            cell.querySelector('button').disabled = toStart ? along <= tolerance : along >= overflow - tolerance;
        }
    };
    const scrollerOf = event => {
        const cell = event.target.closest?.('.tb-tabs__scroll');
        return cell?.parentElement === root ? cell : null;
    };
    const onScrollerClick = event => {
        const cell = scrollerOf(event);
        if (!cell) {
            return;
        }

        const backward = cell.classList.contains('tb-tabs__scroll--start')
            !== (getComputedStyle(list).direction === 'rtl');
        const behavior = matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth';
        list.scrollBy({ left: (backward ? -1 : 1) * list.clientWidth * scrollStep, behavior });
    };
    // A press leaves focus where it was, as on a number field's step buttons.
    const onScrollerPress = event => {
        if (scrollerOf(event)) {
            event.preventDefault();
        }
    };
    root.addEventListener('click', onScrollerClick);
    root.addEventListener('mousedown', onScrollerPress);
    list.addEventListener('scroll', updateScrollers, { passive: true });

    // A focused tab the parent removes, by any route, hands focus on as a closed tab does: to the tab selected in its
    // place if it was the selected one, or else to the next enabled tab or the one before. Removing a tab after focus
    // has left it moves nothing.
    let focused = null;
    // A removal whose focus waits for the parent's new index to be applied.
    let waiting = null;
    // The tab that was selected when the last changes ended. A tab that goes away as the app selects another has
    // already lost aria-selected by the time it is removed.
    const selectedTab = () => list.querySelector(':scope > [role="tab"][aria-selected="true"]');
    let shown = selectedTab();
    const onFocusIn = event => focused = event.target.parentElement === list ? event.target : null;
    // Chromium sends focusout while it removes the focused tab, and a window losing focus sends it too while the tab
    // keeps focus, so wait a microtask to see whether focus really went elsewhere.
    const onFocusOut = event => queueMicrotask(() => {
        if (event.target === focused && event.target.isConnected && list.ownerDocument.activeElement !== event.target) {
            focused = null;
        }
    });
    const refocus = records => {
        const removedBy = new Map(records.flatMap(record => [...record.removedNodes].map(node => [node, record])));
        const removal = focused && !focused.isConnected && removedBy.get(focused);
        if (removal) {
            // Focus follows the selection if the tab that went away was selected, and always in a ribbon.
            const follows = followsSelection || focused === shown;
            waiting = { removal, removedBy, follows };
            focused = null;
        }

        if (!waiting || (waiting.follows && list.hasAttribute('data-settling'))) {
            return;
        }

        const { removal: gone, removedBy: removals, follows } = waiting;
        waiting = null;
        const active = list.ownerDocument.activeElement;
        if (active && active !== list.ownerDocument.body) {
            return;
        }

        const selected = follows
            ? list.querySelector(':scope > [role="tab"][aria-selected="true"]:not(:disabled)') : null;
        // A neighbor removed in the same render is passed over by continuing from the siblings its removal recorded.
        const enabled = node => node.isConnected && node.matches?.('[role="tab"]:not(:disabled)');
        const nearest = (node, side) => {
            while (node && !enabled(node)) {
                node = node.isConnected ? node[side] : removals.get(node)?.[side];
            }

            return node;
        };
        (selected ?? nearest(gone.nextSibling, 'nextSibling') ?? nearest(gone.previousSibling, 'previousSibling'))
            ?.focus();
    };

    const updatePanelStop = () => {
        const id = list.querySelector(':scope > [aria-selected="true"]')?.getAttribute('aria-controls');
        const panel = id && list.ownerDocument.getElementById(id);
        if (panel) {
            settlePanelStop(list, panel);
        }
    };

    // A panel that kept its stop because it had focus is decided again once focus has left it.
    const onPanelFocusOut = event => {
        if (event.target.parentElement === root) {
            updatePanelStop();
        }
    };

    // Tabs arriving already selected change no attribute, and may not change the row's size.
    const selection = new MutationObserver(records => {
        refocus(records);
        shown = selectedTab();
        revealSelected();
        watchTabs();
        updateScrollers();
    });
    selection.observe(list, { childList: true, subtree: true, attributes: true,
        attributeFilter: ['aria-selected', 'data-settling'] });
    // This covers what no selection change does: the first render and a hidden row being shown, which is also when a
    // panel's content can first be seen to start with a control.
    const sizes = watchSizes(() => {
        revealSelected();
        updateScrollers();
        updatePanelStop();
    });
    // A tab whose label grows changes how far the row overflows without resizing the list.
    const watched = new Set();
    const watchTabs = () => {
        for (const tab of list.querySelectorAll(':scope > [role="tab"]')) {
            if (!watched.has(tab)) {
                watched.add(tab);
                sizes.observe(tab);
            }
        }

        for (const tab of watched) {
            if (!tab.isConnected) {
                watched.delete(tab);
                sizes.unobserve(tab);
            }
        }
    };
    sizes.observe(list);
    watchTabs();

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
    // Only a changed header or a moved anchor can change the order, so it isn't reported for other updates.
    const observer = new MutationObserver(records => {
        if (records.some(record => record.target === list
            || [...record.addedNodes, ...record.removedNodes].some(holdsAnchor))) {
            reportOrder();
        }

        // Scroll buttons turned on after attachTabs ran arrive hidden, on a row that may already overflow.
        if (records.some(record => record.target === root)) {
            updateScrollers();
        }

        updatePanelStop();
    });
    // A panel shown again, a control turned off or hidden, a new choice of which panels are stops, and text that
    // fills in before a panel's first control add no node.
    observer.observe(list.parentElement, {
        childList: true, subtree: true, characterData: true, attributes: true,
        attributeFilter: ['disabled', 'hidden', 'data-focusable-panels'],
    });
    // A tab may have rendered between the first render and this call.
    reportOrder();
    // Decided before data-ready is set, so whoever waits for that mark finds the stop settled.
    updatePanelStop();
    list.addEventListener('keydown', onKeyDown);
    list.addEventListener('focusin', onFocusIn);
    list.addEventListener('focusout', onFocusOut);
    root.addEventListener('focusout', onPanelFocusOut);
    list.dataset.ready = '';
    const handle = DotNet.createJSObjectReference({
        dispose() {
            observer.disconnect();
            selection.disconnect();
            sizes.disconnect();
            list.removeEventListener('keydown', onKeyDown);
            list.removeEventListener('focusin', onFocusIn);
            list.removeEventListener('focusout', onFocusOut);
            root.removeEventListener('focusout', onPanelFocusOut);
            list.removeEventListener('scroll', updateScrollers);
            root.removeEventListener('click', onScrollerClick);
            root.removeEventListener('mousedown', onScrollerPress);
        },
    });
    return { handle };
}

// Scrolls a row of tabs, and only the row, until the tab shows. A tab wider than the row shows the start of its label.
export function scrollTabIntoRow(list, tab) {
    const own = tab.getBoundingClientRect();
    const row = list.getBoundingClientRect();
    const start = own.left - row.left;
    const end = own.right - row.right;
    const wider = own.width > row.width;
    const left = wider ? (getComputedStyle(list).direction === 'rtl' ? end : start)
        : start < 0 ? start : Math.max(end, 0);
    list.scrollBy({ left, behavior: 'instant' });
}

// The enabled tab that an arrow key, Home or End moves to from the focused one, if the key moves at all.
export function nextTab(list, event) {
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

const pictures = 'img:not([alt=""]), svg, canvas, video';
// Controls that share one tab stop: the arrow keys reach the ones Tab skips. Anything else that only script can focus,
// such as a list that scrolls, holds content people read.
const skippedByTab = ':is(a, button, input, select, textarea, [role=tab], [role=option], [role=treeitem], [role=row], '
    + '[role=gridcell], [role=columnheader], [role=rowheader], [role=radio], [role^=menuitem], [role=button], '
    + '[role=link], [role=checkbox], [role=switch])[tabindex="-1"]';

// Whether Tab reaches a control in the panel before anything else people read or look at there. Text that names
// something, in a label, a legend or an element an aria-labelledby points at, doesn't count. Neither does a control Tab
// skips because it shares one tab stop with others, such as the tabs before a nested list's selected one.
export function startsWithControl(panel) {
    const names = element => element.id !== ''
        && panel.querySelector(`[aria-labelledby~="${CSS.escape(element.id)}"]`) !== null;
    // An element with display: contents has no box of its own, but what it holds still shows.
    const shows = element => element.checkVisibility() || getComputedStyle(element).display === 'contents';
    // A control Tab reaches is never passed over, though it may name what it opens, as a menu's button does.
    const skipped = element => !isTabbable(element) && (!shows(element)
        || element.matches(`legend, [aria-hidden="true"], ${skippedByTab}`) || names(element));
    const walker = document.createTreeWalker(panel, NodeFilter.SHOW_ELEMENT | NodeFilter.SHOW_TEXT, node =>
        node.nodeType === Node.ELEMENT_NODE && skipped(node) ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT);
    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
        if (node.nodeType === Node.TEXT_NODE) {
            if (node.data.trim() !== '' && !node.parentElement.closest('label')) {
                return false;
            }
        } else if (isTabbable(node)) {
            return true;
        } else if (node.matches(pictures)) {
            return false;
        }
    }

    return false;
}

// The server makes every shown panel a tab stop unless FocusablePanels is Never. With Auto, this takes the stop away
// from a panel that starts with a control, and gives it back when the panel no longer does. A panel that has focus
// keeps its stop, or focus would be lost.
function settlePanelStop(list, panel) {
    const stops = list.dataset.focusablePanels;
    const stop = stops === 'always' || stops !== 'never' && !startsWithControl(panel);
    if (stop && !panel.hasAttribute('tabindex')) {
        panel.tabIndex = 0;
    } else if (!stop && panel.hasAttribute('tabindex') && panel !== panel.ownerDocument.activeElement) {
        panel.removeAttribute('tabindex');
    }
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
        select(event.target.ownerDocument.getElementById(event.target.getAttribute('aria-labelledby')));
    }
}, true);
// The browser may show a match before this script runs, so the tab whose panel it showed is selected now.
for (const tab of document.querySelectorAll('.tb-tabs__list > [role="tab"]:not([data-ready])[aria-selected="false"]')) {
    if (document.getElementById(tab.getAttribute('aria-controls'))?.hidden === false) {
        select(tab);
    }
}

// A static page's panels don't change by themselves, so this decides the shown panels' stops as the script starts and
// after an enhanced navigation brings new ones. Once a list is interactive, it decides its own.
export function settleStaticPanelStops() {
    const shown = '.tb-tabs__list > [role="tab"]:not([data-ready])[aria-selected="true"]';
    for (const tab of document.querySelectorAll(shown)) {
        const panel = document.getElementById(tab.getAttribute('aria-controls'));
        if (panel) {
            settlePanelStop(tab.parentElement, panel);
        }
    }
}
settleStaticPanelStops();

// A tab with no panel switches a view rendered elsewhere, which only Blazor can do.
function select(tab) {
    if (!tab.hasAttribute('aria-controls')) {
        return;
    }

    for (const other of tab.parentElement.querySelectorAll(':scope > [role="tab"]')) {
        const selected = other === tab;
        other.setAttribute('aria-selected', selected);
        other.tabIndex = selected ? 0 : -1;
        const panel = tab.ownerDocument.getElementById(other.getAttribute('aria-controls'));
        if (panel && selected) {
            // Shown first, since a hidden panel's controls don't count.
            panel.removeAttribute('hidden');
            settlePanelStop(tab.parentElement, panel);
        } else if (panel) {
            // Like the server's markup, a disabled tab's panel is plain hidden, so find skips it.
            panel.setAttribute('hidden', other.disabled ? '' : 'until-found');
            panel.removeAttribute('tabindex');
        }
    }
}
