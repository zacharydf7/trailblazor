const svgNamespace = 'http://www.w3.org/2000/svg';
const width = 600;
const height = 200;

// A press on the pad draws a stroke until it lifts, in the pad's own space, whatever size it's shown at. The stroke
// stays in the draft layer until the server draws it with the rest.
export function createSignaturePad(surface) {
    if (!surface) {
        return null;
    }

    const draft = surface.querySelector('[data-part=draft]');
    const ink = surface.querySelector('svg:not([data-part=draft])');
    let points = null;
    let path = null;
    // Strokes sent but not yet drawn by the server, oldest first; each stroke it draws takes the oldest away.
    const sent = [];

    const pointOf = event => {
        const box = surface.getBoundingClientRect();
        return [Math.round((event.clientX - box.left) / box.width * width * 10) / 10,
            Math.round((event.clientY - box.top) / box.height * height * 10) / 10];
    };
    const draw = () => path.setAttribute('d', `M${points.map(point => point.join(' ')).join(' L')}`);
    const start = event => {
        if (event.button !== 0 || surface.hasAttribute('data-locked')) {
            return;
        }

        event.preventDefault();
        surface.setPointerCapture(event.pointerId);
        points = [pointOf(event)];
        path = document.createElementNS(svgNamespace, 'path');
        draft.append(path);
        draw();
    };
    const move = event => {
        if (!points) {
            return;
        }

        // Points closer than a unit to the last one add nothing visible, and skipping them keeps a long stroke's message
        // small enough for the server.
        for (const each of event.getCoalescedEvents?.() ?? [event]) {
            const point = pointOf(each);
            const [x, y] = points.at(-1);
            if (Math.hypot(point[0] - x, point[1] - y) >= 1) {
                points.push(point);
            }
        }

        draw();
    };
    // A tap sends a lone point, which the pad draws as a dot.
    const end = () => {
        if (!points) {
            return;
        }

        const stroke = points.flat();
        points = null;
        sent.push(path);
        surface.dispatchEvent(new CustomEvent('tb-signature-stroke', { bubbles: true, detail: { points: stroke } }));
    };
    const drawn = new MutationObserver(records => {
        const added = records.reduce((count, record) => count + record.addedNodes.length, 0);
        sent.splice(0, added).forEach(stroke => stroke.remove());
    });

    surface.addEventListener('pointerdown', start);
    surface.addEventListener('pointermove', move);
    surface.addEventListener('pointerup', end);
    surface.addEventListener('pointercancel', end);
    drawn.observe(ink, { childList: true });
    surface.setAttribute('data-ready', '');
    return {
        dispose() {
            surface.removeEventListener('pointerdown', start);
            surface.removeEventListener('pointermove', move);
            surface.removeEventListener('pointerup', end);
            surface.removeEventListener('pointercancel', end);
            drawn.disconnect();
            surface.removeAttribute('data-ready');
        },
    };
}

// The browser draws the signature's SVG onto a canvas, which encodes it as a PNG.
export async function toPng(svg, imageWidth, imageHeight) {
    const url = URL.createObjectURL(new Blob([svg], { type: 'image/svg+xml' }));
    try {
        const image = new Image();
        image.src = url;
        await image.decode();
        const canvas = document.createElement('canvas');
        canvas.width = imageWidth;
        canvas.height = imageHeight;
        canvas.getContext('2d').drawImage(image, 0, 0, imageWidth, imageHeight);
        const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
        return new Uint8Array(await blob.arrayBuffer());
    } finally {
        URL.revokeObjectURL(url);
    }
}
