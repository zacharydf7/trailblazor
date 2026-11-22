// The small toolbar that floats over a selection in a rich text editor. Blazor draws it as a popover that stays closed,
// with its buttons out of the tab order; this shows it over the selection while text is selected and the editor has
// the focus, and hides it again. scope is the editor's outer element, which the toolbar is in.
// The host gives selectionBox(), the selected text's box in the window, or null when the toolbar shouldn't show;
// run(button), which does what one of its buttons stands for; and focus(), which gives the text the focus back.
export function createFloatingToolbar(scope, host, signal) {
    const view = scope.ownerDocument.defaultView;
    const bar = () => scope.querySelector(':scope > [data-part="floating-toolbar"]');
    const isOpen = () => !!bar()?.matches(':popover-open');
    const gap = 8;
    // Escape hides the toolbar until the selection changes.
    let dismissed = false;
    // While a pointer is down, the selection is still being made.
    let pressing = false;
    // A selection made by touch gets the system's own menu, which sits where this toolbar would.
    let byTouch = false;
    let queued = false;

    const place = () => {
        queued = false;
        const element = bar();
        if (!element) return;
        const box = dismissed || pressing || byTouch ? null : host.selectionBox();
        if (!box) {
            if (isOpen()) element.hidePopover();
            return;
        }
        if (!isOpen()) element.showPopover();
        // Above the selection, or below it when there is no room above, and never past the window's sides.
        const above = box.top - element.offsetHeight - gap;
        element.style.top = `${above >= gap ? above : box.bottom + gap}px`;
        const centered = box.left + box.width / 2 - element.offsetWidth / 2;
        // The page's width leaves out its scrollbar, which the window's includes.
        const room = scope.ownerDocument.documentElement.clientWidth - element.offsetWidth - gap;
        element.style.left = `${Math.max(gap, Math.min(centered, room))}px`;
    };

    // Several things can change in one turn, so the toolbar is placed once, after them.
    const update = () => {
        if (queued) return;
        queued = true;
        view.requestAnimationFrame(place);
    };

    const onKeyDown = event => {
        byTouch = false;
        const element = bar();
        if (!element?.contains(event.target)) return;
        const buttons = [...element.querySelectorAll('button:not(:disabled)')];
        const at = buttons.indexOf(event.target.closest('button'));
        const forward = view.getComputedStyle(element).direction === 'rtl' ? 'ArrowLeft' : 'ArrowRight';
        const back = forward === 'ArrowRight' ? 'ArrowLeft' : 'ArrowRight';
        const to = { [forward]: at + 1, [back]: at - 1, Home: 0, End: buttons.length - 1 }[event.key];
        if (to !== undefined) {
            event.preventDefault();
            buttons[(to + buttons.length) % buttons.length].focus();
        } else if (event.key === 'Escape') {
            // The text gets the focus back, and nothing around the editor, such as a dialog, hears the key.
            event.preventDefault();
            event.stopPropagation();
            host.focus();
        }
    };

    const on = (target, type, handler, capture = false) => target.addEventListener(type, handler, { signal, capture });
    // Only a press in the text makes a selection; one on a button of the editor's doesn't.
    on(scope, 'pointerdown', event => {
        if (!event.target.closest('[data-part="input"]')) return;
        pressing = true;
        byTouch = event.pointerType === 'touch';
        update();
    });
    for (const type of ['pointerup', 'pointercancel']) {
        on(view, type, () => {
            pressing = false;
            update();
        });
    }
    // Pressing one of its buttons must not take the selection out of the editor.
    on(scope, 'mousedown', event => {
        if (bar()?.contains(event.target)) event.preventDefault();
    });
    on(scope, 'click', event => {
        const button = event.target.closest('button');
        if (button && !button.disabled && bar()?.contains(button)) host.run(button);
    });
    on(scope, 'keydown', onKeyDown);
    on(scope, 'focusin', update);
    on(scope, 'focusout', update);
    on(view, 'scroll', update, true);
    on(view, 'resize', update);

    return {
        update,
        // The selection changed, so a toolbar that Escape hid may show again.
        selectionChanged: () => {
            dismissed = false;
            update();
        },
        // Escape in the text hides the toolbar. False when it wasn't showing, so the key goes on to mean what it did.
        dismiss: () => {
            if (!isOpen()) return false;
            dismissed = true;
            place();
            return true;
        },
        // Alt+F10 in the text moves the focus to the toolbar's first button. False when it isn't showing.
        focus: () => {
            const first = isOpen() ? bar().querySelector('button:not(:disabled)') : null;
            first?.focus();
            return !!first;
        },
    };
}
