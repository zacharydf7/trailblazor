// The documents the app's elements can be in: the page's own, and the one in each browser window that shows a part of
// the page, such as a dock panel opened in a window of its own. An event in another window is sent on to the page's
// document and window, so a listener there hears every window. What a script reads is different: the focused element,
// an element found by id, the window's size and its frames all belong to one window, so a script asks for them
// through the element it works on (`element.ownerDocument`, and its `defaultView`), each time it needs them, since
// the element can change windows.
const pages = new Set([document]);
const starts = [];
const movers = new Set();
const moved = () => [...movers].forEach(mover => mover());

// Runs `start` with the page's document, and with each other document as it joins, for a script that watches the
// whole page and has no element of its own. What it sets up on a window's document goes with that window.
export function onEachPage(start) {
    starts.push(start);
    pages.forEach(start);
}

// Adds a window's document, once the window holds what it shows. The returned function takes the document away again,
// once what the window showed is back on the page.
export function addPage(page) {
    pages.add(page);
    starts.forEach(start => start(page));
    moved();
    return () => {
        pages.delete(page);
        moved();
    };
}

// Calls `changed` whenever an element may have to change windows: a window's document joined or left, or the user
// went to work in another window. The returned function stops the calls.
export function onPagesChange(changed) {
    movers.add(changed);
    return () => movers.delete(changed);
}

// The document the user is working in: the one their last key, press or focus was in.
let current = document;
for (const type of ['keydown', 'pointerdown', 'focusin']) {
    document.addEventListener(type, event => {
        const page = event.target.ownerDocument ?? current;
        if (page !== current) {
            current = page;
            moved();
        }
    }, true);
}

export const currentPage = () => pages.has(current) ? current : document;

// The documents of the other windows.
export const otherPages = () => [...pages].filter(page => page !== document);

// The element with the id, on the page or in another window, for a caller that has only the id.
export const findById = id => [...pages].map(page => page.getElementById(id)).find(Boolean) ?? null;

// The window an element is in now. An element left behind in a window that has closed is in none, and gets the page's.
export const viewOf = element => element.ownerDocument.defaultView ?? window;

// A ResizeObserver belongs to the window that made it and hears nothing of an element in another one. This one keeps
// an observer in each window that holds an element it watches, and follows an element that changes windows. Like a
// ResizeObserver, it doesn't keep an element it watches in memory.
export function watchSizes(callback) {
    const watched = new WeakMap();
    const targets = new Set();
    const observers = new Map();
    const place = target => {
        const watch = watched.get(target);
        const view = viewOf(target);
        if (view !== watch.view) {
            observers.get(watch.view)?.unobserve(target);
            if (!observers.has(view)) {
                observers.set(view, new view.ResizeObserver(callback));
            }

            observers.get(view).observe(target, watch.options);
            watch.view = view;
        }
    };
    const unobserve = target => {
        const watch = watched.get(target);
        if (watch) {
            observers.get(watch.view)?.unobserve(target);
            targets.delete(watch.held);
            watched.delete(target);
        }
    };
    const settle = () => {
        const used = new Set();
        for (const held of targets) {
            const target = held.deref();
            if (target) {
                place(target);
                used.add(watched.get(target).view);
            } else {
                targets.delete(held);
            }
        }

        // A window that holds nothing watched any more may have closed, and its observer would keep it in memory.
        for (const [view, observer] of observers) {
            if (!used.has(view)) {
                observer.disconnect();
                observers.delete(view);
            }
        }
    };
    return {
        observe(target, options) {
            unobserve(target);
            movers.add(settle);
            const held = new WeakRef(target);
            targets.add(held);
            watched.set(target, { options, view: null, held });
            place(target);
        },
        unobserve,
        disconnect() {
            movers.delete(settle);
            observers.forEach(observer => observer.disconnect());
            observers.clear();
            for (const held of targets) {
                watched.delete(held.deref());
            }

            targets.clear();
        },
    };
}

// One frame at a time for an element, in the window it is in. A frame still waited for when the element changes
// windows is asked for again in the new one: the window it left may close, or be covered, and never draw it.
export function frames(element, callback) {
    let waiting = null;
    const stop = () => {
        waiting?.view.cancelAnimationFrame(waiting.id);
        waiting = null;
        movers.delete(settle);
    };
    const ask = () => {
        const view = viewOf(element);
        movers.add(settle);
        waiting = { view, id: view.requestAnimationFrame(run) };
    };
    const run = () => {
        stop();
        callback();
    };
    const settle = () => {
        if (waiting.view !== viewOf(element)) {
            stop();
            ask();
        }
    };
    return {
        // Asks for the next frame, unless one is waited for already.
        request() {
            if (!waiting) {
                ask();
            }
        },
        cancel: stop,
    };
}
