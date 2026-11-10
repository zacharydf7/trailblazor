// The pad follows the pointer at once; the server hears the new saturation and brightness at most once a frame.
// While dragging, the position goes on the thumb, whose style the server's renders never overwrite, until the server
// has the final position.
export function createColorPad(pad, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!pad) {
        return null;
    }

    const thumb = pad.querySelector('.tb-color-picker__pad-thumb');
    let frame = 0;
    let dragging = false;
    let latest = null;

    const report = () => {
        frame = 0;
        return events.invokeMethodAsync('OnPad', latest.x, 1 - latest.y);
    };
    const move = event => {
        const box = pad.getBoundingClientRect();
        const clamp = value => Math.min(Math.max(value, 0), 1);
        latest = {
            x: clamp((event.clientX - box.left) / box.width),
            y: clamp((event.clientY - box.top) / box.height),
        };
        thumb.style.setProperty('--tb-color-picker-x', latest.x);
        thumb.style.setProperty('--tb-color-picker-y', latest.y);
        frame ||= requestAnimationFrame(report);
    };
    const onPointerDown = event => {
        if (event.button !== 0) {
            return;
        }

        dragging = true;
        pad.setPointerCapture(event.pointerId);
        pad.addEventListener('pointermove', move);
        move(event);
    };
    // Only the end of a drag reports; a right-click or a pointer from elsewhere coming up here mustn't.
    const onPointerUp = event => {
        if (!dragging) {
            return;
        }

        dragging = false;
        pad.releasePointerCapture(event.pointerId);
        pad.removeEventListener('pointermove', move);
        cancelAnimationFrame(frame);
        report().finally(() => {
            if (!dragging) {
                thumb.style.removeProperty('--tb-color-picker-x');
                thumb.style.removeProperty('--tb-color-picker-y');
            }
        });
    };

    pad.addEventListener('pointerdown', onPointerDown);
    pad.addEventListener('pointerup', onPointerUp);
    pad.addEventListener('pointercancel', onPointerUp);
    // Marks that a drag on the pad is heard, which tests wait for.
    pad.dataset.ready = '';
    return {
        dispose() {
            cancelAnimationFrame(frame);
            pad.removeEventListener('pointerdown', onPointerDown);
            pad.removeEventListener('pointerup', onPointerUp);
            pad.removeEventListener('pointercancel', onPointerUp);
            pad.removeEventListener('pointermove', move);
        },
    };
}
