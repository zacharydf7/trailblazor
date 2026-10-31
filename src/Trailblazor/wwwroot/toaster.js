// Toasts never take focus themselves, so F8 brings focus to the newest one and Escape sends it back.
export function attachToaster(toaster) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!toaster) {
        return null;
    }

    let returnFocus = null;
    let focused = null;

    // A toast fading out after it closed can't take focus.
    const newestButton = () => [...toaster.querySelectorAll('[data-part="toast"]:not([data-leaving])')].at(-1)
        ?.querySelector('button');

    const onKeyDown = event => {
        if (event.key === 'F8' && !event.altKey && !event.ctrlKey && !event.metaKey && !event.shiftKey) {
            const button = newestButton();
            const from = document.activeElement;
            button?.focus();
            // A modal dialog makes the toaster inert, and then focus stays where it was.
            if (button && document.activeElement === button) {
                event.preventDefault();
                if (!toaster.contains(from)) {
                    returnFocus = from;
                }
            }
        } else if (event.key === 'Escape' && toaster.contains(document.activeElement)) {
            // Reached by Tab or a click instead of F8, a toast inside a dialog hands focus to the dialog.
            const back = returnFocus?.isConnected ? returnFocus : toaster.closest('dialog');
            if (back) {
                event.preventDefault();
                back.focus();
            }
        }
    };
    const onFocusIn = event => {
        focused = event.target;
    };
    const onFocusOut = event => {
        if (event.relatedTarget && !toaster.contains(event.relatedTarget)) {
            focused = null;
        }
    };

    document.addEventListener('keydown', onKeyDown);
    toaster.addEventListener('focusin', onFocusIn);
    toaster.addEventListener('focusout', onFocusOut);

    return {
        // Runs after every render: shows or hides the toaster and keeps focus when the toast holding it closes.
        sync() {
            // A closing toast stays inert while it fades, so focus inside it is as lost as in a removed one.
            const active = document.activeElement;
            if (focused && (!focused.isConnected || focused.closest('[data-leaving]'))
                && (!active || active === document.body || active.closest('[data-leaving]'))) {
                const next = newestButton() ?? (returnFocus?.isConnected ? returnFocus : null);
                focused = null;
                next?.focus();
            }

            const open = toaster.matches(':popover-open');
            if (!toaster.querySelector('[data-part="toast"]')) {
                if (open) {
                    toaster.hidePopover();
                }

                return;
            }

            // Showing it again moves it above a non-modal dialog opened since; a toaster holding focus stays put.
            if (open && document.querySelector('dialog[open]') && !toaster.contains(document.activeElement)) {
                toaster.hidePopover();
            }

            if (!toaster.matches(':popover-open')) {
                toaster.showPopover();
            }
        },
        dispose() {
            document.removeEventListener('keydown', onKeyDown);
            toaster.removeEventListener('focusin', onFocusIn);
            toaster.removeEventListener('focusout', onFocusOut);
            if (toaster.matches(':popover-open')) {
                toaster.hidePopover();
            }
        },
    };
}
