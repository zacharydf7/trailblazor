const oppositeSides = { top: 'bottom', bottom: 'top', left: 'right', right: 'left' };
const viewportPadding = 8;

export function createAnchoredPosition(floating, anchor, options) {
    // The names match the C# enums; any other spelling would silently fall back to the end side.
    if (!['Top', 'Bottom', 'Start', 'End'].includes(options.side)
        || !['Start', 'Center', 'End'].includes(options.align)) {
        throw new Error(`Unknown placement: side ${options.side}, align ${options.align}.`);
    }

    // Fixed coordinates are viewport-relative only in the top layer or without transformed ancestors.
    floating.style.position = 'fixed';
    floating.style.inset = 'auto';
    floating.style.margin = '0';
    // CSS that has to span the gap, such as a tooltip's hover bridge, reads its size from here.
    floating.style.setProperty('--tb-anchored-offset', `${options.offset}px`);

    // CSS that keeps a popup as wide as its anchor, such as a select's list, reads the live width from here.
    const update = () => {
        floating.style.setProperty('--tb-anchor-width', `${anchor.offsetWidth}px`);
        position(floating, anchor, options);
    };
    update();

    // A new anchor width resizes the popup, which inside the observer's own callback is reported as a loop error.
    const resizeObserver = new ResizeObserver(() => floating.style.getPropertyValue('--tb-anchor-width')
        === `${anchor.offsetWidth}px` ? update() : requestAnimationFrame(update));
    resizeObserver.observe(anchor);
    resizeObserver.observe(floating);
    window.addEventListener('scroll', update, { capture: true, passive: true });
    window.addEventListener('resize', update, { passive: true });

    return {
        dispose() {
            resizeObserver.disconnect();
            window.removeEventListener('scroll', update, { capture: true });
            window.removeEventListener('resize', update);
        },
    };
}

function position(floating, anchor, options) {
    const anchorRect = anchor.getBoundingClientRect();
    const size = { width: floating.offsetWidth, height: floating.offsetHeight };
    const viewport = { width: document.documentElement.clientWidth, height: document.documentElement.clientHeight };
    const rtl = getComputedStyle(anchor).direction === 'rtl';

    let side = toPhysicalSide(options.side, rtl);
    let point = place(side, options.align, anchorRect, size, options.offset, rtl);
    if (overflows(side, point, size, viewport)) {
        const flippedPoint = place(oppositeSides[side], options.align, anchorRect, size, options.offset, rtl);
        if (!overflows(oppositeSides[side], flippedPoint, size, viewport)) {
            side = oppositeSides[side];
            point = flippedPoint;
        }
    }

    if (side === 'top' || side === 'bottom') {
        point.x = Math.max(viewportPadding, Math.min(point.x, viewport.width - size.width - viewportPadding));
    } else {
        point.y = Math.max(viewportPadding, Math.min(point.y, viewport.height - size.height - viewportPadding));
    }

    floating.style.left = `${point.x}px`;
    floating.style.top = `${point.y}px`;
    floating.dataset.side = toLogicalSide(side, rtl);
    // How far along its edge the anchor's middle falls, so an arrow can point at it after the popup shifts to fit.
    const center = side === 'top' || side === 'bottom'
        ? anchorRect.left + anchorRect.width / 2 - point.x
        : anchorRect.top + anchorRect.height / 2 - point.y;
    floating.style.setProperty('--tb-anchor-center', `${center}px`);
}

function place(side, align, anchorRect, size, offset, rtl) {
    if (side === 'top' || side === 'bottom') {
        const x = alignOnAxis(align, anchorRect.left, anchorRect.width, size.width, rtl);
        return { x, y: side === 'top' ? anchorRect.top - offset - size.height : anchorRect.bottom + offset };
    }

    const y = alignOnAxis(align, anchorRect.top, anchorRect.height, size.height, false);
    return { x: side === 'left' ? anchorRect.left - offset - size.width : anchorRect.right + offset, y };
}

function alignOnAxis(align, anchorStart, anchorLength, length, reversed) {
    if (align === 'Center') {
        return anchorStart + (anchorLength - length) / 2;
    }

    const alignToStartEdge = (align === 'Start') !== reversed;
    return alignToStartEdge ? anchorStart : anchorStart + anchorLength - length;
}

function overflows(side, point, size, viewport) {
    switch (side) {
        case 'top':
            return point.y < viewportPadding;
        case 'bottom':
            return point.y + size.height > viewport.height - viewportPadding;
        case 'left':
            return point.x < viewportPadding;
        default:
            return point.x + size.width > viewport.width - viewportPadding;
    }
}

function toPhysicalSide(side, rtl) {
    switch (side) {
        case 'Top':
            return 'top';
        case 'Bottom':
            return 'bottom';
        case 'Start':
            return rtl ? 'right' : 'left';
        default:
            return rtl ? 'left' : 'right';
    }
}

function toLogicalSide(side, rtl) {
    if (side === 'left') {
        return rtl ? 'end' : 'start';
    }

    if (side === 'right') {
        return rtl ? 'start' : 'end';
    }

    return side;
}
