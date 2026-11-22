// A browser sends a pointer move when what is under a resting pointer changes, as WebKit does after a list scrolls.
// Such a move isn't the user's, and it comes with the place in its window that the pointer already had.
// Trailblazor's JS initializer imports this at startup, so it is listening before any list loads.
import { onEachPage } from './pages.js';

// Where each kind of pointer last really moved in each window, and where it was the move before. A mouse can rest
// while a finger moves, and in a test browser each window has a pointer of its own.
const places = new WeakMap();
onEachPage(page => page.addEventListener('pointermove', event => {
    // A move that a script made says nothing of where the pointer is.
    if (event.isTrusted) {
        const kinds = places.get(event.view) ?? new Map();
        const previous = kinds.get(event.pointerType);
        kinds.set(event.pointerType, { before: previous?.last, last: { x: event.clientX, y: event.clientY } });
        places.set(event.view, kinds);
    }
}, true));

// Whether this move changed the pointer's place since the move before it from the same kind of pointer in the same
// window; the listener above has already taken this one in. A move that a script made counts. So does a real one
// when its kind of pointer hasn't moved in the window before.
export const pointerMoved = event => {
    if (!event.isTrusted) {
        return true;
    }

    const before = places.get(event.view)?.get(event.pointerType)?.before;
    return before?.x !== event.clientX || before.y !== event.clientY;
};
