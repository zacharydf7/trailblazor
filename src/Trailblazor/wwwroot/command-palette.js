import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';

// Chromium on macOS reports "macOS", and other browsers "MacIntel", "iPhone" or "iPad".
const isApple = /mac|iphone|ipad/i.test(navigator.userAgentData?.platform ?? navigator.platform);

// The listener each marker's data-ready belongs to. A replaced listener can be disposed after its replacement attached,
// and must leave the replacement's mark alone.
const listeners = new WeakMap();

// Focus stays in the search box while the arrow keys move an active option, which Enter clicks, as in a combobox. The
// first option turns active whenever the results change, so Enter right after typing runs the best match.
export function attachPalette(scope) {
    // Blazor passes null for an element that was removed before this call arrived.
    const input = scope?.querySelector('[role="combobox"]');
    const listbox = scope?.querySelector('[role="listbox"]');
    if (!input || !listbox) {
        return null;
    }

    const choosable = '[role="option"]:not([aria-disabled="true"])';
    const options = () => [...listbox.querySelectorAll(choosable)];
    let active = null;

    // Blazor reuses option elements as the results change and doesn't know about these attributes, so the option they
    // were set on is always cleared first.
    const setActive = option => {
        active?.removeAttribute('data-active');
        active?.setAttribute('aria-selected', 'false');
        active = option ?? null;
        if (active) {
            active.setAttribute('data-active', '');
            active.setAttribute('aria-selected', 'true');
            input.setAttribute('aria-activedescendant', active.id);
            active.scrollIntoView({ block: 'nearest' });
        } else {
            input.removeAttribute('aria-activedescendant');
        }
    };
    const move = step => {
        const list = options();
        if (list.length > 0) {
            const index = list.indexOf(active);
            setActive(index < 0 ? list[0] : list[(index + step + list.length) % list.length]);
        }
    };

    const onKeyDown = event => {
        if (isComposing(event)) {
            return;
        }

        switch (event.key) {
            case 'ArrowDown':
                move(1);
                break;
            case 'ArrowUp':
                move(-1);
                break;
            case 'Enter':
                if (!active) {
                    return;
                }

                active.click();
                break;
            default:
                return;
        }

        event.preventDefault();
    };
    // Pressing an option would take focus from the search box.
    const onPointerDown = event => event.preventDefault();
    const onPointerMove = event => {
        const option = event.target.closest(choosable);
        if (option && option !== active) {
            setActive(option);
        }
    };
    const observer = new MutationObserver(() => setActive(options()[0]));

    observer.observe(listbox, { childList: true, subtree: true, characterData: true });
    input.addEventListener('keydown', onKeyDown);
    listbox.addEventListener('pointerdown', onPointerDown);
    listbox.addEventListener('pointermove', onPointerMove);
    setActive(options()[0]);
    // Marks that the keys work, which tests wait for.
    input.dataset.ready = '';

    return {
        dispose() {
            observer.disconnect();
            input.removeEventListener('keydown', onKeyDown);
            listbox.removeEventListener('pointerdown', onPointerDown);
            listbox.removeEventListener('pointermove', onPointerMove);
        },
    };
}

// Opens the palette from anywhere on the page, with a shortcut such as "Mod+K", where Mod is ⌘ on Apple devices and
// Ctrl elsewhere. With Alt held, a Mac types a symbol, so the letter's key is matched as well as its character.
export function attachShortcut(marker, shortcut, events) {
    if (!marker) {
        return null;
    }

    const parts = shortcut.split('+').map(part => part.trim().toLowerCase());
    const key = parts.pop();
    const mod = parts.includes('mod');
    const wanted = {
        ctrlKey: parts.includes('ctrl') || (mod && !isApple),
        metaKey: parts.includes('meta') || (mod && isApple),
        altKey: parts.includes('alt'),
        shiftKey: parts.includes('shift'),
    };
    const onKeyDown = event => {
        if (event.defaultPrevented || event.repeat || isComposing(event)
            || Object.entries(wanted).some(([modifier, held]) => event[modifier] !== held)
            || (event.key?.toLowerCase() !== key && event.code?.toLowerCase() !== `key${key}`)) {
            return;
        }

        event.preventDefault();
        callDotNet(events, 'OnShortcut');
    };

    document.addEventListener('keydown', onKeyDown);
    // Marks which shortcut works, so a test waits for the one the app set last.
    listeners.set(marker, onKeyDown);
    marker.dataset.ready = shortcut;

    return {
        dispose() {
            document.removeEventListener('keydown', onKeyDown);
            if (listeners.get(marker) === onKeyDown) {
                listeners.delete(marker);
                delete marker.dataset.ready;
            }
        },
    };
}
