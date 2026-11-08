// A range slider's inputs take the pointer only on their thumbs, so a press on the track would reach neither, and thumbs
// that overlap can't be told apart until the pointer moves. Those two presses are handled here: the track moves the
// nearer thumb, and a press on both thumbs moves the one on the side the pointer heads to. Any other press on a thumb
// stays native.
const control = '.tb-slider--range .tb-slider__control';

// During a press on both thumbs, the browser mustn't drag the one on top natively, since it would stop at the other.
const held = ['mousedown', 'touchstart', 'touchmove', 'touchend'];
const hold = event => event.preventDefault();

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
    if (!where || event.button !== 0 || start.matches(':disabled') || start.getAttribute('aria-readonly') === 'true') {
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
    // A press on both thumbs moves the one on the side the pointer heads to, which keeps its offset from the pointer.
    const reach = thumbSize / 2 * span;
    const onThumbs = Math.abs(pressed - low) <= reach && Math.abs(pressed - high) <= reach;
    const pressedThumb = event.target === start || event.target === end ? event.target : null;
    if (pressedThumb && !onThumbs) {
        return;
    }

    if (pressedThumb) {
        for (const type of held) {
            pressedThumb.addEventListener(type, hold, { passive: false });
        }
    }

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
        if (move.pointerId !== event.pointerId) {
            return;
        }

        if (finger && !moved && Math.abs(move.clientX - event.clientX) < 10) {
            // A finger held on both thumbs can't scroll the page, so a swipe up or down lets go rather than focusing.
            if (pressedThumb && Math.abs(move.clientY - event.clientY) >= 10) {
                where.releasePointerCapture(event.pointerId);
            }

            return;
        }

        if (!moved) {
            focusThumb();
        }

        moved = true;
        const at = valueAt(move.clientX);
        // The choice follows the pointer back past the press, and the thumb left behind returns, so a wobble can't
        // lock in the wrong thumb.
        const choice = !onThumbs ? input : at - pressed <= -step / 2 ? start : at - pressed >= step / 2 ? end : input;
        if (choice !== input) {
            if (input) {
                moveTo(input, input === start ? low : high);
            }

            input = choice;
            focusThumb();
        }

        if (input) {
            moveTo(input, onThumbs ? (input === start ? low : high) + at - pressed : at);
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
        if (pressedThumb) {
            for (const type of held) {
                pressedThumb.removeEventListener(type, hold);
            }
        }

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
