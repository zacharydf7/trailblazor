import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';

const itemSelector = '[role="menuitem"], [role="menuitemcheckbox"], [role="menuitemradio"]';
// A pointer passing over an item on its way elsewhere doesn't open or close a submenu.
const hoverDelay = 150;
// How long a pointer heading for an open submenu across other items keeps it open.
const graceTime = 400;

// Keyboard and focus handling stay in the browser; only choosing an item, and a labeled menu's opens, reach the server.
export function createMenu(root, menu, options, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root || !menu) {
        return null;
    }

    const trigger = root.querySelector('[aria-haspopup="menu"]');
    trigger.setAttribute('aria-expanded', String(menu.matches(':popover-open')));
    // Set once the page is interactive, so tests know a click on an item reaches the server.
    trigger.dataset.ready = '';
    const submenuOptions = { side: 'End', align: 'Start', offset: 0 };
    const positions = new Map();
    const returnFocus = new Set();
    const openedByHover = new Set();
    let focusLastOnOpen = false;
    let hovered = null;
    let hoverTimer = 0;
    let lastPoint = null;
    let grace = null;

    // The element that opens a list: the trigger for the menu itself, or a submenu's item.
    const opener = list => list === menu ? trigger : menu.querySelector(`[aria-controls="${list.id}"]`);
    // A list's own items, not those of the submenus inside it.
    const items = list => [...list.querySelectorAll(itemSelector)]
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
            // A hover still waiting would undo what Escape or a click just closed; this runs as the list closes.
            clearTimeout(hoverTimer);
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
        // The server hears the menu itself open and close, however it happened; its submenus aren't reported apart.
        if (list === menu) {
            events?.invokeMethodAsync('OnToggle', open);
        }

        if (open) {
            // A submenu the pointer opened leaves focus where it was, as the pointer may only be passing through.
            if (!openedByHover.delete(list)) {
                focusItem(list, list === menu && focusLastOnOpen ? -1 : 0);
            }

            focusLastOnOpen = false;
            return;
        }

        positions.get(list)?.dispose();
        positions.delete(list);
        if (list === menu) {
            forgetPointer();
        }

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
        // A key acts on what has focus now, so a pointer timer still running mustn't undo it.
        clearTimeout(hoverTimer);
        clearTimeout(grace?.timer);
        grace = null;
        const list = event.target.closest('[role="menu"]');
        // Either timer may have been about to close a submenu open from another item, so close it now. Escape closes
        // the topmost list by itself, and closing one here would make it close two.
        if (event.key !== 'Escape') {
            closeOtherSubmenus(list, document.activeElement);
        }

        const all = items(list);
        const current = all.indexOf(document.activeElement);
        // Submenus open towards the end of the line, which is the left in right-to-left text.
        const inward = getComputedStyle(list).direction === 'rtl' ? 'ArrowLeft' : 'ArrowRight';
        const outward = inward === 'ArrowRight' ? 'ArrowLeft' : 'ArrowRight';
        const moveTo = index => {
            focusItem(list, index);
            closeOtherSubmenus(list, document.activeElement);
        };
        switch (event.key) {
            case 'ArrowDown':
                moveTo(current + 1);
                break;
            case 'ArrowUp':
                moveTo(current - 1);
                break;
            case 'Home':
                moveTo(0);
                break;
            case 'End':
                moveTo(-1);
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
            case ' ':
                // A link only follows Enter by itself; the menu pattern lets Space choose any item.
                if (all[current]?.localName !== 'a') {
                    return;
                }

                all[current].click();
                break;
            case 'Tab':
                // Tab moves on to the next control first; closing afterwards leaves focus where Tab put it.
                setTimeout(() => menu.matches(':popover-open') && menu.hidePopover());
                return;
            default:
                if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
                    focusByFirstLetter(all, current, event.key);
                    closeOtherSubmenus(list, document.activeElement);
                }

                return;
        }

        event.preventDefault();
    };
    const onMenuClick = event => {
        const item = event.target.closest(itemSelector);
        const submenu = submenuOf(item);
        // A click, Enter or Space on an item whose submenu is already open goes into it, rather than toggling it shut.
        if (submenu?.matches(':popover-open')) {
            event.preventDefault();
            focusItem(submenu, 0);
            return;
        }

        // An item that opens a submenu toggles it through popovertarget instead; a KeepOpen item leaves it open.
        if (item && item.getAttribute('aria-disabled') !== 'true' && !submenu && !item.hasAttribute('data-keep-open')) {
            menu.hidePopover();
        }
    };

    // A pointer that leaves the menu, or a menu that closes, drops what the pointer was about to open or close.
    const forgetPointer = () => {
        clearTimeout(hoverTimer);
        clearTimeout(grace?.timer);
        hovered = lastPoint = grace = null;
    };
    // A submenu left open beside the active item would keep its own item highlighted and take the next Escape.
    const closeOtherSubmenus = (list, item) => items(list).map(submenuOf)
        .filter(other => other && other !== submenuOf(item) && other.matches(':popover-open'))
        .forEach(other => other.hidePopover());
    // Opens the hovered item's submenu, and closes the other submenus open from the same list.
    const hoverOver = target => {
        const list = target?.closest('[role="menu"]');
        if (!list || !menu.contains(list) || !menu.matches(':popover-open')) {
            return;
        }

        const hoveredItem = target.closest(itemSelector);
        closeOtherSubmenus(list, hoveredItem);
        const submenu = submenuOf(hoveredItem);
        if (submenu && !submenu.matches(':popover-open')) {
            openedByHover.add(submenu);
            submenu.showPopover();
        }
    };
    const hoverOverSoon = target => {
        clearTimeout(hoverTimer);
        hoverTimer = setTimeout(() => hoverOver(target), hoverDelay);
    };
    // The pointer and the keys share one active item, so a key pressed next acts on the item under the pointer.
    const hoverItem = item => {
        hovered = item;
        item?.focus({ preventScroll: true });
    };
    const onPointerMove = event => {
        if (event.pointerType !== 'mouse') {
            return;
        }

        const point = { x: event.clientX, y: event.clientY };
        const from = lastPoint;
        lastPoint = point;
        const item = event.target.closest(itemSelector);
        if (item === hovered) {
            // Moving the pointer takes focus back from a key, unless the key went into this item's submenu.
            if (item && document.activeElement !== item && !submenuOf(item)?.contains(document.activeElement)) {
                item.focus({ preventScroll: true });
                hoverOverSoon(item);
            }

            return;
        }

        // Leaving an item with its submenu open, the pointer may cross other items on the way to the submenu.
        const submenu = submenuOf(hovered);
        if (!grace && from && submenu?.matches(':popover-open') && !submenu.contains(event.target)) {
            grace = { submenu, points: [from, ...nearCorners(submenu, from)], timer: 0 };
            grace.timer = setTimeout(() => {
                grace = null;
                const target = document.elementFromPoint(lastPoint.x, lastPoint.y);
                hoverItem(target?.closest(itemSelector));
                hoverOver(target);
            }, graceTime);
        }

        if (grace) {
            if (!grace.submenu.contains(event.target) && inTriangle(point, grace.points)) {
                clearTimeout(hoverTimer);
                return;
            }

            clearTimeout(grace.timer);
            grace = null;
        }

        hoverItem(item);
        hoverOverSoon(event.target);
    };

    // Toggle events don't bubble, so the submenus' toggles are caught in the capture phase.
    menu.addEventListener('beforetoggle', onBeforeToggle, true);
    menu.addEventListener('toggle', onToggle, true);
    menu.addEventListener('keydown', onMenuKeyDown);
    menu.addEventListener('click', onMenuClick);
    menu.addEventListener('pointermove', onPointerMove);
    menu.addEventListener('pointerleave', forgetPointer);
    trigger.addEventListener('keydown', onTriggerKeyDown);

    // Replay toggles missed by any list clicked open before this script attached, outermost first so focus ends deepest.
    for (const list of [menu, ...menu.querySelectorAll('[role="menu"]')]) {
        if (list.matches(':popover-open')) {
            onBeforeToggle({ target: list, newState: 'open' });
            onToggle({ target: list, newState: 'open' });
        }
    }

    return {
        setOpen(open) {
            if (open !== menu.matches(':popover-open')) {
                menu.togglePopover(open);
            }
        },
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
            menu.removeEventListener('pointermove', onPointerMove);
            menu.removeEventListener('pointerleave', forgetPointer);
            trigger.removeEventListener('keydown', onTriggerKeyDown);
            forgetPointer();
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

// The corners of the submenu's edge nearest the point; with the point they bound the way to the submenu.
function nearCorners(submenu, point) {
    const rect = submenu.getBoundingClientRect();
    const x = Math.abs(point.x - rect.left) < Math.abs(point.x - rect.right) ? rect.left : rect.right;
    return [{ x, y: rect.top }, { x, y: rect.bottom }];
}

function inTriangle(point, [a, b, c]) {
    const side = (p, q, r) => (p.x - r.x) * (q.y - r.y) - (q.x - r.x) * (p.y - r.y);
    const signs = [side(point, a, b), side(point, b, c), side(point, c, a)];
    return !(signs.some(sign => sign < 0) && signs.some(sign => sign > 0));
}
