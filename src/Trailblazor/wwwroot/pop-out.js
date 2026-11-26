// Shows an element in a browser window of its own while it stays part of the page that made it: Blazor keeps
// rendering it there, and its components keep their state.
import { addPage, otherPages } from '../Trailblazor.Primitives/pages.js';

// The events Blazor has handlers for, and those the library's scripts hear on the page's document, as they bubble up
// to it, or happen on it as `selectionchange` does. The library's own events, such as the one a tag input sends for a
// new tag, join them as they are registered.
const bubbling = new Set(['click', 'dblclick', 'auxclick', 'contextmenu', 'mousedown', 'mouseup', 'mousemove',
    'mouseover', 'mouseout', 'pointerdown', 'pointerup', 'pointermove', 'pointerover', 'pointerout', 'pointercancel',
    'gotpointercapture', 'lostpointercapture', 'touchstart', 'touchend', 'touchmove', 'touchcancel', 'keydown',
    'keyup', 'keypress', 'beforeinput', 'input', 'change', 'focusin', 'focusout', 'select', 'selectstart', 'submit',
    'reset', 'wheel', 'mousewheel', 'copy', 'cut', 'paste', 'compositionstart', 'compositionupdate', 'compositionend',
    'dragstart', 'drag', 'dragend', 'dragenter', 'dragleave', 'dragover', 'drop', 'fullscreenchange',
    'fullscreenerror', 'selectionchange', 'visibilitychange']);
export const forwardToo = type => bubbling.add(type);
// The ones that don't bubble, which are heard on their way down: a control's, a dialog's, and an audio or video
// element's.
const captured = ['focus', 'blur', 'scroll', 'mouseenter', 'mouseleave', 'pointerenter', 'pointerleave', 'toggle',
    'beforetoggle', 'invalid', 'load', 'error', 'cancel', 'close', 'abort', 'canplay', 'canplaythrough', 'cuechange',
    'durationchange', 'emptied', 'ended', 'loadeddata', 'loadedmetadata', 'loadend', 'loadstart', 'pause', 'play',
    'playing', 'progress', 'ratechange', 'seeked', 'seeking', 'stalled', 'suspend', 'timeupdate', 'volumechange',
    'waiting'];

// Blazor listens on the page's document only, so each event in the other window is sent on to the page, with the
// element it happened on and the path it took. An event on an element goes to the page's root element, so it passes
// the page's document as it would have on the page: a listener that captures there hears it first, and one that
// stops it at the root keeps it from the document. An event on the other document itself, as `selectionchange` is,
// goes to the page's document.
const forward = event => {
    const Type = window[event.constructor.name] ?? Event;
    const copy = new Type(event.type, event);
    const path = event.composedPath();
    Object.defineProperty(copy, 'target', { value: event.target });
    // WebKit drops a related element that is in another document than the copy, such as the one the focus went to.
    if (event.relatedTarget) {
        Object.defineProperty(copy, 'relatedTarget', { value: event.relatedTarget });
    }

    // A copy each time: Blazor takes the path apart as it reads it, and reads it once for each name an event has.
    Object.defineProperty(copy, 'composedPath', { value: () => [...path] });
    // A default that a listener in the window prevented stays prevented on the page, where Blazor would otherwise
    // follow a link the listener kept. Properties that listeners there set on the event go with it, as a data grid
    // notes which key's default it prevented itself.
    if (event.defaultPrevented) {
        copy.preventDefault();
    }

    // WebKit acts on a key's copy as if the key were pressed on the page: it scrolls the page, and goes back a page
    // where Backspace does that. So the copy arrives prevented, and what its listeners prevent is tracked separately.
    if (copy instanceof KeyboardEvent) {
        let prevented = event.defaultPrevented;
        copy.preventDefault();
        Object.defineProperties(copy, {
            preventDefault: { value: () => { prevented = true; } },
            defaultPrevented: { get: () => prevented },
            returnValue: { get: () => !prevented, set: value => { prevented ||= !value; } },
        });
    }

    for (const [name, value] of Object.entries(event).filter(([name]) => name !== 'isTrusted')) {
        copy[name] = value;
    }

    (event.target === event.currentTarget ? document : document.documentElement).dispatchEvent(copy);
    if (copy.defaultPrevented) {
        event.preventDefault();
    }
};

// A script can turn a sheet off or on through `document.styleSheets` with no attribute changing, and no event tells of
// it, so while a window follows the page's sheets each sheet gets a `disabled` of its own that tells the followers.
const sheetFollowers = new Set();
const watchedSheets = new WeakSet();
const disabledProperty = Object.getOwnPropertyDescriptor(StyleSheet.prototype, 'disabled');
const watchDisabled = sheet => {
    if (!Object.hasOwn(sheet, 'disabled')) {
        watchedSheets.add(sheet);
        Object.defineProperty(sheet, 'disabled', {
            configurable: true,
            get: () => disabledProperty.get.call(sheet),
            set: value => {
                disabledProperty.set.call(sheet, value);
                sheetFollowers.forEach(follow => follow());
            },
        });
    }
};

// Chromium and WebKit report a sheet that the page only offers as an alternate as on whether it applies or not. To
// find out, a rule for the page's head goes into the sheet for a moment, which changes nothing a user can see. A
// sheet whose rules a script can't read, as most from another origin, is taken as not applying. The rule takes no
// effect while the sheet's media doesn't match, so the answer only counts while it does.
const applies = sheet => {
    try {
        const at = sheet.insertRule('head { --_tb-sheet-applies: 1 }', sheet.cssRules.length);
        const found = getComputedStyle(document.head).getPropertyValue('--_tb-sheet-applies') !== '';
        sheet.deleteRule(at);
        return found;
    } catch {
        return false;
    }
};

// A script turns on a sheet the page only offers as an alternate by setting its link's `disabled` and clearing it
// again. Done in one go, that leaves the attributes as they were, so the copy does the same.
const turnOn = copy => {
    copy.disabled = true;
    copy.disabled = false;
};

// The other window's styles follow the page's: its style sheets, as its head gains, loses and changes them, and
// whatever the page sets on its root element.
const mirror = other => {
    const copies = new Map();
    const asked = new WeakSet();
    // Media queries, keyed by their text, that alternates are waiting on before they're asked.
    const awaited = new Map();
    // An alternate that a script turned on before this window saw it left no trace in its attributes, so each is
    // asked once for its copy, when its sheet is first there and its media matches.
    const askAlternate = (sheet, copy) => {
        if (asked.has(copy)) {
            return;
        }

        const query = sheet.media && (awaited.get(sheet.media) ?? matchMedia(sheet.media));
        if (query && !query.matches) {
            if (!awaited.has(sheet.media)) {
                awaited.set(sheet.media, query);
                query.addEventListener('change', copySheets);
            }

            return;
        }

        asked.add(copy);
        if (applies(sheet.sheet)) {
            turnOn(copy);
        }
    };
    const copySheets = () => {
        const sheets = [...document.querySelectorAll('link[rel~="stylesheet"], style')];
        for (const [sheet, copy] of copies) {
            if (!sheets.includes(sheet)) {
                copy.remove();
                copies.delete(sheet);
            }
        }

        const head = other.document.head;
        let last = null;
        for (const sheet of sheets) {
            // A copy takes the page's first nonce before it's inserted, since the window inherits the page's Content
            // Security Policy, which an enhanced navigation's new nonces don't change.
            if (!copies.has(sheet)) {
                copies.set(sheet, Object.assign(other.document.createElement(sheet.localName),
                    { nonce: globalThis.trailblazorNonce || sheet.nonce }));
            }

            // The copies keep the page's order, which decides whose rule wins. Only a copy that is out of place moves,
            // since a link that moves loads its sheet again.
            const copy = copies.get(sheet);
            const place = last ? last.nextSibling : head.firstChild;
            if (copy !== place) {
                head.insertBefore(copy, place);
            }

            last = copy;
            // A value is set only when it differs, since setting a link's address loads its sheet again. A link that
            // the page has turned off, or offers as an alternate, is the same in the window.
            const followed = sheet.localName === 'link' ? ['rel', 'title', 'media', 'disabled', 'href']
                : ['media', 'textContent'];
            for (const name of followed.filter(each => copy[each] !== sheet[each])) {
                copy[name] = sheet[name];
            }

            // A link's `sheet` is null until it has loaded, on the page and in the window.
            if (sheet.sheet) {
                watchDisabled(sheet.sheet);
                if (copy.sheet && copy.sheet.disabled !== sheet.sheet.disabled) {
                    copy.sheet.disabled = sheet.sheet.disabled;
                }

                if (sheet.matches('link[rel~="alternate"]')) {
                    askAlternate(sheet, copy);
                }
            }
        }
    };
    const onLoad = event => {
        if (event.target.localName === 'link') {
            copySheets();
        }
    };
    const copyRoot = () => {
        const [from, to] = [document.documentElement, other.document.documentElement];
        for (const name of to.getAttributeNames()) {
            to.removeAttribute(name);
        }

        for (const name of from.getAttributeNames()) {
            to.setAttribute(name, from.getAttribute(name));
        }
    };
    copySheets();
    copyRoot();
    const sheets = new MutationObserver(records => {
        copySheets();
        const toggled = records.filter(record => record.attributeName === 'disabled').map(record => record.target);
        for (const link of new Set(toggled)) {
            const copy = copies.get(link);
            if (copy && !link.disabled) {
                turnOn(copy);
            }
        }
    });
    sheets.observe(document.head, { subtree: true, childList: true, characterData: true, attributes: true,
        attributeFilter: ['href', 'media', 'rel', 'title', 'disabled'] });
    const root = new MutationObserver(copyRoot);
    root.observe(document.documentElement, { attributes: true });
    sheetFollowers.add(copySheets);
    document.addEventListener('load', onLoad, true);
    other.document.addEventListener('load', onLoad, true);
    return () => {
        sheets.disconnect();
        root.disconnect();
        document.removeEventListener('load', onLoad, true);
        other.document.removeEventListener('load', onLoad, true);
        sheetFollowers.delete(copySheets);
        awaited.forEach(media => media.removeEventListener('change', copySheets));
        if (sheetFollowers.size === 0) {
            for (const sheet of [...document.styleSheets].filter(each => watchedSheets.delete(each))) {
                delete sheet.disabled;
            }
        }
    };
};

// A theme, a palette, a density, a direction or a language set on something around the element, and not on the
// page's root, would be left behind, so the other window's body takes the nearest of each.
const scope = (element, body) => {
    for (const name of ['data-tb-theme', 'data-tb-palette', 'data-tb-density', 'dir', 'lang']) {
        const holder = element.parentElement?.closest(`[${name}]`);
        if (holder && holder !== document.documentElement) {
            body.setAttribute(name, holder.getAttribute(name));
        }
    }

    // Custom properties set in a style attribute around the element are inherited values now, so they are copied.
    for (let holder = element.parentElement; holder; holder = holder.parentElement) {
        for (const name of holder.style) {
            if (name.startsWith('--') && !body.style.getPropertyValue(name)) {
                body.style.setProperty(name, holder.style.getPropertyValue(name));
            }
        }
    }
};

// Blazor finds the element behind a reference, to hand it to a script or to focus it, by asking the page's document
// for the attribute it marked the element with. While a window shows a part of the page, a mark the page doesn't have
// is looked for in the windows too; without that, a component drawn in a window would never get its script.
const findMarked = selector => Document.prototype.querySelector.call(document, selector)
    ?? (selector.startsWith('[_bl_') ? otherPages().map(page => page.querySelector(selector)).find(Boolean) ?? null
        : null);

// A screen reader follows the window that has the focus, and the app's live regions are on the page. So the other
// window gets regions of its own, empty at first since text a live region arrives with isn't spoken, and every
// message added to the page's regions after that is added to them too. A modal dialog's own regions stay with the
// dialog.
const repeatAnnouncements = page => {
    const source = document.querySelector('[data-part="announcer"]:not(dialog *)');
    if (!source) {
        return () => { };
    }

    // Each copy has its element's attributes and none of what it holds.
    const host = page.body.appendChild(page.importNode(source));
    const regions = new Map([...source.children].map(region => [region, host.appendChild(page.importNode(region))]));
    const said = new Map();
    const follow = new MutationObserver(records => {
        for (const record of records) {
            for (const message of record.addedNodes) {
                said.set(message, regions.get(record.target).appendChild(page.createElement('p')));
                said.get(message).textContent = message.textContent;
            }

            for (const message of record.removedNodes) {
                said.get(message)?.remove();
                said.delete(message);
            }
        }
    });
    for (const region of regions.keys()) {
        follow.observe(region, { childList: true });
    }

    return () => follow.disconnect();
};

// What a window says of itself, that its size changed or that it lost the focus, is sent on to the page's window.
const ofTheWindow = ['resize', 'blur'];

// How long the window's linked sheets get to load before the element moves in without them, in milliseconds.
const sheetsLimit = 3000;
const sheetsLoaded = page => {
    const loading = [...page.head.querySelectorAll('link[rel~="stylesheet"]:not([disabled])')]
        .filter(link => !link.sheet);
    return Promise.race([new Promise(resolve => setTimeout(resolve, sheetsLimit)),
        Promise.all(loading.map(link => new Promise(resolve => {
            link.addEventListener('load', resolve, { once: true });
            link.addEventListener('error', resolve, { once: true });
        })))]);
};

// Moves the element into the other window, which the caller opened, names the window and gives its body a class.
// The element moves once the window's linked sheets have loaded, or their time is up, so it seldom shows unstyled
// there and a script that measures it finds it laid out; the promise gives null when the window was closed
// meanwhile, or when the element left the page, and then it closes the window. The caller moves the element back, and
// then calls the function the promise gives, which stops following the page's styles and repeating its announcements.
export async function showInWindow(other, element, title, className) {
    const page = other.document;
    // A window opens on a blank page in quirks mode, which lays out and measures some things differently from the
    // page that made it, so the blank page is written again with a doctype.
    page.open();
    page.write('<!DOCTYPE html>');
    page.close();
    page.title = title;
    // The blank page has the page's address but not its `<base>`, which a Blazor app's relative addresses count on.
    page.head.appendChild(page.createElement('base')).href = document.baseURI;
    const unstyle = mirror(other);
    page.body.className = className;
    scope(element, page.body);
    const unsay = repeatAnnouncements(page);
    const stop = () => {
        unstyle();
        unsay();
    };
    await sheetsLoaded(page);
    // The element can have left the page meanwhile too, and the window would then show a dead copy of it.
    if (other.closed || !element.isConnected) {
        stop();
        other.close();
        return null;
    }

    page.body.appendChild(element);
    for (const type of bubbling) {
        page.addEventListener(type, forward);
    }

    for (const type of captured) {
        page.addEventListener(type, forward, true);
    }

    for (const type of ofTheWindow) {
        other.addEventListener(type, () => window.dispatchEvent(new Event(type)));
    }

    const leave = addPage(page);
    document.querySelector = findMarked;
    return () => {
        stop();
        leave();
        if (otherPages().length === 0) {
            delete document.querySelector;
        }
    };
}
