import { createRovingFocus } from '../Trailblazor.Primitives/roving-focus.js';

// A menubar's keys (WAI-ARIA menubar pattern): Tab reaches the bar at one item, Left and Right move along it, Home and
// End go to its ends, and Left and Right in an open menu close it and open the menu beside it. Each menu's own script
// opens it with Down, Enter or Space, and handles the keys inside it.
export function createMenubar(root) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    const focus = createRovingFocus(root, { items: ':scope > .tb-menu > [data-part="trigger"]' });
    const menuOf = item => item.ownerDocument.getElementById(item.getAttribute('aria-controls'));
    const isOpen = item => menuOf(item)?.matches(':popover-open') ?? false;
    const opens = item => !item.matches('[aria-disabled="true"]');
    // Focus moves first, so the closing menu doesn't send it back to its own trigger.
    const switchMenu = (from, to) => {
        to.focus();
        menuOf(from)?.hidePopover();
        menuOf(to)?.showPopover();
    };

    // Inside an open menu, its own script leaves these keys alone where they can't open or close a submenu.
    const onKeyDown = event => {
        const list = event.target.closest('[role="menu"]');
        if (!list || event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        // In right-to-left text the next menu is to the left.
        const rtl = getComputedStyle(root).direction === 'rtl';
        const step = { ArrowRight: rtl ? -1 : 1, ArrowLeft: rtl ? 1 : -1 }[event.key];
        const items = focus.focusable().filter(opens);
        const opener = items.find(item => menuOf(item)?.contains(list));
        if (!step || !opener) {
            return;
        }

        const target = items[(items.indexOf(opener) + step + items.length) % items.length];
        if (target !== opener) {
            event.preventDefault();
            switchMenu(opener, target);
        }
    };
    // With one menu open, pointing at another opens it instead, as desktop menubars do. A touch or pen fires this just
    // before its tap, whose click then opens the menu itself.
    const onPointerOver = event => {
        const items = focus.focusable();
        const item = items.find(candidate => candidate.contains(event.target));
        const open = items.find(candidate => candidate !== item && isOpen(candidate));
        if (event.pointerType === 'mouse' && item && open && opens(item) && !isOpen(item)) {
            switchMenu(open, item);
        }
    };

    root.addEventListener('keydown', onKeyDown);
    root.addEventListener('pointerover', onPointerOver);

    return {
        dispose() {
            focus.dispose();
            root.removeEventListener('keydown', onKeyDown);
            root.removeEventListener('pointerover', onPointerOver);
        },
    };
}
