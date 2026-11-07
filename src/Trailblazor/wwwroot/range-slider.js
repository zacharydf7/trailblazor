// A range slider's inputs take the pointer only on their thumbs, so a press on the track would reach neither, and thumbs
// on one value can't be told apart until the pointer moves. Those two presses are handled here: the track moves the
// nearer thumb, and thumbs on one value separate in the direction the pointer moves. Any other press on a thumb stays
// native.
const control = '.tb-slider--range .tb-slider__control';

const thumbWidth = where => {
    const probe = document.createElement('div');
    probe.style.position = 'absolute';
    probe.style.inlineSize = 'var(--tb-slider-thumb)';
    where.append(probe);
    const width = probe.getBoundingClientRect().width;
    probe.remove();
    return width;
};

document.addEventListener('pointerdown', event => {
    const where = event.target.closest?.(control);
    const start = where?.querySelector('[data-part="start-thumb"]');
    const end = where?.querySelector('[data-part="end-thumb"]');
    if (!where || event.button !== 0 || event.target === start || event.target === end || start.matches(':disabled')
        || start.getAttribute('aria-readonly') === 'true') {
        return;
    }

    const [min, max, step] = [Number(start.min), Number(start.max), Number(start.step)];
    const thumbSize = thumbWidth(where);
    const box = start.getBoundingClientRect();
    const rtl = getComputedStyle(start).direction === 'rtl';
    const span = (max - min) / (box.width - thumbSize);
    const valueAt = x => min + ((rtl ? box.right - x : x - box.left) - thumbSize / 2) * span;

    const moveTo = (input, value) => {
        const before = input.value;
        // The browser keeps the value to the step and limits, and the slider stops a thumb at the other.
        input.value = String(value);
        if (input.value !== before) {
            input.dispatchEvent(new Event('input', { bubbles: true }));
        }
    };

    const pressed = valueAt(event.clientX);
    const [low, high] = [Number(start.value), Number(end.value)];
    // A press on shared thumbs keeps its offset from them, and picks one once the pointer moves half a step.
    const onThumbs = low === high && Math.abs(pressed - low) <= thumbSize / 2 * span;
    const offset = onThumbs ? pressed - low : 0;
    let input = onThumbs ? null : pressed < (low + high) / 2 ? start : end;
    let moved = false;

    // Focus from a script would show the ring that's for the keyboard, so data-pointer-focus holds it back.
    const focusThumb = () => {
        const thumb = input ?? end;
        thumb.dataset.pointerFocus = '';
        thumb.focus({ preventScroll: true });
    };
    // A finger may be about to scroll, which cancels the press, so it takes focus and moves a thumb only once it slides
    // sideways or lifts.
    const finger = event.pointerType !== 'mouse';
    if (!finger) {
        focusThumb();
        if (input) {
            moveTo(input, pressed);
        }
    }

    const onMove = move => {
        if (move.pointerId !== event.pointerId || (finger && !moved && Math.abs(move.clientX - event.clientX) < 10)) {
            return;
        }

        if (!moved) {
            focusThumb();
        }

        moved = true;
        const value = valueAt(move.clientX) - offset;
        // While the thumbs still share a value, the choice stays open, so a wobble can't lock in the thumb that can't move.
        if (onThumbs && start.value === end.value && Math.abs(value - Number(start.value)) >= step / 2) {
            input = value < Number(start.value) ? start : end;
            focusThumb();
        }

        if (input) {
            moveTo(input, value);
        }
    };
    const onUp = up => {
        if (up.pointerId === event.pointerId && !moved) {
            focusThumb();
            if (input) {
                moveTo(input, pressed);
            }
        }
    };
    const onLost = lost => {
        if (lost.pointerId !== event.pointerId) {
            return;
        }

        where.removeEventListener('pointermove', onMove);
        where.removeEventListener('pointerup', onUp);
        where.removeEventListener('lostpointercapture', onLost);
        if (input && (Number(start.value) !== low || Number(end.value) !== high)) {
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }
    };

    where.setPointerCapture(event.pointerId);
    where.addEventListener('pointermove', onMove);
    where.addEventListener('pointerup', onUp);
    where.addEventListener('lostpointercapture', onLost);
});

// A thumb focused by a press shows its ring once the keyboard takes over.
for (const type of ['keydown', 'focusout']) {
    document.addEventListener(type, event => {
        if (event.target.matches?.(`${control} input`)) {
            delete event.target.dataset.pointerFocus;
        }
    });
}

// A press on anything but an input in the control would take focus from the thumb and start selecting text.
document.addEventListener('mousedown', event => {
    if (event.target.closest?.(control) && event.target.tagName !== 'INPUT') {
        event.preventDefault();
    }
});
