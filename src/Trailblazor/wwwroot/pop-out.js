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
    'fullscreenerror', 'selectionchange']);
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
    // A copy each time: Blazor takes the path apart as it reads it, and reads it once for each name an event has.
    Object.defineProperty(copy, 'composedPath', { value: () => [...path] });
    // A default that a listener in the window prevented stays prevented on the page, where Blazor would otherwise
    // follow a link the listener kept. Properties that listeners there set on the event go with it, as a data grid
    // notes which key's default it prevented itself.
    if (event.defaultPrevented) {
        copy.preventDefault();
    }

    for (const [name, value] of Object.entries(event).filter(([name]) => name !== 'isTrusted')) {
        copy[name] = value;
    }

    (event.target === event.currentTarget ? document : document.documentElement).dispatchEvent(copy);
    if (copy.defaultPrevented) {
        event.preventDefault();
    }
};

// The other window's styles follow the page's: its style sheets, as its head gains, loses and changes them, and
// whatever the page sets on its root element.
const mirror = other => {
    const copies = new Map();
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
            if (!copies.has(sheet)) {
                copies.set(sheet, other.document.createElement(sheet.localName));
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
    const sheets = new MutationObserver(copySheets);
    sheets.observe(document.head, { subtree: true, childList: true, characterData: true, attributes: true,
        attributeFilter: ['href', 'media', 'rel', 'title', 'disabled'] });
    const root = new MutationObserver(copyRoot);
    root.observe(document.documentElement, { attributes: true });
    return () => {
        sheets.disconnect();
        root.disconnect();
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
// meanwhile. The caller moves the element back, and then calls the function the promise gives, which stops following
// the page's styles and repeating its announcements.
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
    if (other.closed) {
        stop();
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
