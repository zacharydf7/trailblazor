import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';

// Keyboard and focus handling stay in the browser; only choosing an item reaches the server.
export function createMenu(root, menu, options) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root || !menu) {
        return null;
    }

    const trigger = root.querySelector('[aria-haspopup="menu"]');
    trigger.setAttribute('aria-expanded', String(menu.matches(':popover-open')));
    const submenuOptions = { side: 'End', align: 'Start', offset: 0 };
    const positions = new Map();
    const returnFocus = new Set();
    let focusLastOnOpen = false;

    // The element that opens a list: the trigger for the menu itself, or a submenu's item.
    const opener = list => list === menu ? trigger : menu.querySelector(`[aria-controls="${list.id}"]`);
    // A list's own items, not those of the submenus inside it.
    const items = list => [...list.querySelectorAll('[role="menuitem"]')]
        .filter(item => item.closest('[role="menu"]') === list);
    const focusItem = (list, index) => {
        const all = items(list);
        all[(index + all.length) % all.length]?.focus();
    };
    const submenuOf = item => item?.getAttribute('aria-haspopup') === 'menu'
        ? document.getElementById(item.getAttribute('aria-controls'))
        : null;

    const onBeforeToggle = event => {
        const list = event.target;
        if (!list.matches('[role="menu"]')) {
            return;
        }

        // Escape or choosing an item closes from inside the list, and focus then goes back to what opened it.
        if (event.newState === 'closed') {
            if (list.contains(document.activeElement)) {
                returnFocus.add(list);
            } else {
                returnFocus.delete(list);
            }

            return;
        }

        // toggle fires a task later, possibly after a paint, so the list is placed in the next frame, before it paints.
        requestAnimationFrame(() => {
            if (list.matches(':popover-open') && !positions.has(list)) {
                const placement = list === menu ? options : submenuOptions;
                positions.set(list, createAnchoredPosition(list, opener(list), placement));
            }
        });
    };
    const onToggle = event => {
        const list = event.target;
        if (!list.matches('[role="menu"]')) {
            return;
        }

        const open = event.newState === 'open';
        opener(list).setAttribute('aria-expanded', String(open));
        if (open) {
            focusItem(list, list === menu && focusLastOnOpen ? -1 : 0);
            focusLastOnOpen = false;
            return;
        }

        positions.get(list)?.dispose();
        positions.delete(list);
        if (returnFocus.delete(list)) {
            opener(list).focus();
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
        const list = event.target.closest('[role="menu"]');
        const all = items(list);
        const current = all.indexOf(document.activeElement);
        // Submenus open towards the end of the line, which is the left in right-to-left text.
        const inward = getComputedStyle(list).direction === 'rtl' ? 'ArrowLeft' : 'ArrowRight';
        const outward = inward === 'ArrowRight' ? 'ArrowLeft' : 'ArrowRight';
        switch (event.key) {
            case 'ArrowDown':
                focusItem(list, current + 1);
                break;
            case 'ArrowUp':
                focusItem(list, current - 1);
                break;
            case 'Home':
                focusItem(list, 0);
                break;
            case 'End':
                focusItem(list, -1);
                break;
            case inward: {
                const submenu = submenuOf(all[current]);
                if (!submenu) {
                    return;
                }

                if (submenu.matches(':popover-open')) {
                    focusItem(submenu, 0);
                } else {
                    submenu.showPopover();
                }

                break;
            }
            case outward:
                if (list === menu) {
                    return;
                }

                list.hidePopover();
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
        // An item that opens a submenu toggles it through popovertarget instead.
        if (item && item.getAttribute('aria-disabled') !== 'true' && !submenuOf(item)) {
            menu.hidePopover();
        }
    };

    // Toggle events don't bubble, so the submenus' toggles are caught in the capture phase.
    menu.addEventListener('beforetoggle', onBeforeToggle, true);
    menu.addEventListener('toggle', onToggle, true);
    menu.addEventListener('keydown', onMenuKeyDown);
    menu.addEventListener('click', onMenuClick);
    trigger.addEventListener('keydown', onTriggerKeyDown);

    return {
        dispose() {
            // Closing the menu closes any submenu open inside it.
            if (menu.matches(':popover-open')) {
                menu.hidePopover();
            }

            positions.forEach(position => position.dispose());
            menu.removeEventListener('beforetoggle', onBeforeToggle, true);
            menu.removeEventListener('toggle', onToggle, true);
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
