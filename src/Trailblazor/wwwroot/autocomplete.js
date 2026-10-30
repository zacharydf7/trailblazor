import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';

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

    const isOpen = () => listbox.matches(':popover-open');
    const options = () => [...listbox.querySelectorAll('[role="option"]')];

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
        if (input.disabled || isOpen()) {
            return;
        }

        listbox.showPopover();
        position = createAnchoredPosition(listbox, control, { side: 'Bottom', align: 'Start', offset: 4 });
        input.setAttribute('aria-expanded', 'true');
        events.invokeMethodAsync('OnToggle', true);
        if (browse) {
            events.invokeMethodAsync('OnBrowse');
        }
    };
    const close = () => {
        if (!isOpen()) {
            return;
        }

        listbox.hidePopover();
        position?.dispose();
        position = null;
        setActive(null);
        input.setAttribute('aria-expanded', 'false');
        events.invokeMethodAsync('OnToggle', false);
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
            case 'Escape':
                if (!isOpen()) {
                    return;
                }

                close();
                break;
            default:
                return;
        }

        event.preventDefault();
    };
    const onFocusOut = event => {
        if (!listbox.contains(event.relatedTarget)) {
            close();
        }
    };
    // Pressing an option would take focus from the input, which closes the list before the click arrives.
    const onListboxPointerDown = event => event.preventDefault();
    const onListboxClick = event => {
        if (event.target.closest('[role="option"]')) {
            close();
        }
    };
    const onListboxPointerMove = event => {
        const option = event.target.closest('[role="option"]');
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
    input.addEventListener('focusout', onFocusOut);
    listbox.addEventListener('pointerdown', onListboxPointerDown);
    listbox.addEventListener('click', onListboxClick);
    listbox.addEventListener('pointermove', onListboxPointerMove);
    // Marks that the keys work, which tests wait for.
    input.dataset.ready = '';

    return {
        // Returns whether the list ended up open, since a disabled field won't open.
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
            observer.disconnect();
            input.removeEventListener('input', onInput);
            input.removeEventListener('keydown', onKeyDown);
            input.removeEventListener('focusout', onFocusOut);
            listbox.removeEventListener('pointerdown', onListboxPointerDown);
            listbox.removeEventListener('click', onListboxClick);
            listbox.removeEventListener('pointermove', onListboxPointerMove);
        },
    };
}
