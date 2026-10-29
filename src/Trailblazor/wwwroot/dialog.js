import { focusOrigin } from './pointer-origin.js';

// What Tab reaches; controls kept out of the tab order, such as a number field's step buttons, don't count.
const focusableSelector = ['a[href]', 'button:not([disabled])', 'input:not([disabled]):not([type="hidden"])',
    'select:not([disabled])', 'textarea:not([disabled])', '[tabindex]']
    .map(selector => `${selector}:not([tabindex="-1"])`)
    .join(', ');

// Open dialogs, oldest first; keys go to the newest, so a nested dialog closes before the one beneath it.
const stack = [];

// Minimized dialogs, in the order they were minimized, set out in rows from the bottom-end corner.
const tray = [];

// A row that runs out of room wraps upward, so every minimized dialog stays on screen to be restored.
function layOutTray() {
    if (tray.length === 0) {
        return;
    }

    // The CSS registers these as lengths, so they come back in px whatever the theme and font size.
    const style = getComputedStyle(tray[0]);
    const trayGap = parseFloat(style.getPropertyValue('--tb-dialog-tray-gap'));
    const room = document.documentElement.clientWidth - 2 * parseFloat(style.getPropertyValue('--tb-dialog-tray-edge'));
    let inline = 0;
    let block = 0;
    let rowHeight = 0;
    for (const dialog of tray) {
        if (inline > 0 && inline + dialog.offsetWidth > room) {
            inline = 0;
            block += rowHeight + trayGap;
            rowHeight = 0;
        }

        dialog.style.setProperty('--tb-dialog-tray-inline', `${inline}px`);
        dialog.style.setProperty('--tb-dialog-tray-block', `${block}px`);
        inline += dialog.offsetWidth + trayGap;
        rowHeight = Math.max(rowHeight, dialog.offsetHeight);
    }
}

window.addEventListener('resize', layOutTray);

function leaveTray(dialog) {
    const slot = tray.indexOf(dialog);
    if (slot >= 0) {
        tray.splice(slot, 1);
        layOutTray();
    }
}

// An element marked autofocus wins; a field component carries the attribute on its wrapper, so look inside it.
function dialogFocusTarget(dialog, options) {
    const body = dialog.querySelector('[data-part="body"]');
    const named = body?.querySelector('[autofocus]');
    const target = named?.matches(focusableSelector) ? named : named?.querySelector(focusableSelector);
    if (target) {
        return target;
    }

    return options.initialFocus === 'Dialog' ? dialog : body?.querySelector(focusableSelector) ?? dialog;
}

export function openDialog(dialog, dotNet, options) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!dialog) {
        return null;
    }

    const opener = focusOrigin();
    if (options.modal) {
        dialog.showModal();
    } else {
        // show() alone leaves a non-modal dialog out of the top layer; the manual popover puts it there.
        dialog.show();
        dialog.showPopover();
    }

    const entry = { dialog, options };
    stack.push(entry);
    dialogFocusTarget(dialog, options).focus();

    let finished = false;
    let pressedOnBackdrop = false;

    const onBackdrop = event => {
        if (event.target !== dialog) {
            return false;
        }

        const box = dialog.getBoundingClientRect();
        return event.clientX < box.left || event.clientX > box.right
            || event.clientY < box.top || event.clientY > box.bottom;
    };

    // Escape is stopped on keydown: cancelling the dialog's cancel event fails on a second press in every engine.
    // An open menu and a focused toast take Escape first; a tooltip, being a manual popover, doesn't.
    const onKeyDown = event => {
        if (event.key !== 'Escape' || stack.at(-1) !== entry
            || dialog.querySelector('[popover]:not([popover="manual"]):popover-open')
            || event.target.closest?.('[data-part="toaster"]')) {
            return;
        }

        if (!options.modal && !dialog.contains(document.activeElement)) {
            return;
        }

        event.preventDefault();
        if (options.dismissOnEscape) {
            dotNet.invokeMethodAsync('Cancel');
        }
    };
    // Other close requests, such as a back gesture, are refused once when the dialog mustn't be dismissed.
    const onCancel = event => {
        if (!options.dismissOnEscape) {
            event.preventDefault();
        }
    };
    // A close the browser made by itself still has to reach .NET, or the caller would wait forever.
    const onClose = () => {
        if (finish()) {
            dotNet.invokeMethodAsync('Cancel');
        }
    };
    // A drag that starts in a field and ends over the backdrop must not dismiss the dialog and lose the input.
    const onPointerDown = event => {
        pressedOnBackdrop = onBackdrop(event);
    };
    const onClick = event => {
        if (options.modal && options.dismissOnBackdrop && pressedOnBackdrop && onBackdrop(event)) {
            dotNet.invokeMethodAsync('Cancel');
        }
    };

    document.addEventListener('keydown', onKeyDown, { capture: true });
    dialog.addEventListener('cancel', onCancel);
    dialog.addEventListener('close', onClose);
    dialog.addEventListener('pointerdown', onPointerDown);
    dialog.addEventListener('click', onClick);

    // Removes the listeners and the stack entry once, however the dialog ends; true the first time.
    function finish() {
        if (finished) {
            return false;
        }

        finished = true;
        document.removeEventListener('keydown', onKeyDown, { capture: true });
        dialog.removeEventListener('cancel', onCancel);
        dialog.removeEventListener('close', onClose);
        dialog.removeEventListener('pointerdown', onPointerDown);
        dialog.removeEventListener('click', onClick);
        stack.splice(stack.indexOf(entry), 1);
        leaveTray(dialog);
        return true;
    }

    function hide() {
        if (!options.modal && dialog.matches(':popover-open')) {
            dialog.hidePopover();
        }

        if (dialog.open) {
            dialog.close();
        }
    }

    return {
        // 'normal', 'minimized' or 'maximized'.
        setWindowState(state) {
            dialog.dataset.windowState = state;
            leaveTray(dialog);
            if (state === 'minimized') {
                tray.push(dialog);
                layOutTray();
            }
        },
        // Closes the dialog and gives focus back to whatever opened it.
        close() {
            const hadFocus = dialog.contains(document.activeElement);
            finish();
            hide();
            if ((hadFocus || document.activeElement === document.body) && opener?.isConnected) {
                opener.focus();
            }
        },
        // Cleans up when the dialog is removed without being closed, for example when its host goes away.
        dispose() {
            if (finish()) {
                hide();
            }
        },
    };
}

// Resolves once the browser has drawn a frame, by which time a dialog it just opened is in the accessibility tree.
export function nextFrame() {
    return new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
}
