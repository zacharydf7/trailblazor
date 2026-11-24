import { frames, watchSizes } from './pages.js';

// A scrollbar that takes room is at least this many pixels thick; less is rounding at a zoomed size.
const thinnestBar = 4;

// Marks a box that scrolls sideways with the sides that have more content past them, so its edge can fade there to say
// that it scrolls. A box whose scrollbar takes room of its own already shows that, so it isn't marked. The scroll
// position is physical, so the marks are left and right. `covered` says which of the start and end sides something
// that stays in view covers, such as pinned columns; a covered side isn't marked either.
export function markScrollEdges(box, covered = () => ({})) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!box) {
        return null;
    }

    const sync = () => {
        const style = getComputedStyle(box);
        const bar = box.offsetHeight - box.clientHeight - parseFloat(style.borderTopWidth)
            - parseFloat(style.borderBottomWidth);
        const [along, most] = [Math.abs(box.scrollLeft), box.scrollWidth - box.clientWidth];
        const { start = false, end = false } = covered();
        const [moreAtStart, moreAtEnd] = [bar < thinnestBar && along > 1 && !start,
            bar < thinnestBar && along < most - 1 && !end];
        const rtl = style.direction === 'rtl';
        box.toggleAttribute('data-more-left', rtl ? moreAtEnd : moreAtStart);
        box.toggleAttribute('data-more-right', rtl ? moreAtStart : moreAtEnd);
    };
    const frame = frames(box, sync);
    const syncSoon = () => frame.request();
    // The content's width changes what is past the edges as much as the box's does.
    const sizes = watchSizes(syncSoon);
    const watch = () => {
        sizes.disconnect();
        sizes.observe(box);
        for (const child of box.children) {
            sizes.observe(child);
        }
    };
    const children = new MutationObserver(() => {
        watch();
        syncSoon();
    });
    children.observe(box, { childList: true });
    box.addEventListener('scroll', syncSoon, { passive: true });
    watch();
    sync();

    return {
        sync,
        dispose() {
            frame.cancel();
            sizes.disconnect();
            children.disconnect();
            box.removeEventListener('scroll', syncSoon);
            box.removeAttribute('data-more-left');
            box.removeAttribute('data-more-right');
        },
    };
}
