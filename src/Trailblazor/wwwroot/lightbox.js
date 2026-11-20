import { onEachPage } from '../Trailblazor.Primitives/pages.js';

// A lightbox's keys, dragging and swiping, and the sizes its images fit at. It acts on every lightbox through the
// document, so it needs no setup when a lightbox opens. The keys and gestures press the lightbox's own buttons, so the
// server keeps one way to change the image, zoom or rotation.
const lightboxOf = target => target.nodeType === Node.ELEMENT_NODE ? target.closest('dialog.tb-lightbox') : null;
const press = (lightbox, part) => {
    const button = lightbox.querySelector(`[data-part="${part}"]`);
    if (!button || button.matches(':disabled, [aria-disabled="true"]')) {
        return false;
    }

    button.click();
    return true;
};
const rtl = element => getComputedStyle(element).direction === 'rtl';

// Every key acts only while focus is in the lightbox, so typing elsewhere never triggers one. The arrow keys move
// between images, except on a zoomed image's stage, where they scroll it as on any page.
document.addEventListener('keydown', event => {
    const lightbox = lightboxOf(event.target);
    if (!lightbox || event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey) {
        return;
    }

    const panning = event.target.matches('[data-part="stage"][data-zoomed]');
    const back = rtl(lightbox) ? 'next' : 'previous';
    const on = rtl(lightbox) ? 'previous' : 'next';
    const part = {
        ArrowLeft: panning ? null : back,
        ArrowRight: panning ? null : on,
        '+': 'zoom-in',
        '=': 'zoom-in',
        '-': 'zoom-out',
        0: 'fit',
        r: 'rotate-right',
        R: 'rotate-left',
    }[event.key];
    if (part && press(lightbox, part)) {
        event.preventDefault();
    }
});

// A mouse drags a zoomed image around; a finger scrolls it natively, and swipes between images when it isn't zoomed.
const swipe = 48;
document.addEventListener('pointerdown', event => {
    const stage = event.target.nodeType === Node.ELEMENT_NODE
        ? event.target.closest('.tb-lightbox [data-part="stage"]') : null;
    if (!stage || event.button !== 0) {
        return;
    }

    const start = { x: event.clientX, y: event.clientY, left: stage.scrollLeft, top: stage.scrollTop };
    if (event.pointerType === 'mouse') {
        if (!stage.hasAttribute('data-zoomed')) {
            return;
        }

        // Keeps the browser from selecting or dragging the image while it pans.
        event.preventDefault();
        const move = moved => {
            stage.scrollLeft = start.left - (moved.clientX - start.x);
            stage.scrollTop = start.top - (moved.clientY - start.y);
        };
        stage.setPointerCapture(event.pointerId);
        stage.addEventListener('pointermove', move);
        stage.addEventListener('lostpointercapture', () => stage.removeEventListener('pointermove', move),
            { once: true });
        return;
    }

    if (stage.hasAttribute('data-zoomed')) {
        return;
    }

    // Only this finger's lift counts, and a pinch or scroll the browser takes over ends the gesture with a cancel.
    const listening = new AbortController();
    const lift = lifted => {
        if (lifted.pointerId !== event.pointerId) {
            return;
        }

        listening.abort();
        const dx = lifted.clientX - start.x;
        if (lifted.type === 'pointerup' && Math.abs(dx) >= swipe
            && Math.abs(dx) > 2 * Math.abs(lifted.clientY - start.y)) {
            // A swipe toward the start brings the next image in from the end.
            press(lightboxOf(stage), (dx < 0) !== rtl(stage) ? 'next' : 'previous');
        }
    };
    stage.addEventListener('pointerup', lift, { signal: listening.signal });
    stage.addEventListener('pointercancel', lift, { signal: listening.signal });
});

// The image's own shape and size, which the styles fit to the window and zoom from. Each image gets a new frame.
document.addEventListener('load', event => {
    const image = event.target;
    if (image.localName !== 'img' || !image.matches('.tb-lightbox [data-part="image"]')
        || image.naturalWidth === 0 || image.naturalHeight === 0) {
        return;
    }

    const frame = image.parentElement;
    frame.style.setProperty('--tb-lightbox-ratio', String(image.naturalWidth / image.naturalHeight));
    frame.style.setProperty('--tb-lightbox-natural-width', `${image.naturalWidth}px`);
    frame.dataset.measured = '';
}, true);

// Zooming keeps the middle of the view where it was, and a rotation starts the view from the middle.
const onChanges = records => {
    for (const { target: stage, attributeName, oldValue } of records) {
        if (!stage.matches('.tb-lightbox [data-part="stage"]')) {
            continue;
        }

        const { clientWidth: width, clientHeight: height } = stage;
        const ratio = attributeName === 'data-zoom' ? Number(stage.dataset.zoom) / Number(oldValue) : null;
        const x = ratio ? (Math.abs(stage.scrollLeft) + width / 2) * ratio - width / 2
            : (stage.scrollWidth - width) / 2;
        const y = ratio ? (stage.scrollTop + height / 2) * ratio - height / 2 : (stage.scrollHeight - height) / 2;
        stage.scrollTo((rtl(stage) ? -1 : 1) * Math.max(0, x), Math.max(0, y));
    }
};
onEachPage(page => new MutationObserver(onChanges).observe(page.documentElement,
    { subtree: true, attributes: true, attributeFilter: ['data-zoom', 'data-turns'], attributeOldValue: true }));
