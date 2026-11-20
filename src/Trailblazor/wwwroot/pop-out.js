// Shows an element in a browser window of its own while it stays part of the page that made it: Blazor keeps
// rendering it there, and its components keep their state.

// The events Blazor has handlers for, and those the library's scripts hear on the page's document, as they bubble up
// to it. The library's own events, such as the one a tag input sends for a new tag, join them as they are registered.
// `selectionchange` is left out: the scripts that hear it read the page's own selection, not the other window's.
const bubbling = new Set(['click', 'dblclick', 'auxclick', 'contextmenu', 'mousedown', 'mouseup', 'mousemove',
    'mouseover', 'mouseout', 'pointerdown', 'pointerup', 'pointermove', 'pointerover', 'pointerout', 'pointercancel',
    'gotpointercapture', 'lostpointercapture', 'touchstart', 'touchend', 'touchmove', 'touchcancel', 'keydown',
    'keyup', 'keypress', 'beforeinput', 'input', 'change', 'focusin', 'focusout', 'select', 'selectstart', 'submit',
    'reset', 'wheel', 'mousewheel', 'copy', 'cut', 'paste', 'compositionstart', 'compositionupdate', 'compositionend',
    'dragstart', 'drag', 'dragend', 'dragenter', 'dragleave', 'dragover', 'drop', 'fullscreenchange',
    'fullscreenerror']);
export const forwardToo = type => bubbling.add(type);
// The ones that don't bubble, which are heard on their way down: a control's, a dialog's, and an audio or video
// element's.
const captured = ['focus', 'blur', 'scroll', 'mouseenter', 'mouseleave', 'pointerenter', 'pointerleave', 'toggle',
    'beforetoggle', 'invalid', 'load', 'error', 'cancel', 'close', 'abort', 'canplay', 'canplaythrough', 'cuechange',
    'durationchange', 'emptied', 'ended', 'loadeddata', 'loadedmetadata', 'loadend', 'loadstart', 'pause', 'play',
    'playing', 'progress', 'ratechange', 'seeked', 'seeking', 'stalled', 'suspend', 'timeupdate', 'volumechange',
    'waiting'];

// Blazor listens on the page's document only, so each event in the other window is sent on to that document, with
// the element it happened on and the path it took.
const forward = event => {
    const Type = window[event.constructor.name] ?? Event;
    const copy = new Type(event.type, event);
    const path = event.composedPath();
    Object.defineProperty(copy, 'target', { value: event.target });
    // A copy each time: Blazor takes the path apart as it reads it, and reads it once for each name an event has.
    Object.defineProperty(copy, 'composedPath', { value: () => [...path] });
    document.dispatchEvent(copy);
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

// Moves the element into the other window, which the caller opened, names the window and gives its body a class.
// The returned function stops following the page's styles; the caller moves the element back.
export function showInWindow(other, element, title, className) {
    const page = other.document;
    // A window opens on a blank page in quirks mode, which lays out and measures some things differently from the
    // page that made it, so the blank page is written again with a doctype.
    page.open();
    page.write('<!DOCTYPE html>');
    page.close();
    page.title = title;
    const stop = mirror(other);
    page.body.className = className;
    scope(element, page.body);
    page.body.appendChild(element);
    for (const type of bubbling) {
        page.addEventListener(type, forward);
    }

    for (const type of captured) {
        page.addEventListener(type, forward, true);
    }

    return stop;
}
