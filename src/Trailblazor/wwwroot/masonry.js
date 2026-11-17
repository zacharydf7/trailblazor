// Places each card, in the order given, under the shortest column, so cards of different heights leave no gaps. The
// stylesheet's grid works out the columns; the script then positions each card absolutely in one of them and gives the
// masonry the height of its tallest column. The cards keep their order in the page, so reading and Tab follow it.
export function attachMasonry(root) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    let frame = 0;
    const layout = () => {
        frame = 0;
        // A hidden masonry, such as in a tab that isn't shown, keeps its last placement until it shows again.
        if (root.clientWidth === 0) {
            return;
        }

        const style = getComputedStyle(root);
        const tracks = style.gridTemplateColumns.split(' ').map(parseFloat).filter(width => width > 0);
        // With no width to fill, there are no columns, and the cards stay where they were.
        if (tracks.length === 0) {
            return;
        }

        const columnGap = parseFloat(style.columnGap) || 0;
        const rowGap = parseFloat(style.rowGap) || 0;
        // The cards are placed from the padding edge, and the tracks start inside the padding.
        const paddingTop = parseFloat(style.paddingBlockStart) || 0;
        const starts = [];
        let start = parseFloat(style.paddingInlineStart) || 0;
        for (const width of tracks) {
            starts.push(start);
            start += width + columnGap;
        }

        // Every width is set before any height is read, so the browser lays the page out once, not once per card.
        const placed = [...root.children];
        for (const card of placed) {
            card.style.inlineSize = `${tracks[0]}px`;
        }

        const cardHeights = placed.map(card => card.getBoundingClientRect().height);
        const heights = tracks.map(() => 0);
        placed.forEach((card, index) => {
            const column = heights.indexOf(Math.min(...heights));
            card.style.insetInlineStart = `${starts[column]}px`;
            card.style.insetBlockStart = `${paddingTop + heights[column]}px`;
            heights[column] += cardHeights[index] + rowGap;
        });

        let height = Math.max(0, Math.max(0, ...heights) - rowGap);
        if (style.boxSizing === 'border-box') {
            height += paddingTop + (parseFloat(style.paddingBlockEnd) || 0)
                + (parseFloat(style.borderBlockStartWidth) || 0) + (parseFloat(style.borderBlockEndWidth) || 0);
        }

        root.style.blockSize = `${height}px`;
        root.dataset.ready = '';
    };
    const schedule = () => frame ||= requestAnimationFrame(layout);

    // Place the cards again when one changes size, such as when its image loads, or when the masonry's width changes.
    const sizes = new ResizeObserver(schedule);
    const observe = () => {
        sizes.disconnect();
        sizes.observe(root);
        for (const card of root.children) {
            sizes.observe(card);
        }
    };
    const cards = new MutationObserver(() => {
        observe();
        schedule();
    });
    cards.observe(root, { childList: true });
    observe();
    layout();

    return {
        dispose() {
            cancelAnimationFrame(frame);
            sizes.disconnect();
            cards.disconnect();
        },
    };
}
