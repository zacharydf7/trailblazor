// The documents the app's elements can be in: the page's own, and the one in each browser window that shows a part of
// the page, such as a dock panel opened in a window of its own. An event in another window is sent on to the page's
// document and window, so a listener there hears every window. What a script reads is different: the focused element,
// an element found by id, the window's size and its frames all belong to one window, so a script asks for them
// through the element it works on (`element.ownerDocument`, and its `defaultView`), each time it needs them, since
// the element can change windows.
const pages = new Set([document]);
const starts = [];
const movers = new Set();
// Followers that this list mustn't keep in memory: those of the page's IntersectionObserver, which any script may
// make and drop. The browser keeps one alive, through the observers it made, for as long as they watch something.
const looseMovers = new Set();
const moved = cause => {
    [...movers].forEach(mover => mover(cause));
    [...looseMovers].forEach(held => held.deref()?.());
};
const heldLoosely = new WeakMap();
const dropped = new FinalizationRegistry(held => looseMovers.delete(held));
const loosely = {
    add(mover) {
        if (!heldLoosely.has(mover)) {
            heldLoosely.set(mover, new WeakRef(mover));
            dropped.register(mover, heldLoosely.get(mover));
        }

        looseMovers.add(heldLoosely.get(mover));
    },
    delete: mover => looseMovers.delete(heldLoosely.get(mover)),
};

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
// went to work in another window, and then with the key, press or focus event that showed it. The returned function
// stops the calls.
export function onPagesChange(changed) {
    movers.add(changed);
    return () => movers.delete(changed);
}

// Makes those calls for a script that moved an element to another window itself, as a dialog that goes to the user.
export const pagesChanged = () => moved();

// The document the user is working in: the one their last key, press or focus was in.
let current = document;
for (const type of ['keydown', 'pointerdown', 'focusin']) {
    document.addEventListener(type, event => {
        const page = /** @type {Node} */ (event.target).ownerDocument ?? current;
        if (page !== current) {
            current = page;
            moved(event);
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

// An observer belongs to the window that made it. This keeps one, made by `create`, in each window that holds an
// element it watches, and joins `followers` to follow an element that changes windows. Like the browser's observers,
// it doesn't keep an element it watches in memory.
function inEachWindow(create, followers) {
    const watched = new WeakMap();
    const targets = new Set();
    const observers = new Map();
    const observerIn = view => {
        if (!observers.has(view)) {
            observers.set(view, create(view));
        }

        return observers.get(view);
    };
    const place = (target, watch) => {
        const view = viewOf(target);
        if (view !== watch.view) {
            observers.get(watch.view)?.unobserve(target);
            observerIn(view).observe(target, watch.options);
            watch.view = view;
        }
    };
    // With nothing left to watch, it stops following, so `followers` doesn't keep it and its callback in memory.
    const rest = () => {
        if (targets.size === 0) {
            followers.delete(settle);
            observers.forEach(observer => observer.disconnect());
            observers.clear();
        }
    };
    const unobserve = target => {
        const watch = watched.get(target);
        if (watch) {
            observers.get(watch.view)?.unobserve(target);
            targets.delete(watch.held);
            watched.delete(target);
            rest();
        }
    };
    const settle = () => {
        const used = new Set();
        for (const held of targets) {
            const target = held.deref();
            if (target) {
                place(target, watched.get(target));
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

        rest();
    };
    return {
        observers,
        watches: target => watched.has(target),
        // The browser's observer refuses what it can't watch before anything is kept of it.
        observe(target, options) {
            unobserve(target);
            const watch = { options, view: null, held: new WeakRef(target) };
            place(target, watch);
            followers.add(settle);
            targets.add(watch.held);
            watched.set(target, watch);
        },
        unobserve,
        disconnect() {
            for (const held of targets) {
                watched.delete(held.deref());
            }

            targets.clear();
            rest();
        },
    };
}

// A ResizeObserver hears nothing of an element in a window other than its own, so this one keeps an observer in each
// window that holds an element it watches.
export function watchSizes(callback) {
    const { observe, unobserve, disconnect } = inEachWindow(view => new view.ResizeObserver(callback), movers);
    return { observe, unobserve, disconnect };
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

// Firefox runs an IntersectionObserver only as the window that made it draws. Blazor's Virtualize makes its own in
// the page's window for a list that may be in another one, so the page's IntersectionObserver is this one from before
// Blazor starts: it keeps a browser's observer in each window that holds a target, and its callback gets this
// observer, not the browser's, as its observer argument and as `this`.
const BrowsersObserver = window.IntersectionObserver;
// @ts-expect-error The static block below adds the option members, which TypeScript can't see.
window.IntersectionObserver = class IntersectionObserver {
    #watch;
    #options;

    constructor(callback, options) {
        // The browser refuses a callback or options it can't use here, as it would for its own.
        new BrowsersObserver(callback, options).disconnect();
        this.#watch = inEachWindow(view => new (view === window ? BrowsersObserver : view.IntersectionObserver)(
            entries => callback.call(this, entries, this), options), loosely);
        this.#options = options;
    }

    // One of the browser's observers with these options: one that watches something, or else one made to ask.
    #any() {
        return this.#watch.observers.values().next().value ?? new BrowsersObserver(() => { }, this.#options);
    }

    // Watching a target again changes nothing, where a ResizeObserver starts over with the new options.
    observe(target) {
        if (!this.#watch.watches(target)) {
            this.#watch.observe(target);
        }
    }

    unobserve(target) {
        this.#watch.unobserve(target);
    }

    disconnect() {
        this.#watch.disconnect();
    }

    takeRecords() {
        return [...this.#watch.observers.values()].flatMap(observer => observer.takeRecords());
    }

    // What the browser's observer says of its options, for each member this browser's has.
    static {
        for (const name of ['root', 'rootMargin', 'scrollMargin', 'thresholds', 'delay', 'trackVisibility']) {
            if (name in BrowsersObserver.prototype) {
                Object.defineProperty(this.prototype, name, {
                    get() {
                        return this.#any()[name];
                    },
                });
            }
        }
    }
};
