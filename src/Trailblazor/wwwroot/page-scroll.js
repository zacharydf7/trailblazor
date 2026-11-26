// Whether scrollbars here take up room at all. On a phone they don't, and a wide page makes the window report a width
// that has nothing to do with them.
function scrollbarsTakeRoom() {
    const probe = document.createElement('div');
    probe.style.cssText = 'position: absolute; overflow: scroll; visibility: hidden';
    document.documentElement.append(probe);
    const takeRoom = probe.offsetWidth > probe.clientWidth;
    probe.remove();
    return takeRoom;
}

const haveRoom = scrollbarsTakeRoom();

// A fixed element at the window's left edge starts after a scrollbar drawn there, as Firefox can draw it on a
// right-to-left page or in a right-to-left Firefox.
function scrollbarOnLeft() {
    const probe = document.createElement('div');
    probe.style.cssText = 'position: fixed; left: 0; top: 0; visibility: hidden';
    document.documentElement.append(probe);
    const onLeft = probe.getBoundingClientRect().left > 0;
    probe.remove();
    return onLeft;
}

// Stores the scrollbar's width, and whether it's on the left, so CSS can add it back as padding on that side when a
// modal or overlay locks the page. A locked page has no scrollbar to measure, so what was measured before the lock
// stands.
export function measurePageScrollbar() {
    const root = document.documentElement;
    if (getComputedStyle(root).getPropertyValue('--tb-scroll-lock-offset') === '') {
        const width = haveRoom ? window.innerWidth - root.clientWidth : 0;
        root.style.setProperty('--_tb-page-scrollbar', `${width}px`);
        root.toggleAttribute('data-tb-scrollbar-left', width > 0 && scrollbarOnLeft());
    }
}

// A scrollbar coming or going changes the root's width. When a lock ends, the content box changes if the scrollbar
// stays gone and the border box if it comes back, so between them the page catches anything it skipped while locked.
new ResizeObserver(measurePageScrollbar).observe(document.documentElement);
new ResizeObserver(measurePageScrollbar).observe(document.documentElement, { box: 'border-box' });
// A direction change can move Firefox's scrollbar to the other side without changing the root's width, so the
// ResizeObserver misses it.
new MutationObserver(measurePageScrollbar).observe(document.documentElement, { attributeFilter: ['dir'] });
