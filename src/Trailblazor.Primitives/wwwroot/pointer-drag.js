// Drags items with the pointer, a mouse, pen or finger, without the browser's own drag and drop and its ghost image.
// A mouse or pen starts a drag by moving a few pixels; a finger holds still for a moment first, so a swipe still
// scrolls. Escape cancels. The caller decides what the item is dragged over and what a drop does.
const threshold = 4;
const holdForTouch = 300;
const edge = 32;
const scrollStep = 12;

export function createPointerDrag(container,
    { itemSelector, ignoreSelector, ownsPress = () => true, onStart, onMove, onDrop, onCancel }) {
    let pending = null;
    let drag = null;
    // Set when a drag ends, so the click that may follow doesn't also act on the row. That click comes before a timer
    // can run, so a timer clears the flag after a release, and a key or the next press clears it after a touch drag,
    // which sends no click.
    let justDragged = false;
    const settle = () => setTimeout(() => { justDragged = false; });

    const scrollParent = () => {
        for (let element = container; element; element = element.parentElement) {
            const { overflowY } = getComputedStyle(element);
            if ((overflowY === 'auto' || overflowY === 'scroll') && element.scrollHeight > element.clientHeight) {
                return element;
            }
        }

        return document.scrollingElement;
    };

    // A drag near the top or bottom of what scrolls brings more rows into reach.
    const scrollNearEdge = event => {
        const scroller = scrollParent();
        const box = scroller === document.scrollingElement
            ? { top: 0, bottom: innerHeight } : scroller.getBoundingClientRect();
        if (event.clientY < box.top + edge) {
            scroller.scrollBy(0, -scrollStep);
        } else if (event.clientY > box.bottom - edge) {
            scroller.scrollBy(0, scrollStep);
        }
    };

    const begin = () => {
        drag = pending;
        pending = null;
        clearTimeout(drag.timer);
        onStart(drag.item);
    };

    const end = () => {
        if (pending) {
            clearTimeout(pending.timer);
        }

        pending = null;
        drag = null;
    };

    const onPointerDown = event => {
        justDragged = false;
        const item = event.target.closest?.(itemSelector);
        if (event.button !== 0 || !event.isPrimary || !item || !container.contains(item) || !ownsPress(event.target)
            || (ignoreSelector && event.target.closest(ignoreSelector))) {
            return;
        }

        pending = { item, pointerId: event.pointerId, x: event.clientX, y: event.clientY, touch: false, timer: 0 };
        if (event.pointerType === 'touch') {
            pending.touch = true;
            pending.timer = setTimeout(begin, holdForTouch);
        }
    };

    // Moves and the release are heard on the document, so a drag keeps going when the pointer leaves the container.
    const onPointerMove = event => {
        if (pending && event.pointerId === pending.pointerId) {
            const moved = Math.hypot(event.clientX - pending.x, event.clientY - pending.y) >= threshold;
            if (moved && pending.touch) {
                end();
            } else if (moved) {
                begin();
            }
        }

        if (drag && event.pointerId === drag.pointerId) {
            scrollNearEdge(event);
            onMove(event);
        }
    };

    const onPointerUp = event => {
        if (drag && event.pointerId === drag.pointerId) {
            end();
            justDragged = true;
            settle();
            onDrop();
        } else {
            end();
            // The release after a drag Escape cancelled sends its click now.
            if (justDragged) {
                settle();
            }
        }
    };

    const cancel = () => {
        const wasDragging = drag;
        end();
        if (wasDragging) {
            justDragged = true;
            onCancel();
        }
    };

    const onClick = event => {
        if (justDragged) {
            justDragged = false;
            event.stopPropagation();
        }
    };

    const onKeyDown = event => {
        justDragged = false;
        if (drag && event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            cancel();
        }
    };

    // Text or a link in a row would otherwise start the browser's own drag, which ends this one.
    const onDragStart = event => {
        if (pending || drag) {
            event.preventDefault();
        }
    };

    // A finger that started a drag mustn't scroll the page as it moves.
    const onTouchMove = event => {
        if (drag) {
            event.preventDefault();
        }
    };

    container.addEventListener('pointerdown', onPointerDown);
    container.addEventListener('click', onClick, { capture: true });
    container.addEventListener('dragstart', onDragStart);
    container.addEventListener('touchmove', onTouchMove, { passive: false });
    document.addEventListener('pointermove', onPointerMove);
    document.addEventListener('pointerup', onPointerUp);
    document.addEventListener('pointercancel', cancel);
    document.addEventListener('keydown', onKeyDown, { capture: true });
    return {
        dispose() {
            end();
            container.removeEventListener('pointerdown', onPointerDown);
            container.removeEventListener('click', onClick, { capture: true });
            container.removeEventListener('dragstart', onDragStart);
            container.removeEventListener('touchmove', onTouchMove, { passive: false });
            document.removeEventListener('pointermove', onPointerMove);
            document.removeEventListener('pointerup', onPointerUp);
            document.removeEventListener('pointercancel', cancel);
            document.removeEventListener('keydown', onKeyDown, { capture: true });
        },
    };
}
