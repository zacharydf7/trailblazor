import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';

const itemSelector = '[role="menuitem"], [role="menuitemcheckbox"], [role="menuitemradio"]';
// A pointer passing over an item on its way elsewhere doesn't open or close a submenu.
const hoverDelay = 150;
// How long a pointer heading for an open submenu across other items keeps it open.
const graceTime = 400;

// How far a context menu stays from the window's edges.
const viewportPadding = 8;
// A touch that rests this long on a context menu's target opens the menu, unless it moves further than pressSlop.
const longPressDelay = 500;
const pressSlop = 10;

// Keyboard and focus handling stay in the browser; only choosing an item, and a labeled menu's opens, reach the server.
export function createMenu(root, menu, options, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root || !menu) {
        return null;
    }

    // By the menu's id, since a submenu's button inside it also has aria-haspopup.
    const trigger = root.querySelector(`[aria-controls="${menu.id}"]`);
    if (!trigger) {
        throw new Error('A custom TbMenu trigger needs context.Attributes on its button.');
    }

    trigger.setAttribute('aria-expanded', String(menu.matches(':popover-open')));
    // Set once the page is interactive, so tests know a click on an item reaches the server.
    trigger.dataset.ready = '';
    let focusLastOnOpen = false;
    const lists = attachLists(menu, events, {
        place: () => createAnchoredPosition(menu, trigger, options),
        toggled: open => trigger.setAttribute('aria-expanded', String(open)),
        firstItem: () => {
            const last = focusLastOnOpen;
            focusLastOnOpen = false;
            return last ? -1 : 0;
        },
        returnFocus: () => trigger.focus(),
    });
    // A trigger that's off, including one that stays focusable or is loading, opens nothing.
    const onTriggerKeyDown = event => {
        if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey
            || trigger.matches(':disabled, [aria-disabled="true"]')) {
            return;
        }

        if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
            event.preventDefault();
            focusLastOnOpen = event.key === 'ArrowUp';
            menu.showPopover();
        }
    };
    trigger.addEventListener('keydown', onTriggerKeyDown);
    lists.replay();

    return {
        setOpen: lists.setOpen,
        dispose() {
            // Cleared so tests can wait for a script attached in its place to mark it ready again.
            delete trigger.dataset.ready;
            lists.dispose();
            trigger.removeEventListener('keydown', onTriggerKeyDown);
        },
    };
}

// Opens the menu where the target is right-clicked or long-pressed, or at its focused element on Shift+F10 or the
// context menu key. An element in the target with a data-context-key opens the menu for that key: the server draws the
// key's items first and writes back the number of the request it drew them for, so no item meant for another key is
// ever on screen.
export function createContextMenu(target, menu, events) {
    if (!target || !menu) {
        return null;
    }

    let point = null;
    let pending = null;
    let requests = 0;
    let returnTo = null;
    let press = null;
    let keyOpened = false;
    const lists = attachLists(menu, events, {
        place: () => keepAtPoint(menu, point),
        toggled: () => {},
        firstItem: () => 0,
        // Focus goes back without scrolling, as the scroll that closed the menu may have moved that element out of view.
        returnFocus: () => returnTo?.focus({ preventScroll: true }),
    });

    const keyOf = element => {
        const keyed = element.closest('[data-context-key]');
        return keyed && target.contains(keyed) ? keyed.dataset.contextKey : '';
    };
    // Placed at once, as the list's own placement waits a frame, which would show it at the window's corner first.
    const show = at => {
        point = at;
        menu.showPopover();
        placeAtPoint(menu, at);
    };
    const open = (element, at) => {
        if (menu.matches(':popover-open')) {
            return;
        }

        returnTo = document.activeElement;
        const key = keyOf(element);
        // Until the server has answered the last request, that answer could still redraw the items, so a key already
        // drawn is asked for again.
        if (menu.dataset.request === String(requests) && (menu.dataset.key ?? '') === key) {
            show(at);
            return;
        }

        // The latest opening wins, so a second right-click before the server answers opens for the second key.
        pending = { request: ++requests, at };
        target.dispatchEvent(new CustomEvent('tb-context-menu-open',
            { bubbles: true, detail: { key, request: pending.request } }));
    };
    const requestObserver = new MutationObserver(() => {
        if (pending && menu.dataset.request === String(pending.request)) {
            const at = pending.at;
            pending = null;
            show(at);
        }
    });
    requestObserver.observe(menu, { attributes: true, attributeFilter: ['data-request'] });

    // A context menu inside the target handles its own area. This menu sits inside the target too, so its events
    // bubble here, and a right-click on it opens nothing else. A disabled one leaves every way in to the browser.
    const ownArea = event => !target.hasAttribute('data-disabled')
        && event.target.closest('.tb-context-menu') === target && !menu.contains(event.target);
    const onContextMenu = event => {
        if (menu.contains(event.target)) {
            event.preventDefault();
            return;
        }

        if (!ownArea(event)) {
            return;
        }

        event.preventDefault();
        // The context menu key and Shift+F10 also make some browsers fire contextmenu, as the key is pressed or let go.
        if (keyOpened) {
            return;
        }

        cancelPress();
        open(event.target, { x: event.clientX, y: event.clientY });
    };
    const onKeyDown = event => {
        if (!ownArea(event) || !(event.key === 'ContextMenu' || event.key === 'F10' && event.shiftKey)) {
            return;
        }

        event.preventDefault();
        keyOpened = true;
        const rect = event.target.getBoundingClientRect();
        const rtl = getComputedStyle(event.target).direction === 'rtl';
        open(event.target, { x: rtl ? rect.right : rect.left, y: rect.bottom });
    };
    // Cleared a task later, so a contextmenu event fired by the key's release still sees keyOpened.
    const onKeyUp = () => setTimeout(() => keyOpened = false);
    // A mouse has its right button; touch and pen open the menu by resting on the target.
    const onPointerDown = event => {
        if (event.pointerType === 'mouse' || !ownArea(event)) {
            return;
        }

        cancelPress();
        const at = { x: event.clientX, y: event.clientY };
        press = { at, timer: setTimeout(() => {
            press = null;
            open(event.target, at);
        }, longPressDelay) };
    };
    const onPointerMove = event => {
        if (press && Math.hypot(event.clientX - press.at.x, event.clientY - press.at.y) > pressSlop) {
            cancelPress();
        }
    };
    const cancelPress = () => {
        clearTimeout(press?.timer);
        press = null;
    };
    // The menu is a manual popover, as the browser would close an auto one as the press that opened it ends. So it
    // closes itself on a press outside it, and on Escape unless a submenu is open, which Escape closes first.
    // A press outside also drops an opening still waiting for the server, as the user has moved on. It doesn't send
    // focus back, since the press itself moves focus once the menu has closed.
    const onOutsidePress = event => {
        keyOpened = false;
        if (!menu.contains(event.target)) {
            pending = null;
            if (menu.matches(':popover-open')) {
                returnTo = null;
                menu.hidePopover();
            }
        }
    };
    // The menu stays where it opened, so a scroll that moves what it was opened on closes it.
    const onScroll = event => {
        if (!menu.contains(event.target)) {
            pending = null;
            if (menu.matches(':popover-open')) {
                menu.hidePopover();
            }
        }
    };
    const onMenuEscape = event => {
        if (event.key === 'Escape' && !menu.querySelector('[role="menu"]:popover-open')) {
            event.preventDefault();
            menu.hidePopover();
        }
    };

    target.addEventListener('contextmenu', onContextMenu);
    target.addEventListener('keydown', onKeyDown);
    target.addEventListener('keyup', onKeyUp);
    target.addEventListener('pointerdown', onPointerDown);
    target.addEventListener('pointermove', onPointerMove);
    target.addEventListener('pointerup', cancelPress);
    target.addEventListener('pointercancel', cancelPress);
    document.addEventListener('pointerdown', onOutsidePress, true);
    window.addEventListener('scroll', onScroll, { capture: true, passive: true });
    menu.addEventListener('keydown', onMenuEscape);
    // Set once the page is interactive, so tests know a right-click opens the menu.
    target.dataset.ready = '';

    return {
        dispose() {
            delete target.dataset.ready;
            cancelPress();
            requestObserver.disconnect();
            lists.dispose();
            target.removeEventListener('contextmenu', onContextMenu);
            target.removeEventListener('keydown', onKeyDown);
            target.removeEventListener('keyup', onKeyUp);
            target.removeEventListener('pointerdown', onPointerDown);
            target.removeEventListener('pointermove', onPointerMove);
            target.removeEventListener('pointerup', cancelPress);
            target.removeEventListener('pointercancel', cancelPress);
            document.removeEventListener('pointerdown', onOutsidePress, true);
            window.removeEventListener('scroll', onScroll, { capture: true });
            menu.removeEventListener('keydown', onMenuEscape);
        },
    };
}

// The keys, pointer handling, focus and placement of a menu and its submenus. The hooks handle what differs for the
// menu itself: where it's placed, what opened it, and where focus goes back to.
function attachLists(menu, events, hooks) {
    const submenuOptions = { side: 'End', align: 'Start', offset: 0 };
    const positions = new Map();
    const returnFocus = new Set();
    const openedByHover = new Set();
    const shown = new Set();
    // Whether the server last knew the menu as open, from its own setOpen or from a toggle reported to it.
    let reported = false;
    let hovered = null;
    let hoverTimer = 0;
    let lastPoint = null;
    let grace = null;

    // The item that opens a submenu.
    const opener = list => menu.querySelector(`[aria-controls="${list.id}"]`);
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
                positions.set(list, list === menu ? hooks.place()
                    : createAnchoredPosition(list, opener(list), submenuOptions));
            }
        });
    };
    const onToggle = event => {
        const list = event.target;
        if (!list.matches('[role="menu"]')) {
            return;
        }

        const open = event.newState === 'open';
        // The server hears the menu, not its submenus, whenever it differs from what the server last knew: if code
        // opens the menu and it closes before the toggle event runs, the browser fires one toggle that stays closed.
        if (list === menu && open !== reported) {
            reported = open;
            if (events) {
                callDotNet(events, 'OnToggle', open);
            }
        }

        // The replay at attach and a toggle still queued from before it can both report one opening.
        if (open === shown.has(list)) {
            return;
        }

        if (open) {
            shown.add(list);
        } else {
            shown.delete(list);
        }

        if (list === menu) {
            hooks.toggled(open);
        } else {
            opener(list).setAttribute('aria-expanded', String(open));
        }

        if (open) {
            // A submenu the pointer opened leaves focus where it was, as the pointer may only be passing through.
            if (!openedByHover.delete(list)) {
                focusItem(list, list === menu ? hooks.firstItem() : 0);
            }

            return;
        }

        positions.get(list)?.dispose();
        positions.delete(list);
        if (list === menu) {
            forgetPointer();
        }

        if (returnFocus.delete(list)) {
            if (list === menu) {
                hooks.returnFocus();
            } else {
                opener(list).focus();
            }
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

    return {
        // Replays toggles missed by any list clicked open before the script attached, outermost first so focus ends
        // deepest.
        replay() {
            for (const list of [menu, ...menu.querySelectorAll('[role="menu"]')]) {
                if (list.matches(':popover-open')) {
                    onBeforeToggle({ target: list, newState: 'open' });
                    onToggle({ target: list, newState: 'open' });
                }
            }
        },
        setOpen(open) {
            reported = open;
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
            forgetPointer();
        },
    };
}

// Places a context menu's corner at the point, or its other corner where the window has no room past the point.
function placeAtPoint(menu, point) {
    menu.style.position = 'fixed';
    menu.style.inset = 'auto';
    menu.style.margin = '0';
    const width = document.documentElement.clientWidth;
    const height = document.documentElement.clientHeight;
    // A menu taller than the window scrolls, as the CSS caps it at this.
    menu.style.setProperty('--tb-available-block-size', `${Math.max(height - 2 * viewportPadding, 0)}px`);
    const size = { width: menu.offsetWidth, height: menu.offsetHeight };
    // In right-to-left text the menu opens toward the left, as the end of the line is there.
    const rtl = getComputedStyle(menu).direction === 'rtl';
    const after = rtl ? point.x - size.width : point.x;
    const before = rtl ? point.x : point.x - size.width;
    const fitsAfter = after >= viewportPadding && after + size.width <= width - viewportPadding;
    const x = fitsAfter ? after : before;
    const y = point.y + size.height <= height - viewportPadding ? point.y : point.y - size.height;
    const clamp = (value, room) => Math.max(viewportPadding, Math.min(value, room - viewportPadding));
    menu.style.left = `${clamp(x, width - size.width)}px`;
    menu.style.top = `${clamp(y, height - size.height)}px`;
}

// Keeps a context menu placed at the point as its items render and the window resizes.
function keepAtPoint(menu, point) {
    const update = () => placeAtPoint(menu, point);
    update();
    const resizeObserver = new ResizeObserver(update);
    resizeObserver.observe(menu);
    window.addEventListener('resize', update, { passive: true });
    return {
        dispose() {
            resizeObserver.disconnect();
            window.removeEventListener('resize', update);
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
