import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
// The pad follows the pointer at once; the server hears the new saturation and brightness when the drag ends, and at
// most every reportEvery milliseconds before then. While dragging, the position goes on the thumb, whose style the
// server's renders never overwrite, until the server has the final position.
const reportEvery = 120;

export function createColorPad(pad, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!pad) {
        return null;
    }

    const thumb = pad.querySelector('.tb-color-picker__pad-thumb');
    let timer = 0;
    let reportedAt = -Infinity;
    let dragging = false;
    let latest = null;

    const report = () => {
        timer = 0;
        reportedAt = performance.now();
        return callDotNet(events, 'OnPad', latest.x, 1 - latest.y);
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
        timer ||= setTimeout(report, Math.max(0, reportedAt + reportEvery - performance.now()));
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
        clearTimeout(timer);
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
            clearTimeout(timer);
            pad.removeEventListener('pointerdown', onPointerDown);
            pad.removeEventListener('pointerup', onPointerUp);
            pad.removeEventListener('pointercancel', onPointerUp);
            pad.removeEventListener('pointermove', move);
        },
    };
}
