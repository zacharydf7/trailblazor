import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';

// Keyboard and focus handling stay in the browser; only choosing an item reaches the server.
export function createMenu(root, menu, options) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root || !menu) {
        return null;
    }

    const trigger = root.querySelector('[aria-haspopup="menu"]');
    trigger.setAttribute('aria-expanded', String(menu.matches(':popover-open')));
    let position = null;
    let focusLastOnOpen = false;
    let returnFocus = false;

    const items = () => [...menu.querySelectorAll('[role="menuitem"]')];
    const focusItem = index => {
        const all = items();
        all[(index + all.length) % all.length]?.focus();
    };

    const onBeforeToggle = event => {
        // Escape or choosing an item closes from inside the menu, and focus then goes back to the trigger.
        if (event.newState === 'closed') {
            returnFocus = menu.contains(document.activeElement);
            return;
        }

        // toggle fires a task later, possibly after a paint, so the menu is placed in the next frame, before it paints.
        requestAnimationFrame(() => {
            if (menu.matches(':popover-open') && !position) {
                position = createAnchoredPosition(menu, trigger, options);
            }
        });
    };
    const onToggle = event => {
        const open = event.newState === 'open';
        trigger.setAttribute('aria-expanded', String(open));
        if (open) {
            focusItem(focusLastOnOpen ? -1 : 0);
            focusLastOnOpen = false;
            return;
        }

        position?.dispose();
        position = null;
        if (returnFocus) {
            trigger.focus();
        }
    };
    const onTriggerKeyDown = event => {
        if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
            event.preventDefault();
            focusLastOnOpen = event.key === 'ArrowUp';
            menu.showPopover();
        }
    };
    const onMenuKeyDown = event => {
        const all = items();
        const current = all.indexOf(document.activeElement);
        switch (event.key) {
            case 'ArrowDown':
                focusItem(current + 1);
                break;
            case 'ArrowUp':
                focusItem(current - 1);
                break;
            case 'Home':
                focusItem(0);
                break;
            case 'End':
                focusItem(-1);
                break;
            case 'Tab':
                // Tab moves on to the next control first; closing afterwards leaves focus where Tab put it.
                setTimeout(() => menu.matches(':popover-open') && menu.hidePopover());
                return;
            default:
                if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
                    focusByFirstLetter(all, current, event.key);
                }

                return;
        }

        event.preventDefault();
    };
    const onMenuClick = event => {
        const item = event.target.closest('[role="menuitem"]');
        if (item && item.getAttribute('aria-disabled') !== 'true') {
            menu.hidePopover();
        }
    };

    menu.addEventListener('beforetoggle', onBeforeToggle);
    menu.addEventListener('toggle', onToggle);
    menu.addEventListener('keydown', onMenuKeyDown);
    menu.addEventListener('click', onMenuClick);
    trigger.addEventListener('keydown', onTriggerKeyDown);

    return {
        dispose() {
            if (menu.matches(':popover-open')) {
                menu.hidePopover();
            }

            position?.dispose();
            menu.removeEventListener('beforetoggle', onBeforeToggle);
            menu.removeEventListener('toggle', onToggle);
            menu.removeEventListener('keydown', onMenuKeyDown);
            menu.removeEventListener('click', onMenuClick);
            trigger.removeEventListener('keydown', onTriggerKeyDown);
        },
    };
}

// Moves to the next item, after the current one, whose label starts with the typed letter.
function focusByFirstLetter(items, current, letter) {
    const lower = letter.toLowerCase();
    for (let step = 1; step <= items.length; step++) {
        const item = items[(current + step) % items.length];
        if (item.textContent.trim().toLowerCase().startsWith(lower)) {
            item.focus();
            return;
        }
    }
}
