export function attachGrid(grid, dotNetReference) {
    // Arrow keys move the active row; stop them from also scrolling the grid natively.
    const onKeyDown = event => {
        if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
            event.preventDefault();
        }
    };
    grid.addEventListener('keydown', onKeyDown);

    // Measurement hooks; window-level so Playwright can drive the spike.
    window.gridSpike = {
        stats: () => dotNetReference.invokeMethodAsync('GetStats'),
        touchRow: index => dotNetReference.invokeMethodAsync('TouchRow', index),
        rerender: () => dotNetReference.invokeMethodAsync('Rerender'),
        managedMemory: () => dotNetReference.invokeMethodAsync('GetManagedMemory'),
        measureScroll: segments => measureScroll(grid, segments),
        measureFill: scrollTop => measureFill(grid, scrollTop),
        measureAlignment: segments => measureAlignment(grid, segments),
        renderedRows: () => [...grid.querySelectorAll('.grid-row[data-row]')].map(row => Number(row.dataset.row)),
        domNodes: () => grid.querySelectorAll('*').length,
    };

    return {
        dispose() {
            grid.removeEventListener('keydown', onKeyDown);
            delete window.gridSpike;
        },
    };
}

// Rows sit below the sticky header at index × rowHeight in the scrolled content.
export function scrollRowIntoView(grid, index, rowHeight) {
    const header = grid.querySelector('.grid-header').offsetHeight;
    const top = index * rowHeight;
    if (top < grid.scrollTop) {
        grid.scrollTop = top;
    } else if (header + top + rowHeight > grid.scrollTop + grid.clientHeight) {
        grid.scrollTop = header + top + rowHeight - grid.clientHeight;
    }
}

function nextFrame() {
    return new Promise(resolve => requestAnimationFrame(resolve));
}

function afterNextPaint() {
    return new Promise(resolve => requestAnimationFrame(() => {
        const channel = new MessageChannel();
        channel.port1.onmessage = () => resolve();
        channel.port2.postMessage(null);
    }));
}

// Scrolls a fixed distance per frame (frames: null means to the end); returns overall and per-segment frame stats.
async function measureScroll(grid, segments) {
    const all = [];
    const perSegment = [];
    let last = await nextFrame();
    for (const { pxPerFrame, frames } of segments) {
        const longTasks = [];
        const observer = new PerformanceObserver(list => longTasks.push(...list.getEntries().map(e => e.duration)));
        observer.observe({ type: 'longtask' });
        const intervals = [];
        const atEnd = () => grid.scrollTop + grid.clientHeight >= grid.scrollHeight - 1;
        for (let i = 0; frames === null ? !atEnd() : i < frames; i++) {
            grid.scrollTop += pxPerFrame;
            const now = await nextFrame();
            intervals.push(now - last);
            last = now;
        }

        observer.disconnect();
        all.push(...intervals);
        perSegment.push({ pxPerFrame, ...summarize(intervals), longestTask: Math.max(0, ...longTasks) });
    }

    const longestTask = Math.max(...perSegment.map(segment => segment.longestTask));
    return { ...summarize(all), longestTask, segments: perSegment };
}

function summarize(intervals) {
    const sorted = [...intervals].sort((a, b) => a - b);
    return {
        frames: sorted.length,
        median: sorted[Math.floor(sorted.length / 2)],
        p95: sorted[Math.floor(sorted.length * 0.95)],
    };
}

// Milliseconds from a jump until a painted frame shows real rows covering the whole body, or -1 after 5 seconds.
async function measureFill(grid, scrollTop) {
    const start = performance.now();
    grid.scrollTop = scrollTop;
    while (performance.now() - start < 5000) {
        await afterNextPaint();
        if (isFilled(grid)) {
            return performance.now() - start;
        }
    }

    return -1;
}

function isFilled(grid) {
    const top = grid.querySelector('.grid-header').getBoundingClientRect().bottom;
    const bottom = grid.getBoundingClientRect().top + grid.clientTop + grid.clientHeight;
    const rows = [...grid.querySelectorAll('.grid-row[data-row]')]
        .map(row => row.getBoundingClientRect())
        .filter(rect => rect.bottom > top && rect.top < bottom)
        .sort((a, b) => a.top - b.top);
    if (rows.length === 0 || rows[0].top > top + 1) {
        return false;
    }

    for (let i = 1; i < rows.length; i++) {
        if (rows[i].top > rows[i - 1].bottom + 1) {
            return false;
        }
    }

    return rows.at(-1).bottom >= bottom - 1;
}

// Scrolls as measureScroll does and returns the worst drift, in px, of the header and the pinned column.
async function measureAlignment(grid, segments) {
    let worst = 0;
    for (const { pxPerFrame, frames, horizontal } of segments) {
        for (let i = 0; i < frames; i++) {
            if (horizontal) {
                grid.scrollLeft += pxPerFrame;
            } else {
                grid.scrollTop += pxPerFrame;
            }

            await afterNextPaint();
            const gridRect = grid.getBoundingClientRect();
            const innerTop = gridRect.top + grid.clientTop;
            const innerLeft = gridRect.left + grid.clientLeft;
            const headerTop = grid.querySelector('.grid-header').getBoundingClientRect().top;
            worst = Math.max(worst, Math.abs(headerTop - innerTop));
            for (const cell of grid.querySelectorAll('.grid-row .pinned')) {
                worst = Math.max(worst, Math.abs(cell.getBoundingClientRect().left - innerLeft));
            }
        }
    }

    return worst;
}
