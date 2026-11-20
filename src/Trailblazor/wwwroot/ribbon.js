import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { focusWentElsewhere } from '../Trailblazor.Primitives/focus-scope.js';
import { createRovingFocus } from '../Trailblazor.Primitives/roving-focus.js';
import { controls } from './toolbar.js';

// A ribbon tab's row of commands is one tab stop whose arrow keys move between its controls, across its groups. When
// the row is too narrow, its groups collapse one at a time into a button that opens the group's controls as a popup:
// those with a collapse order first, lowest first, and then the others from the last. The controls stay where they
// are in the page, so they keep their state.
export function attachRibbon(toolbar) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!toolbar) {
        return null;
    }

    const groups = () => [...toolbar.querySelectorAll(':scope > [data-part="group"]')];
    const buttonOf = group => group.querySelector(':scope > [data-part="group-button"]');
    const itemsOf = group => group.querySelector(':scope > [data-part="items"]');
    const groupPopup = element => element.dataset?.part === 'items' && element.parentElement?.parentElement === toolbar;
    // A control inside a menu's popup belongs to the menu, which has keys of its own. A collapsed group's popup holds
    // the row's own controls, which the arrow keys reach while it is open.
    const inPopup = control => {
        const popup = control.closest('[popover]');
        return popup !== null && toolbar.contains(popup) && !groupPopup(popup);
    };
    const focus = createRovingFocus(toolbar, {
        items: controls,
        filter: control => !inPopup(control) && control.checkVisibility(),
    });

    const positions = new Map();
    const returnFocus = new Set();
    const openedFrom = new Map();
    const place = popup => {
        if (popup.matches(':popover-open') && !positions.has(popup)) {
            positions.set(popup, createAnchoredPosition(popup, buttonOf(popup.parentElement),
                { side: 'Bottom', align: 'Start', offset: 4 }));
        }
    };
    // The placement writes its position onto the popup, which mustn't keep it once it sits in the row again.
    const unplace = popup => {
        positions.get(popup)?.dispose();
        positions.delete(popup);
        popup.removeAttribute('style');
        popup.removeAttribute('data-side');
    };

    const collapseOrder = () => {
        const all = groups();
        const ordered = all.filter(group => 'collapseOrder' in group.dataset)
            .sort((one, other) => one.dataset.collapseOrder - other.dataset.collapseOrder);
        return [...ordered, ...all.filter(group => !('collapseOrder' in group.dataset)).reverse()];
    };
    // The button and the mark change the row's layout; the popover attribute goes on once the fit has settled.
    const mark = (group, collapsed) => {
        group.toggleAttribute('data-collapsed', collapsed);
        buttonOf(group).hidden = !collapsed;
    };
    // Puts a group's items back in the row, which closes a popup that is open. Firefox sends no toggle for a popup
    // closed by losing its attribute, so what a close would clear is cleared here.
    const restore = group => {
        const popup = itemsOf(group);
        unplace(popup);
        returnFocus.delete(popup);
        openedFrom.delete(popup);
        buttonOf(group).setAttribute('aria-expanded', 'false');
        popup.removeAttribute('popover');
        mark(group, false);
    };
    const overflows = () => toolbar.scrollWidth > toolbar.clientWidth;
    // The row's width and each group's as the last fit left them. A resize report with these same sizes comes from
    // that fit itself, and fitting again would close a popup opened since.
    const sizes = () => [toolbar.clientWidth, ...groups().map(group => group.offsetWidth)].join();
    let fitted = '';
    const fit = () => {
        delete toolbar.dataset.fitting;
        // A row that isn't shown, as when another tab is, has no width to fit, and keeps its last fit.
        if (!toolbar.checkVisibility()) {
            return;
        }

        const focused = document.activeElement;
        const all = groups();
        // A popup takes no room in the row, open or closed, so every group is measured as part of the row again.
        all.forEach(restore);

        for (const group of collapseOrder()) {
            if (!overflows()) {
                break;
            }

            const before = toolbar.scrollWidth;
            mark(group, true);
            // A group no wider than its button stays as it is.
            if (toolbar.scrollWidth >= before) {
                mark(group, false);
            }
        }

        for (const group of all.filter(group => group.hasAttribute('data-collapsed'))) {
            itemsOf(group).popover = 'auto';
        }

        focus.sync();
        // Focus can't stay on a control that went into a closed popup, so it goes to the group's button, and from a
        // button whose group came back it goes to the group's first control. A button that was hidden only while the
        // row was measured may have lost focus, and takes it back.
        if (toolbar.contains(focused)) {
            const group = focused.closest('[data-part="group"]');
            const target = focused.checkVisibility() ? focused
                : group?.hasAttribute('data-collapsed') ? buttonOf(group)
                : focus.focusable().find(control => group?.contains(control));
            if (target && target !== document.activeElement) {
                target.focus();
            }
        }

        fitted = sizes();
    };

    const onBeforeToggle = event => {
        const popup = event.target;
        if (!groupPopup(popup)) {
            return;
        }

        if (event.newState === 'closed') {
            if (popup.contains(document.activeElement)) {
                returnFocus.add(popup);
            } else {
                returnFocus.delete(popup);
            }

            return;
        }

        openedFrom.set(popup, document.activeElement);
        // toggle fires a task later, possibly after a paint, so the popup is placed in the next frame, before it paints.
        requestAnimationFrame(() => place(popup));
    };
    const onToggle = event => {
        const popup = event.target;
        if (!groupPopup(popup)) {
            return;
        }

        const open = event.newState === 'open';
        const button = buttonOf(popup.parentElement);
        button.setAttribute('aria-expanded', String(open));
        focus.sync();
        if (open) {
            place(popup);
            focus.focusable().find(control => popup.contains(control))?.focus();
            return;
        }

        unplace(popup);
        const elsewhere = focusWentElsewhere(popup, openedFrom.get(popup));
        openedFrom.delete(popup);
        if (returnFocus.delete(popup) && !elsewhere) {
            button.focus();
        }
    };
    // A popup closes as focus leaves it for anything but its own button, which closes it itself when pressed.
    const onFocusOut = event => {
        const popup = event.target.closest?.('[data-part="items"]:popover-open');
        const next = event.relatedTarget;
        if (popup && groupPopup(popup) && next && !popup.contains(next) && next !== buttonOf(popup.parentElement)) {
            popup.hidePopover();
        }
    };

    let frame = 0;
    // Marks a fit that is still to come, which a test waits out before using a control that may move.
    const fitSoon = () => {
        toolbar.dataset.fitting = '';
        cancelAnimationFrame(frame);
        frame = requestAnimationFrame(fit);
    };
    // Groups change size as fonts load or their content changes, so each one is watched along with the row.
    const resizes = new ResizeObserver(() => {
        if (sizes() !== fitted) {
            fitSoon();
        }
    });
    const watch = () => {
        resizes.disconnect();
        resizes.observe(toolbar);
        for (const group of groups()) {
            resizes.observe(group);
        }
    };
    const changes = new MutationObserver(() => {
        watch();
        fitSoon();
    });
    changes.observe(toolbar, { childList: true });
    // toggle and beforetoggle don't bubble, so they are caught on their way down.
    toolbar.addEventListener('beforetoggle', onBeforeToggle, true);
    toolbar.addEventListener('toggle', onToggle, true);
    toolbar.addEventListener('focusout', onFocusOut);
    watch();
    fit();
    // Marks that the keys work and the first fit is done, which tests wait for.
    toolbar.dataset.ready = '';

    return {
        // Collapsed groups come back, so a script attached in its place starts from the row as rendered.
        dispose() {
            cancelAnimationFrame(frame);
            resizes.disconnect();
            changes.disconnect();
            focus.dispose();
            toolbar.removeEventListener('beforetoggle', onBeforeToggle, true);
            toolbar.removeEventListener('toggle', onToggle, true);
            toolbar.removeEventListener('focusout', onFocusOut);
            groups().forEach(restore);

            delete toolbar.dataset.ready;
            delete toolbar.dataset.fitting;
        },
    };
}
