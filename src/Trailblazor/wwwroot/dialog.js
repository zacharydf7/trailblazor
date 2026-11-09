import { autofocusTarget, firstTabbable } from '../Trailblazor.Primitives/focus-scope.js';
import { focusForLastInput, focusOrigin } from '../Trailblazor.Primitives/pointer-origin.js';
import { alwaysUsable, hold, release } from './inert.js';

// Open dialogs, back to front; keys go to the frontmost, or to a modeless one that has focus, so a nested dialog closes
// before the one beneath it.
const stack = [];

// A popup that closes itself on a click outside it, and that a dialog coming forward would cover.
const lightDismissPopup = '[popover]:not([popover="manual"]):popover-open';

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
        ?? (options.initialFocus === 'Dialog' ? dialog : firstTabbable(body) ?? dialog);
}

// The elements the topmost modal on the page layer made inert, with that dialog.
let pageBlock = { dialog: null, elements: [] };

// The top layer would make the page inert, popups other scripts add to it included, so a modal on the page layer
// makes everything beside it and its ancestors inert itself. Only the topmost one counts, however they close.
function blockPage() {
    const topEntry = stack.findLast(entry => entry.modal && !entry.topLayer);
    const top = topEntry?.dialog ?? null;
    if (top === pageBlock.dialog) {
        return;
    }

    const elements = [];
    for (let node = top; node && node !== document.body; node = node.parentElement) {
        for (const sibling of node.parentElement?.children ?? []) {
            if (sibling !== node && !sibling.matches(topEntry.usable)) {
                elements.push(sibling);
            }
        }
    }

    // Hold the new block before releasing the previous one, so what both cover never takes input in between.
    hold(elements);
    release(pageBlock.elements);
    pageBlock = { dialog: top, elements };
}

function show(dialog, modal, topLayer) {
    if (!topLayer) {
        // Out of the top layer, it draws its own backdrop, under popups added to the page.
        dialog.removeAttribute('popover');
        dialog.toggleAttribute('data-backdrop', modal);
        if (modal) {
            dialog.setAttribute('aria-modal', 'true');
        } else {
            dialog.removeAttribute('aria-modal');
        }

        dialog.show();
    } else if (modal) {
        dialog.removeAttribute('popover');
        dialog.showModal();
    } else {
        // show() alone leaves a non-modal dialog out of the top layer; the manual popover puts it there.
        dialog.popover = 'manual';
        dialog.show();
        dialog.showPopover();
    }
}

// On the page layer, a dialog's place in the stack sets how high it draws.
function numberLevels() {
    stack.forEach((entry, level) => entry.dialog.style.setProperty('--tb-dialog-level', level));
}

// Hiding and showing a manual popover in one task moves it to the top of the top layer. Inert meanwhile, it has nothing
// to give focus to, so focus stays where it is and no focus event fires.
function moveToTopOfLayer(element) {
    if (element.matches(':popover-open')) {
        hold([element]);
        element.hidePopover();
        element.showPopover();
        release([element]);
    }
}

// A dialog moves to the front with the ones nested in it. Then any tooltip or list open from it, and the toaster, go
// back above them.
function raise(entry) {
    const raised = [entry, ...stack.filter(other => other !== entry && entry.dialog.contains(other.dialog))];
    for (const each of raised) {
        stack.splice(stack.indexOf(each), 1);
        stack.push(each);
        moveToTopOfLayer(each.dialog);
    }

    entry.dialog.querySelectorAll('[popover="manual"]:popover-open:not(dialog)').forEach(moveToTopOfLayer);
    document.querySelectorAll('[data-part="toaster"]').forEach(moveToTopOfLayer);
    numberLevels();
}

// The first item of a computed list such as "ease, cubic-bezier(0, 0, 1, 1)", whose items can hold commas themselves.
function firstListItem(list) {
    let depth = 0;
    for (let index = 0; index < list.length; index++) {
        if (list[index] === '(') {
            depth++;
        } else if (list[index] === ')') {
            depth--;
        } else if (list[index] === ',' && depth === 0) {
            return list.slice(0, index).trim();
        }
    }

    return list.trim();
}

export function openDialog(dialog, dotNet, options, outsidePopups) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!dialog) {
        return null;
    }

    const opener = focusOrigin();
    // A modal in the top layer makes the page inert, so a dialog it opens joins it there.
    const topLayer = options.topLayer || stack.some(entry => entry.modal && entry.topLayer);
    let usable = null;
    if (!topLayer) {
        // A selector the app got wrong throws here, quoted in the error, before the dialog is half set up.
        usable = [...alwaysUsable, ...outsidePopups].join(', ');
        document.querySelector(usable);
        dialog.dataset.layer = 'page';
    }

    show(dialog, options.modal, topLayer);
    // Whether it is modal now, which can change while it's open.
    const entry = { dialog, options, modal: options.modal, topLayer, usable };
    stack.push(entry);
    numberLevels();
    blockPage();
    focusForLastInput(dialogFocusTarget(dialog, options));

    let finished = false;
    let pressedOnBackdrop = false;

    // On the page layer, a click on the inert page lands on one of the dialog's ancestors, while one on a popup added
    // to the page lands on that popup.
    const onBackdrop = event => {
        if (!entry.topLayer) {
            return stack.at(-1) === entry && event.target !== dialog && event.target.contains?.(dialog);
        }

        if (event.target !== dialog) {
            return false;
        }

        const box = dialog.getBoundingClientRect();
        return event.clientX < box.left || event.clientX > box.right
            || event.clientY < box.top || event.clientY > box.bottom;
    };

    // Escape is stopped on keydown: canceling the dialog's cancel event fails on a second press in every engine.
    // It is heard as it bubbles, so whatever inside used it first, such as an autocomplete list or a drag, keeps it.
    // An open menu, a shown tooltip, a focused toast and the topmost layer, such as a closable overlay, take it first.
    // The frontmost dialog takes it, and so does a modeless one with focus, since that is the window the user is
    // working in; focus in a dialog nested inside it belongs to the nested one.
    const onKeyDown = event => {
        const frontmost = stack.at(-1) === entry;
        if (event.key !== 'Escape' || event.defaultPrevented
            || !(frontmost || !entry.modal && document.activeElement?.closest('dialog') === dialog)
            || dialog.querySelector(`${lightDismissPopup}, [role="tooltip"]:popover-open, [data-dismissable-layer]`)
            || event.target.closest?.('[data-part="toaster"]')) {
            return;
        }

        const outside = !dialog.contains(document.activeElement);
        if (!entry.modal && outside) {
            return;
        }

        // A page-layer modal leaves keys pressed in a popup added to the page above it to that popup.
        if (!entry.topLayer && outside && document.activeElement !== document.body) {
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
    // A close the browser made by itself still has to reach .NET, or the caller would wait forever. A dialog switching
    // between modal and modeless closes and opens again, so it is open by the time its close event arrives.
    const onClose = () => {
        if (!dialog.open && finish()) {
            dotNet.invokeMethodAsync('Cancel');
        }
    };
    // A drag that starts in a field and ends over the backdrop must not dismiss the dialog and lose the input.
    const onPointerDown = event => {
        pressedOnBackdrop = onBackdrop(event);
    };
    // A press or focus in a modeless dialog brings it to the front, if a dialog outside it is drawn above it. It waits
    // while a popup that closes on its own, or a modal in the top layer, is open from it, since it would cover them.
    const bringToFront = () => {
        if (!entry.modal && stack.slice(stack.indexOf(entry)).some(other => !dialog.contains(other.dialog))
            && !dialog.querySelector(`${lightDismissPopup}, :modal`)) {
            raise(entry);
        }
    };
    const onClick = event => {
        if (entry.modal && options.dismissOnBackdrop && pressedOnBackdrop && onBackdrop(event)) {
            dotNet.invokeMethodAsync('Dismiss', 'Backdrop');
        }
    };

    // A dialog can be moved and resized only while it is modeless, so these are asked each time.
    const draggable = () => dialog.classList.contains('tb-dialog--draggable');
    const resizable = () => dialog.classList.contains('tb-dialog--resizable');
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
        const grip = resizable() && handle?.dataset.part === 'resize';
        const header = draggable() && event.target.closest('[data-part="header"]');
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

    document.addEventListener('keydown', onKeyDown);
    dialog.addEventListener('pointerdown', bringToFront, { capture: true });
    dialog.addEventListener('focusin', bringToFront, { capture: true });
    dialog.addEventListener('cancel', onCancel);
    dialog.addEventListener('close', onClose);
    document.addEventListener('pointerdown', onPointerDown);
    document.addEventListener('click', onClick);
    if (options.draggable || options.resizable) {
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
        document.removeEventListener('keydown', onKeyDown);
        dialog.removeEventListener('cancel', onCancel);
        dialog.removeEventListener('close', onClose);
        dialog.removeEventListener('pointerdown', bringToFront, { capture: true });
        dialog.removeEventListener('focusin', bringToFront, { capture: true });
        document.removeEventListener('pointerdown', onPointerDown);
        document.removeEventListener('click', onClick);
        dialog.removeEventListener('pointerdown', onDragStart);
        dialog.removeEventListener('pointermove', onDragMove);
        dialog.removeEventListener('pointerup', onDragEnd);
        dialog.removeEventListener('pointercancel', onDragEnd);
        dialog.removeEventListener('click', onClickAfterDrag, { capture: true });
        dialog.removeEventListener('keydown', onArrowKey, { capture: true });
        window.removeEventListener('resize', keepInside);
        stack.splice(stack.indexOf(entry), 1);
        blockPage();
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
        if (dialog.matches(':popover-open')) {
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
            dialog.style.removeProperty('--tb-dialog-width');
            dialog.classList.replace(from, to);
            keepInside();
        },
        // The window state is 'normal', 'minimized' or 'maximized'.
        async setState(state, modal) {
            // A dialog that is closing has left the stack and the tray, and mustn't open again.
            if (finished) {
                return;
            }

            dialog.dataset.windowState = state;
            leaveTray(dialog);
            if (state === 'minimized') {
                tray.push(dialog);
                layOutTray();
            } else if (state === 'normal') {
                // The window may have shrunk while the dialog was minimized or maximized.
                keepInside();
            }

            if (modal === entry.modal) {
                return;
            }

            // An open dialog can't change between modal and modeless, so it closes and opens again the other way.
            // Focus stays where it was, or moves into the dialog as it turns modal.
            const focused = dialog.contains(document.activeElement) ? document.activeElement : null;
            entry.modal = modal;
            hide();
            show(dialog, modal, entry.topLayer);
            // Opening again puts it above the others, so Escape goes to it first.
            stack.splice(stack.indexOf(entry), 1);
            stack.push(entry);
            numberLevels();
            blockPage();
            focusForLastInput(focused ?? dialogFocusTarget(dialog, options));
            // A modal's own live regions join the accessibility tree with the next frame, as when it opens.
            if (modal) {
                await nextFrame();
            }
        },
        // Closes the dialog and gives focus back to whatever opened it, unless focus has already moved on.
        async close() {
            const giveBack = dialog.contains(document.activeElement) || document.activeElement === document.body;
            finish();
            // A modal keeps the page inert, so it fades out as a popover and the page takes input at once. Its content
            // goes inert first, so showing it again doesn't move focus into it.
            dialog.dataset.leaving = '';
            for (const child of dialog.children) {
                child.inert = true;
            }

            if (entry.modal && entry.topLayer) {
                hide();
                show(dialog, false, true);
                // Shown again, it would start its transition afresh, which WebKit sometimes drops, so it fades out as
                // an animation with the transition's timing; reduced motion makes that 0s. A drawer also slides back
                // to the translate its data-leaving rule sets; a dialog has no translate, so it only fades.
                const { transitionDuration, transitionTimingFunction, translate } = getComputedStyle(dialog);
                const timing = {
                    duration: parseFloat(transitionDuration) * 1000,
                    easing: firstListItem(transitionTimingFunction),
                };
                if (timing.duration > 0) {
                    dialog.animate([{ opacity: 1, translate: 'none' }, { opacity: 0, translate }], timing);
                    dialog.animate([{ opacity: 1 }, { opacity: 0 }], { ...timing, pseudoElement: '::backdrop' });
                }
            }

            if (giveBack && opener?.isConnected) {
                focusForLastInput(opener);
            }

            await leave();
            hide();
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
