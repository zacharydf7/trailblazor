import { autofocusTarget } from '../Trailblazor.Primitives/focus-scope.js';
import { focusOrigin } from '../Trailblazor.Primitives/pointer-origin.js';

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

// How far one press of an arrow key moves or resizes a dialog, in pixels.
const keyStep = 16;

// Small enough for a narrow window, and big enough to keep a header's buttons usable.
const minimumSize = { width: 240, height: 120 };

// How far the pointer moves before a press becomes a drag, so a small slip while clicking Move or Resize still opens
// its menu.
const dragThreshold = 4;

const clamp = (value, low, high) => Math.min(Math.max(value, low), Math.max(low, high));

// An element marked autofocus wins.
function dialogFocusTarget(dialog, options) {
    const body = dialog.querySelector('[data-part="body"]');
    return autofocusTarget(body)
        ?? (options.initialFocus === 'Dialog' ? dialog : body?.querySelector(focusableSelector) ?? dialog);
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
            dotNet.invokeMethodAsync('Dismiss', 'Escape');
        }
    };
    // Other close requests, such as a back gesture, ask the dialog like Escape. A browser that closes it anyway, as
    // one may on a second request, still reaches .NET through the close event.
    const onCancel = event => {
        event.preventDefault();
        if (options.dismissOnEscape) {
            dotNet.invokeMethodAsync('Dismiss', 'Escape');
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
            dotNet.invokeMethodAsync('Dismiss', 'Backdrop');
        }
    };

    const draggable = dialog.classList.contains('tb-dialog--draggable');
    const resizable = dialog.classList.contains('tb-dialog--resizable');
    let drag = null;
    // The Move or Resize button a drag started on, whose click as the drag ends mustn't open its menu.
    let draggedHandle = null;

    // A minimized or maximized dialog has its place set by its window state.
    const isNormal = () => (dialog.dataset.windowState ?? 'normal') === 'normal';

    function place(left, top) {
        dialog.style.setProperty('--tb-dialog-left', `${left}px`);
        dialog.style.setProperty('--tb-dialog-top', `${top}px`);
        dialog.dataset.placed = '';
    }

    // Pins the dialog where it is, so moving or resizing starts from its current box.
    function measure() {
        const box = dialog.getBoundingClientRect();
        place(box.left, box.top);
        return { left: box.left, top: box.top, width: box.width, height: box.height };
    }

    // Arrow keys and pointers move in screen directions, whichever way the page's text runs. The dialog stays whole
    // inside the window, so its header can always be reached.
    function moveBy(start, dx, dy) {
        const room = document.documentElement;
        place(clamp(start.left + dx, 0, room.clientWidth - start.width),
            clamp(start.top + dy, 0, room.clientHeight - start.height));
    }

    // The grip sits in the bottom-end corner, so on a right-to-left page the left edge follows it.
    function resizeBy(start, dx, dy) {
        const room = document.documentElement;
        const rightToLeft = getComputedStyle(dialog).direction === 'rtl';
        const width = rightToLeft
            ? clamp(start.width - dx, minimumSize.width, start.left + start.width)
            : clamp(start.width + dx, minimumSize.width, room.clientWidth - start.left);
        const height = clamp(start.height + dy, minimumSize.height, room.clientHeight - start.top);
        dialog.style.setProperty('--tb-dialog-resized-width', `${width}px`);
        dialog.style.setProperty('--tb-dialog-resized-height', `${height}px`);
        dialog.dataset.sized = '';
        place(rightToLeft ? start.left + start.width - width : start.left, start.top);
    }

    // The Move or Resize menu whose button the element is, if any; the menus' items don't count.
    const handleOf = element => element.closest?.('[data-part="trigger"]')
        ?.closest('[data-part="move"], [data-part="resize"]');

    // The header moves the dialog, apart from its buttons other than Move and an open Move menu; the grip resizes it.
    const onDragStart = event => {
        draggedHandle = null;
        const button = event.target.closest('button');
        const handle = button && handleOf(button);
        const grip = resizable && handle?.dataset.part === 'resize';
        const header = draggable && event.target.closest('[data-part="header"]');
        if (event.button !== 0 || !isNormal() || event.target.closest('[data-part="menu"]')
            || !(grip || (header && (!button || handle)))) {
            return;
        }

        // Stops the browser starting a drag of its own, which WebKit would take the rest of the pointer's moves for.
        event.preventDefault();
        // Capturing on the pressed button, not the dialog, keeps its click there, so a click still opens the menu.
        (handle ? button : header).setPointerCapture(event.pointerId);
        drag = { x: event.clientX, y: event.clientY, resize: grip, start: null, handle };
    };
    const onDragMove = event => {
        if (!drag) {
            return;
        }

        const dx = event.clientX - drag.x;
        const dy = event.clientY - drag.y;
        if (!drag.start) {
            if (Math.hypot(dx, dy) < dragThreshold) {
                return;
            }

            drag.start = measure();
        }

        (drag.resize ? resizeBy : moveBy)(drag.start, dx, dy);
    };
    const onDragEnd = event => {
        draggedHandle = event.type === 'pointerup' && drag?.start ? drag.handle : null;
        drag = null;
    };
    const onClickAfterDrag = event => {
        if (draggedHandle?.contains(event.target)) {
            event.preventDefault();
            event.stopPropagation();
        }

        draggedHandle = null;
    };
    // Runs before the menu's own keys, which would open it on Up and Down.
    const steps = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] };
    const onArrowKey = event => {
        draggedHandle = null;
        const step = steps[event.key];
        const handle = handleOf(event.target);
        if (!step || !handle || event.altKey || event.ctrlKey || event.metaKey || !isNormal()) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        const change = handle.dataset.part === 'move' ? moveBy : resizeBy;
        change(measure(), step[0] * keyStep, step[1] * keyStep);
    };
    // Brings a moved or resized dialog back inside a window that shrank: moving it first keeps its size if it fits.
    function keepInside() {
        if (!('placed' in dialog.dataset) || !isNormal()) {
            return;
        }

        moveBy(measure(), 0, 0);
        if ('sized' in dialog.dataset) {
            resizeBy(measure(), 0, 0);
        }
    }

    document.addEventListener('keydown', onKeyDown, { capture: true });
    dialog.addEventListener('cancel', onCancel);
    dialog.addEventListener('close', onClose);
    dialog.addEventListener('pointerdown', onPointerDown);
    dialog.addEventListener('click', onClick);
    if (draggable || resizable) {
        dialog.addEventListener('pointerdown', onDragStart);
        dialog.addEventListener('pointermove', onDragMove);
        dialog.addEventListener('pointerup', onDragEnd);
        dialog.addEventListener('pointercancel', onDragEnd);
        dialog.addEventListener('click', onClickAfterDrag, { capture: true });
        dialog.addEventListener('keydown', onArrowKey, { capture: true });
        window.addEventListener('resize', keepInside);
    }

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
        dialog.removeEventListener('pointerdown', onDragStart);
        dialog.removeEventListener('pointermove', onDragMove);
        dialog.removeEventListener('pointerup', onDragEnd);
        dialog.removeEventListener('pointercancel', onDragEnd);
        dialog.removeEventListener('click', onClickAfterDrag, { capture: true });
        dialog.removeEventListener('keydown', onArrowKey, { capture: true });
        window.removeEventListener('resize', keepInside);
        stack.splice(stack.indexOf(entry), 1);
        leaveTray(dialog);
        return true;
    }

    // The dialog fades out before it closes, as long as the theme's transitions last; under reduced motion they're
    // instant, so it closes at once. A hidden tab may hold transitions back, so the wait has a limit.
    function leave() {
        dialog.dataset.leaving = '';
        dialog.inert = true;
        const transitions = dialog.getAnimations({ subtree: true })
            .filter(animation => animation.effect?.target === dialog);
        if (transitions.length === 0) {
            return Promise.resolve();
        }

        const longest = Math.max(...transitions.map(transition => transition.effect.getComputedTiming().endTime));
        return Promise.race([Promise.allSettled(transitions.map(transition => transition.finished)),
            new Promise(resolve => setTimeout(resolve, longest + 100))]);
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
        // A place or size chosen from the Move or Resize menu replaces where the dialog was dragged or sized to.
        moveTo(from, to) {
            delete dialog.dataset.placed;
            dialog.classList.replace(from, to);
        },
        resizeTo(from, to) {
            delete dialog.dataset.sized;
            dialog.classList.replace(from, to);
            keepInside();
        },
        // 'normal', 'minimized' or 'maximized'.
        setWindowState(state) {
            dialog.dataset.windowState = state;
            leaveTray(dialog);
            if (state === 'minimized') {
                tray.push(dialog);
                layOutTray();
            } else if (state === 'normal') {
                // The window may have shrunk while the dialog was minimized or maximized.
                keepInside();
            }
        },
        // Closes the dialog and gives focus back to whatever opened it.
        async close() {
            const hadFocus = dialog.contains(document.activeElement);
            finish();
            await leave();
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
