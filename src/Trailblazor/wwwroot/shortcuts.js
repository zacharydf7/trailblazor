import { isComposing } from '../Trailblazor.Primitives/composing.js';

// Chromium on macOS reports "macOS", and other browsers "MacIntel", "iPhone" or "iPad".
export const isApple = /mac|iphone|ipad/i.test(navigator.userAgentData?.platform ?? navigator.platform);

// Keys a shortcut names by a word, as their own character would be a separator or blank.
const named = { space: ' ', plus: '+' };

// Reads a shortcut such as "Mod+K", "Ctrl+Shift+P", "Mod+Space" or "?", where Mod is ⌘ on Apple devices and Ctrl
// elsewhere.
export function parseShortcut(text) {
    const parts = text.split('+').map(part => part.trim().toLowerCase());
    const name = parts.pop();
    const key = named[name] ?? name;
    const mod = parts.includes('mod');
    return {
        key,
        ctrlKey: parts.includes('ctrl') || (mod && !isApple),
        metaKey: parts.includes('meta') || (mod && isApple),
        altKey: parts.includes('alt'),
        shiftKey: parts.includes('shift'),
        // A symbol such as "?" takes Shift on some keyboard layouts and not on others, so Shift isn't matched for it.
        symbol: key.length === 1 && !/[a-z0-9 ]/.test(key),
    };
}

// With Alt held, a Mac types a symbol, so a letter's, digit's or Space's key is matched as well as its character.
export function matchesShortcut(event, shortcut) {
    if (event.repeat || isComposing(event)) {
        return false;
    }

    for (const modifier of ['ctrlKey', 'metaKey', 'altKey', 'shiftKey']) {
        if (event[modifier] !== shortcut[modifier] && !(modifier === 'shiftKey' && shortcut.symbol)) {
            return false;
        }
    }

    const code = event.code?.toLowerCase();
    return event.key?.toLowerCase() === shortcut.key || code === `key${shortcut.key}`
        || code === `digit${shortcut.key}` || (shortcut.key === ' ' && code === 'space');
}
