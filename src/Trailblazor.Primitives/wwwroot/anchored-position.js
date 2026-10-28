const oppositeSides = { top: 'bottom', bottom: 'top', left: 'right', right: 'left' };
const viewportPadding = 8;

export function createAnchoredPosition(floating, anchor, options) {
    // Fixed coordinates are viewport-relative only in the top layer or without transformed ancestors.
    floating.style.position = 'fixed';
    floating.style.inset = 'auto';
    floating.style.margin = '0';

    const update = () => position(floating, anchor, options);
    update();

    const resizeObserver = new ResizeObserver(update);
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
