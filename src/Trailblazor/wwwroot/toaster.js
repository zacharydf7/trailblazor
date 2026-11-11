import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
// Toasts never take focus themselves, so F8 brings focus to the newest one and Escape sends it back.
export function attachToaster(toaster, events) {
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
    // Only keyboard focus is handed on when its toast closes; a click on Dismiss leaves focus alone.
    const onFocusIn = event => {
        focused = event.target.matches(':focus-visible') ? event.target : null;
    };
    const onFocusOut = event => {
        if (event.relatedTarget && !toaster.contains(event.relatedTarget)) {
            focused = null;
        }
    };

    // The pointer anywhere on the stack, or the page being hidden, pauses every toast, so none moves from under the
    // pointer and none runs out unseen. A lifted finger fires pointerleave, while a tap's emulated mouse events send no
    // mouseleave until the next tap elsewhere.
    let pointerOn = false;
    let paused = false;
    const updatePause = () => {
        if ((pointerOn || document.hidden) !== paused) {
            paused = !paused;
            callDotNet(events, 'OnPause', paused);
        }
    };
    const onPointerEnter = () => {
        pointerOn = true;
        updatePause();
    };
    const onPointerLeave = () => {
        pointerOn = false;
        updatePause();
    };

    document.addEventListener('keydown', onKeyDown);
    toaster.addEventListener('focusin', onFocusIn);
    toaster.addEventListener('focusout', onFocusOut);
    toaster.addEventListener('pointerenter', onPointerEnter);
    toaster.addEventListener('pointerleave', onPointerLeave);
    document.addEventListener('visibilitychange', updatePause);
    updatePause();
    // Marks that F8 and the pause on hover or focus work, which tests wait for.
    toaster.dataset.ready = '';

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
            toaster.removeEventListener('pointerenter', onPointerEnter);
            toaster.removeEventListener('pointerleave', onPointerLeave);
            document.removeEventListener('visibilitychange', updatePause);
            if (toaster.matches(':popover-open')) {
                toaster.hidePopover();
            }
        },
    };
}
