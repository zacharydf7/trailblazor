import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { isComposing } from '../Trailblazor.Primitives/composing.js';

// How much a pixel of wheel movement zooms, as a power, so each notch zooms by the same ratio.
const wheelZoom = 0.002;
// A wheel or a pinch reports its zoom once it pauses, rather than once a frame.
const settleDelay = 150;
// A drag pans only once the pointer has moved this far, so a click on the content stays a click.
const panThreshold = 4;
// A press on one of these starts no pan, since the element handles the press itself.
const ownPress = 'a[href], button, input, select, textarea, label, summary, [contenteditable="true"], [tabindex]';

// Zooms and pans a viewport's content in the browser. The content scales with CSS zoom, so the box scrolls over its
// whole zoomed size, and a zoom keeps the point under the pointer, or at the box's center, where it was. `reporter`
// hears each zoom the keys, the wheel and a pinch make, and each one a fit makes as the box or the content resizes.
export function attachViewport(root, reporter) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    const surface = root.querySelector(':scope > [data-part=surface]');
    const content = surface.querySelector(':scope > [data-part=content]');
    // The server renders the zoom it has into the content's style, which the script takes over from.
    let zoom = Number(getComputedStyle(content).zoom) || 1;
    let fit = 'None';
    let limits = { min: 0.1, max: 8, step: 1.25 };
    let drag = true;
    let settleTimer = 0;

    const clamp = value => Math.min(Math.max(value, limits.min), limits.max);

    // Scales the content around a point in the window, which stays over the same spot of the content.
    const zoomTo = (next, around) => {
        next = clamp(next);
        const box = surface.getBoundingClientRect();
        const point = around ?? { x: box.left + surface.clientWidth / 2, y: box.top + surface.clientHeight / 2 };
        const before = content.getBoundingClientRect();
        const spot = { x: (point.x - before.left) / zoom, y: (point.y - before.top) / zoom };
        zoom = next;
        content.style.zoom = String(next);
        const after = content.getBoundingClientRect();
        surface.scrollLeft += after.left + spot.x * next - point.x;
        surface.scrollTop += after.top + spot.y * next - point.y;
    };

    // The zoom that fits the content's own size into the box, which keeps room for a scrollbar so fitting the width
    // can't bring one in and change the width it fits.
    const fitZoom = () => {
        const box = content.getBoundingClientRect();
        const width = box.width / zoom;
        const height = box.height / zoom;
        if (!width || !height) {
            return zoom;
        }

        const across = surface.clientWidth / width;
        return clamp(fit === 'Width' ? across : Math.min(across, surface.clientHeight / height));
    };

    const report = input => {
        clearTimeout(settleTimer);
        callDotNet(reporter, 'OnZoom', zoom, fit, input);
    };

    const reportWhenSettled = input => {
        clearTimeout(settleTimer);
        settleTimer = setTimeout(() => report(input), settleDelay);
    };

    // A zoom the user makes leaves any fit.
    const zoomByUser = (next, around, input, settled) => {
        const before = zoom;
        zoomTo(next, around);
        if (zoom === before && fit === 'None') {
            return;
        }

        fit = 'None';
        if (settled) {
            reportWhenSettled(input);
        } else {
            report(input);
        }
    };

    const onKeyDown = event => {
        if (event.target !== surface || event.ctrlKey || event.metaKey || event.altKey || isComposing(event)) {
            return;
        }

        const next = { '+': zoom * limits.step, '=': zoom * limits.step, '-': zoom / limits.step,
            '_': zoom / limits.step, '0': 1 }[event.key];
        if (next !== undefined) {
            event.preventDefault();
            zoomByUser(next, null, 'keyboard', false);
        }
    };

    // Ctrl with the wheel, which is also what a trackpad's pinch sends, zooms around the pointer.
    const onWheel = event => {
        if (!event.ctrlKey && !event.metaKey) {
            return;
        }

        event.preventDefault();
        const pixels = event.deltaY * (event.deltaMode === WheelEvent.DOM_DELTA_LINE ? 16 : 1);
        zoomByUser(zoom * Math.exp(-pixels * wheelZoom), { x: event.clientX, y: event.clientY }, 'pointer', true);
    };

    // One pointer pans; a second one down pinches, zooming by how far apart the two move around their midpoint.
    const pointers = new Map();
    let pan = null;
    let pinch = null;
    const midpoint = () => {
        const [a, b] = [...pointers.values()];
        return { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2, apart: Math.hypot(a.x - b.x, a.y - b.y) };
    };

    const onPointerDown = event => {
        const owner = event.target.closest(ownPress);
        if (!drag || event.button !== 0 || owner && owner !== surface) {
            return;
        }

        // A mouse or pen let go outside the box before a pan started never reports its release here, so its stale entry
        // is cleared before it can turn this press into a pinch.
        if (event.pointerType !== 'touch') {
            pointers.clear();
        }

        pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
        if (pointers.size === 2) {
            pan = null;
            const middle = midpoint();
            pinch = { apart: middle.apart || 1, zoom };
            pointers.forEach((_, id) => surface.setPointerCapture(id));
            return;
        }

        pan = { x: event.clientX, y: event.clientY, left: surface.scrollLeft, top: surface.scrollTop, moved: false };
        // A mouse press would select text as it drags, so the press focuses the box itself.
        if (event.pointerType === 'mouse') {
            event.preventDefault();
            surface.focus({ preventScroll: true });
        }
    };

    const onPointerMove = event => {
        if (!pointers.has(event.pointerId)) {
            return;
        }

        // A mouse or pen let go outside the box before a pan started comes back with no button held.
        if (event.pointerType !== 'touch' && event.buttons === 0) {
            onPointerUp(event);
            return;
        }

        pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
        if (pinch && pointers.size === 2) {
            const middle = midpoint();
            zoomByUser(pinch.zoom * middle.apart / pinch.apart, middle, 'pointer', true);
            return;
        }

        if (!pan) {
            return;
        }

        const dx = event.clientX - pan.x;
        const dy = event.clientY - pan.y;
        if (!pan.moved && Math.hypot(dx, dy) < panThreshold) {
            return;
        }

        // Capturing only once a pan starts leaves a plain click on the content to the element it lands on.
        if (!pan.moved) {
            pan.moved = true;
            surface.setPointerCapture(event.pointerId);
        }

        root.toggleAttribute('data-panning', true);
        surface.scrollLeft = pan.left - dx;
        surface.scrollTop = pan.top - dy;
    };

    // An image's own drag would carry it away instead of panning.
    const onDragStart = event => {
        if (drag) {
            event.preventDefault();
        }
    };

    const onPointerUp = event => {
        if (!pointers.delete(event.pointerId)) {
            return;
        }

        pinch = null;
        pan = null;
        root.toggleAttribute('data-panning', false);
    };

    // A fit follows the box and the content as they resize, such as when an image loads.
    const resizes = new ResizeObserver(() => {
        if (fit === 'None') {
            return;
        }

        const next = fitZoom();
        if (Math.abs(next - zoom) > 1e-4) {
            zoomTo(next);
            reportWhenSettled('resize');
        }
    });
    resizes.observe(surface);
    resizes.observe(content);

    surface.addEventListener('keydown', onKeyDown);
    surface.addEventListener('wheel', onWheel, { passive: false });
    surface.addEventListener('pointerdown', onPointerDown);
    surface.addEventListener('pointermove', onPointerMove);
    surface.addEventListener('pointerup', onPointerUp);
    surface.addEventListener('pointercancel', onPointerUp);
    surface.addEventListener('dragstart', onDragStart);
    return {
        // The server's zoom, fit and limits; a fit works out its own zoom. Returns the zoom applied.
        update(nextZoom, nextFit, min, max, step, dragToPan) {
            limits = { min, max, step };
            drag = dragToPan;
            fit = nextFit;
            zoomTo(fit === 'None' ? nextZoom : fitZoom());
            // Ready once the script has the server's zoom, which would otherwise undo a zoom made before it came.
            root.toggleAttribute('data-ready', true);
            return zoom;
        },
        dispose() {
            root.toggleAttribute('data-ready', false);
            clearTimeout(settleTimer);
            resizes.disconnect();
            surface.removeEventListener('keydown', onKeyDown);
            surface.removeEventListener('wheel', onWheel);
            surface.removeEventListener('pointerdown', onPointerDown);
            surface.removeEventListener('pointermove', onPointerMove);
            surface.removeEventListener('pointerup', onPointerUp);
            surface.removeEventListener('pointercancel', onPointerUp);
            surface.removeEventListener('dragstart', onDragStart);
        },
    };
}
