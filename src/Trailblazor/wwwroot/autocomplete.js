import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { discard, flush } from './debounced-input.js';
import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';

// The list opens as the user types and the server fills it; the keys move an active option in the browser, and
// choosing clicks it, which reaches the server. Focus never leaves the input.
export function createAutocomplete(input, listbox, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!input || !listbox) {
        return null;
    }

    const control = input.closest('[data-part="control"]') ?? input.parentElement;
    let position = null;
    let active = null;
    let pressedOutside = false;

    const isOpen = () => listbox.matches(':popover-open');
    const choosable = '[role="option"]:not([aria-disabled="true"])';
    const options = () => [...listbox.querySelectorAll(choosable)];

    const setActive = option => {
        active?.removeAttribute('data-active');
        active = option ?? null;
        if (active) {
            active.setAttribute('data-active', '');
            input.setAttribute('aria-activedescendant', active.id);
            active.scrollIntoView({ block: 'nearest' });
        } else {
            input.removeAttribute('aria-activedescendant');
        }
    };
    // Typing searches by itself, so only an open from the keys asks the server to browse.
    const open = browse => {
        if (input.disabled || input.readOnly || isOpen()) {
            return;
        }

        listbox.showPopover();
        document.addEventListener('pointerdown', onDocumentPointerDown, true);
        document.addEventListener('pointercancel', onDocumentPointerCancel, true);
        document.addEventListener('contextmenu', onDocumentPointerCancel, true);
        document.addEventListener('click', onDocumentClick, true);
        document.addEventListener('keydown', onDocumentKeyDown, true);
        position = createAnchoredPosition(listbox, control, { side: 'Bottom', align: 'Start', offset: 4 });
        input.setAttribute('aria-expanded', 'true');
        callDotNet(events, 'OnToggle', true);
        if (browse) {
            callDotNet(events, 'OnBrowse');
        }
    };
    // Text still waiting out the delay is sent first, so the server handles Enter or Escape against the latest text,
    // and no input sent later reopens the list.
    const close = () => {
        flush(input);
        if (!isOpen()) {
            return;
        }

        listbox.hidePopover();
        stopListening();
        position?.dispose();
        position = null;
        setActive(null);
        input.setAttribute('aria-expanded', 'false');
        callDotNet(events, 'OnToggle', false);
    };
    const move = step => {
        const list = options();
        if (list.length === 0) {
            return;
        }

        const index = list.indexOf(active);
        setActive(index < 0 ? (step > 0 ? list[0] : list.at(-1)) : list[(index + step + list.length) % list.length]);
    };

    const onInput = () => {
        setActive(null);
        open(false);
    };
    const onKeyDown = event => {
        // The list can still hold results from before the field turned read-only, and the keys must not reach them.
        if (input.readOnly || isComposing(event)) {
            return;
        }

        switch (event.key) {
            case 'ArrowDown':
                if (!isOpen()) {
                    open(true);
                    if (event.altKey) {
                        break;
                    }
                }

                move(1);
                break;
            case 'ArrowUp':
                if (!isOpen()) {
                    open(true);
                }

                move(-1);
                break;
            case 'Enter':
                // With no option active, Enter closes the list, and the browser's change event settles what was typed.
                if (!active) {
                    close();
                    return;
                }

                active.click();
                break;
            default:
                return;
        }

        event.preventDefault();
    };
    // Focus may never have reached the field when code opens the list, so closing can't rely on the field's events.
    // Judge a click by where its press began: a drag out of the field fires the click on an ancestor of the field. Only a
    // primary press on something enabled ends in a click.
    const onDocumentPointerDown = event => {
        pressedOutside = event.button === 0 && !control.contains(event.target) && !event.target.closest(':disabled');
    };
    // A press that turns into a scroll, a drag and drop or a context menu (which Ctrl+click opens on a Mac) never clicks,
    // so the list closes here if focus has left the field.
    const onDocumentPointerCancel = () => {
        if (pressedOutside) {
            pressedOutside = false;
            if (!control.contains(document.activeElement)) {
                close();
            }
        }
    };
    // Waits for the click's own handlers, so a button that toggles the list doesn't see it closed and reopen it.
    const onDocumentClick = () => {
        if (pressedOutside) {
            setTimeout(close);
        }
    };
    // Added only while the list is open, so Escape on a closed list reaches what surrounds it, such as a dialog.
    const onDocumentKeyDown = event => {
        if (event.key === 'Escape' && !isComposing(event)) {
            event.preventDefault();
            close();
        }
    };
    const stopListening = () => {
        document.removeEventListener('pointerdown', onDocumentPointerDown, true);
        document.removeEventListener('pointercancel', onDocumentPointerCancel, true);
        document.removeEventListener('contextmenu', onDocumentPointerCancel, true);
        document.removeEventListener('click', onDocumentClick, true);
        pressedOutside = false;
        document.removeEventListener('keydown', onDocumentKeyDown, true);
    };
    // A click on the clear button is remembered: Safari doesn't focus a pressed button, so focus drops to the page and
    // the clear's refocus would look as if it came from outside.
    let clearing = false;
    const onControlClick = event => {
        clearing = event.target.closest('[data-part="clear"]') !== null;
    };
    // With OpenOnFocus, focus coming into the input from outside the field opens and browses the list as the arrow keys
    // do; focus back from within the field, such as from the list or the clear button, doesn't.
    const onFocusIn = event => {
        const fromClear = clearing;
        clearing = false;
        if (input.hasAttribute('data-open-on-focus') && !fromClear && !control.contains(event.relatedTarget)) {
            open(true);
        }
    };
    // A primary press elsewhere is left to onDocumentClick, so a button that toggles the list doesn't reopen it.
    const onFocusOut = event => {
        if (!listbox.contains(event.relatedTarget) && !pressedOutside) {
            close();
        }
    };
    // Pressing an option would take focus from the input, which closes the list before the click arrives.
    const onListboxPointerDown = event => event.preventDefault();
    // A choice replaces the typed text, so text still waiting is dropped rather than searched and reported first.
    const onListboxClick = event => {
        if (event.target.closest(choosable)) {
            discard(input);
            close();
        }
    };
    const onListboxPointerMove = event => {
        const option = event.target.closest(choosable);
        if (option && option !== active) {
            setActive(option);
        }
    };
    // An active option the server took away, as a new search ran, is no longer active.
    const observer = new MutationObserver(() => {
        if (active && !listbox.contains(active)) {
            setActive(null);
        }
    });

    observer.observe(listbox, { childList: true, subtree: true });
    input.addEventListener('input', onInput);
    input.addEventListener('keydown', onKeyDown);
    input.addEventListener('focusin', onFocusIn);
    input.addEventListener('focusout', onFocusOut);
    control.addEventListener('click', onControlClick);
    listbox.addEventListener('pointerdown', onListboxPointerDown);
    listbox.addEventListener('click', onListboxClick);
    listbox.addEventListener('pointermove', onListboxPointerMove);
    // Marks that the keys work, which tests wait for.
    input.dataset.ready = '';

    return {
        // Returns whether the list ended up open, since a disabled or read-only field won't open.
        setOpen(value) {
            if (value) {
                open(false);
            } else {
                close();
            }

            return isOpen();
        },
        dispose() {
            close();
            stopListening();
            observer.disconnect();
            input.removeEventListener('input', onInput);
            input.removeEventListener('keydown', onKeyDown);
            input.removeEventListener('focusin', onFocusIn);
            input.removeEventListener('focusout', onFocusOut);
            control.removeEventListener('click', onControlClick);
            listbox.removeEventListener('pointerdown', onListboxPointerDown);
            listbox.removeEventListener('click', onListboxClick);
            listbox.removeEventListener('pointermove', onListboxPointerMove);
        },
    };
}
