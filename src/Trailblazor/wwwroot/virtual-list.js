import { getTabbables } from '../Trailblazor.Primitives/focus-scope.js';
import { typedInSelector } from '../Trailblazor.Primitives/pointer-origin.js';

// Fields inside an item that use the feed's keys themselves keep them, as do controls that already handled them.
const usesKeys = `${typedInSelector}, select, input[type=range]`;

// The keys of the WAI-ARIA feed pattern over the items Virtualize has drawn: Page Down and Page Up move between items,
// and Ctrl+End and Ctrl+Home leave the feed. One item is in the tab order, so Tab enters the feed once.
export function createVirtualList(feed) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!feed) {
        return null;
    }

    // The item focus last rested on, or is heading to; it holds the tab stop, and Page Down and Page Up move from it.
    let current = 0;
    // Whether focus is in the feed, or was until a render removed the item holding it.
    let inside = feed.contains(document.activeElement);
    // Only the feed's own children count, so parts of components inside an item, such as a toggle group's items,
    // are left alone.
    const items = () => [...feed.querySelectorAll(':scope > [data-part="item"]')];
    const item = index => feed.querySelector(`:scope > [data-part="item"][data-index="${index}"]`);
    const itemOf = element => {
        while (element && element.parentElement !== feed) {
            element = element.parentElement;
        }

        return element?.matches('[data-part="item"]') ? element : null;
    };
    const count = () => Number(feed.dataset.count ?? 0);
    const estimate = () => {
        const drawn = items();
        const height = drawn.reduce((sum, one) => sum + one.getBoundingClientRect().height, 0);
        return drawn.length > 0 ? height / drawn.length : Number(feed.dataset.itemSize);
    };
    const scroller = () => {
        for (let element = feed; element; element = element.parentElement) {
            if (/(auto|scroll)/.test(getComputedStyle(element).overflowY)) {
                return element;
            }
        }

        return document.scrollingElement;
    };
    // Resolves at the first render that makes until true, or after the wait, with until's last answer.
    const rendered = (wait, until = () => true) => new Promise(resolve => {
        const done = () => {
            watch.disconnect();
            clearTimeout(timer);
            resolve(until());
        };
        const watch = new MutationObserver(() => until() && done());
        const timer = setTimeout(done, wait);
        watch.observe(feed, { childList: true, subtree: true });
    });
    const holdTabStop = () => {
        const stop = item(current) ?? items()[0];
        items().forEach(other => {
            if (other !== stop && other.tabIndex === 0) {
                other.tabIndex = -1;
            }
        });
        if (stop) {
            stop.tabIndex = 0;
        }
    };

    // Virtualize draws only the items near the view, so the list scrolls from the nearest drawn place toward a far item
    // by its estimated distance, until Virtualize draws it.
    async function scrollToIndex(index, focus) {
        if (index >= count()) {
            return;
        }

        if (focus) {
            current = index;
        }

        for (let tries = 0; tries < 10 && !item(index); tries++) {
            const drawn = [...feed.querySelectorAll(':scope > [data-index]')].map(place => Number(place.dataset.index));
            const nearest = index < Math.min(...drawn) ? Math.min(...drawn) : Math.max(...drawn);
            if (drawn.length > 0 && nearest !== index) {
                feed.querySelector(`:scope > [data-index="${nearest}"]`).scrollIntoView({ block: 'start' });
                scroller().scrollTop += (index - nearest) * estimate();
            }

            // Waits for the next render, or a second when none comes, such as while a provider loads.
            await rendered(1000);
        }

        const target = item(index) ?? await rendered(5000, () => item(index));
        // A later key may have moved on while this one waited, and only the latest one takes focus.
        if (!target || (focus && current !== index)) {
            return;
        }

        target.scrollIntoView({ block: 'nearest' });
        if (focus) {
            holdTabStop();
            target.focus();
        }
    }

    const onKeyDown = event => {
        if (event.defaultPrevented || event.target.matches(usesKeys)) {
            return;
        }

        if (event.ctrlKey && (event.key === 'End' || event.key === 'Home')) {
            event.preventDefault();
            leave(event.key === 'End');
        } else if (!event.ctrlKey && !event.altKey && !event.metaKey
            && (event.key === 'PageDown' || event.key === 'PageUp')) {
            event.preventDefault();
            // When keys come faster than items are drawn, each one moves on from where the last was heading.
            const next = current + (event.key === 'PageDown' ? 1 : -1);
            if (next >= 0 && next < count()) {
                scrollToIndex(next, true);
            }
        }
    };
    // Ctrl+End goes to the first control after the feed, and Ctrl+Home to the last one before it.
    const leave = forward => {
        const outside = getTabbables(document.body).filter(element => !feed.contains(element));
        const following = element => feed.compareDocumentPosition(element) & Node.DOCUMENT_POSITION_FOLLOWING;
        const target = forward ? outside.find(following) : outside.filter(element => !following(element)).at(-1);
        target?.focus();
    };
    const onFocusIn = event => {
        inside = true;
        const focused = itemOf(event.target);
        if (focused) {
            current = Number(focused.dataset.index);
            holdTabStop();
        }
    };
    // An item scrolled out of view leaves the page, so focus on it moves to the feed, where the keys keep working.
    const renders = new MutationObserver(() => {
        if (inside && document.activeElement === document.body) {
            feed.focus({ preventScroll: true });
        }

        holdTabStop();
    });
    const onFocusOut = event => {
        if (event.relatedTarget && !feed.contains(event.relatedTarget)) {
            inside = false;
        }
    };
    // A press outside the feed moves the user on, so a render that follows leaves focus alone.
    const onPointerDown = event => {
        if (!feed.contains(event.target)) {
            inside = false;
        }
    };

    feed.addEventListener('keydown', onKeyDown);
    feed.addEventListener('focusin', onFocusIn);
    feed.addEventListener('focusout', onFocusOut);
    document.addEventListener('pointerdown', onPointerDown, true);
    renders.observe(feed, { childList: true });
    holdTabStop();
    feed.dataset.ready = '';
    return {
        scrollToIndex,
        dispose() {
            renders.disconnect();
            feed.removeEventListener('keydown', onKeyDown);
            feed.removeEventListener('focusin', onFocusIn);
            feed.removeEventListener('focusout', onFocusOut);
            document.removeEventListener('pointerdown', onPointerDown, true);
        },
    };
}
