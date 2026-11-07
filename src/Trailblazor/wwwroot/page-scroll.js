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

// Stores the scrollbar's width, which CSS adds back as padding when a modal or overlay locks the page. A locked page has
// no scrollbar to measure, so the width from before the lock stands.
export function measurePageScrollbar() {
    const root = document.documentElement;
    if (getComputedStyle(root).getPropertyValue('--tb-scroll-lock-offset') === '') {
        root.style.setProperty('--tb-page-scrollbar', haveRoom ? `${window.innerWidth - root.clientWidth}px` : '0px');
    }
}

// A scrollbar coming or going changes the root's width.
new ResizeObserver(measurePageScrollbar).observe(document.documentElement);
