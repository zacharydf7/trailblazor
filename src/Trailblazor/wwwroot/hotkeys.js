import { typedInSelector } from '../Trailblazor.Primitives/pointer-origin.js';
import { matchesShortcut, parseShortcut } from './shortcuts.js';

// Every hotkey on the page, heard through one listener so that only one of those with the same keys fires.
const hotkeys = new Set();
let listening = false;
// The hotkey each marker's data-ready belongs to. A replaced hotkey can be disposed after its replacement attached, and
// must leave the replacement's mark alone.
const owners = new WeakMap();

// A field that takes typing keeps its keys, so a character key hotkey never fires in one.
const typing = `${typedInSelector}, select`;

const depth = element => {
    let count = 0;
    for (let node = element; node; node = node.parentElement) {
        count++;
    }

    return count;
};

// While a modal dialog is open, only hotkeys inside it fire. A Parent hotkey fires only while focus is inside the
// element it sits in. A marker the page has already removed never fires; it only waits for its script to be disposed.
const applies = (hotkey, focus, modal) => hotkey.marker.isConnected && !hotkey.marker.hasAttribute('data-disabled')
    && !(hotkey.characterKey && focus?.closest(typing))
    && (!modal || modal.contains(hotkey.marker))
    && (hotkey.scope === 'Page' || !!focus && hotkey.marker.parentElement.contains(focus));

const onKeyDown = event => {
    if (event.defaultPrevented) {
        return;
    }

    const focus = event.target instanceof Element ? event.target : null;
    const modal = [...document.querySelectorAll('dialog:modal')].at(-1);
    const found = [...hotkeys]
        .filter(hotkey => matchesShortcut(event, hotkey.shortcut) && applies(hotkey, focus, modal));
    if (found.length === 0) {
        return;
    }

    // A Parent hotkey, whose element holds focus, wins over the page's; among the same kind, the deepest wins.
    const rank = hotkey => [hotkey.scope === 'Parent' ? 1 : 0, depth(hotkey.marker)];
    const innermost = found.reduce((best, hotkey) => {
        const [scope, deep] = rank(hotkey);
        const [bestScope, bestDeep] = rank(best);
        return scope > bestScope || (scope === bestScope && deep > bestDeep) ? hotkey : best;
    });
    event.preventDefault();
    innermost.marker.dispatchEvent(new CustomEvent('tb-hotkey', { bubbles: true }));
};

// Listens for the hotkey's keys, and tells the marker when they're pressed where the hotkey applies.
export function attachHotkey(marker, keys, scope) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!marker) {
        return null;
    }

    const shortcut = parseShortcut(keys);
    const characterKey = !shortcut.ctrlKey && !shortcut.metaKey && !shortcut.altKey && shortcut.key.length === 1;
    const hotkey = { marker, shortcut, scope, characterKey };
    hotkeys.add(hotkey);
    if (!listening) {
        document.addEventListener('keydown', onKeyDown);
        listening = true;
    }

    // Marks which keys work, so a test waits for the ones the app set last.
    owners.set(marker, hotkey);
    marker.dataset.ready = keys;
    return {
        dispose() {
            hotkeys.delete(hotkey);
            if (owners.get(marker) === hotkey) {
                owners.delete(marker);
                delete marker.dataset.ready;
            }
        },
    };
}
