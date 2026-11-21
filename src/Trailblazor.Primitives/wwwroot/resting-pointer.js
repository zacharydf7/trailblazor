// A browser sends a pointer move when what is under a resting pointer changes, as WebKit does after a list scrolls.
// Such a move isn't the user's, and it comes with the place in its window that the pointer already had.
// Trailblazor's JS initializer imports this at startup, so it is listening before any list loads.
import { onEachPage } from './pages.js';

let last = null;
let before = null;
onEachPage(page => page.addEventListener('pointermove', event => {
    // A move that a script made says nothing of where the pointer is.
    if (event.isTrusted) {
        before = last;
        last = { view: event.view, x: event.clientX, y: event.clientY };
    }
}, true));

// Whether the pointer's place changed with this move, from the move before it, since the listener above has taken
// this one in already. A move that a script made counts. So does a real one when the real move before it was in
// another window, or when there was none.
export const pointerMoved = event => !event.isTrusted || before?.view !== event.view || before.x !== event.clientX
    || before.y !== event.clientY;
