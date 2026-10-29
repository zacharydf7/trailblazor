import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';

// Keys, typing and the open list stay in the browser; choosing clicks the option, which reaches the server.
export function createSelect(combobox, listbox, toggle) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!combobox || !listbox) {
        return null;
    }

    const control = combobox.parentElement;
    const label = document.getElementById(combobox.getAttribute('aria-labelledby'));
    combobox.setAttribute('aria-expanded', 'false');
    let position = null;
    let active = null;
    let wasOpen = false;
    let typed = '';
    let typedAt = 0;

    const isOpen = () => listbox.matches(':popover-open');
    // Choosing in a multiple select toggles the option and leaves the list open for more.
    const multiple = () => listbox.getAttribute('aria-multiselectable') === 'true';
    const enabled = () => [...listbox.querySelectorAll('[role="option"]:not([aria-disabled="true"])')];
    const selected = () => listbox.querySelector('[role="option"][aria-selected="true"]:not([aria-disabled="true"])');

    const setActive = option => {
        active?.removeAttribute('data-active');
        active = option ?? null;
        if (active) {
            active.setAttribute('data-active', '');
            combobox.setAttribute('aria-activedescendant', active.id);
            active.scrollIntoView({ block: 'nearest' });
        } else {
            combobox.removeAttribute('aria-activedescendant');
        }
    };
    const open = target => {
        if (combobox.getAttribute('aria-disabled') === 'true') {
            return;
        }

        if (!isOpen()) {
            listbox.style.minWidth = `${control.offsetWidth}px`;
            listbox.showPopover();
        }

        setActive(target ?? selected() ?? enabled()[0]);
    };
    const close = () => {
        if (isOpen()) {
            listbox.hidePopover();
        }
    };
    const choose = option => {
        // The option's click handler sets the value; the listbox's click listener closes a single select.
        option?.click();
    };
    const move = (option, step) => {
        const options = enabled();
        const index = options.indexOf(option);
        return options[Math.min(Math.max(index + step, 0), options.length - 1)];
    };
    // Typing letters quickly finds the next option that starts with them.
    const typeahead = key => {
        const now = Date.now();
        typed = now - typedAt > 500 ? key.toLowerCase() : typed + key.toLowerCase();
        typedAt = now;
        const options = enabled();
        const current = options.indexOf(active ?? selected());
        const start = current < 0 ? 0 : current + (typed.length === 1 ? 1 : 0);
        const match = options.map((_, step) => options[(start + step) % options.length])
            .find(option => (option.dataset.text ?? option.textContent).trim().toLowerCase().startsWith(typed));
        if (match) {
            open(match);
        }
    };

    const onBeforeToggle = event => {
        if (event.newState !== 'open') {
            return;
        }

        // toggle fires a task later, possibly after a paint, so the list is placed in the next frame, before it paints.
        requestAnimationFrame(() => {
            if (isOpen() && !position) {
                position = createAnchoredPosition(listbox, control, { side: 'bottom', align: 'start', offset: 4 });
            }
        });
    };
    const onToggle = event => {
        const opened = event.newState === 'open';
        combobox.setAttribute('aria-expanded', String(opened));
        toggle.invokeMethodAsync('OnToggle', opened);
        if (!opened) {
            setActive(null);
            position?.dispose();
            position = null;
        }
    };
    const onKeyDown = event => {
        const key = event.key;
        if (event.ctrlKey || event.metaKey) {
            return;
        }

        if (!isOpen()) {
            if (['ArrowDown', 'ArrowUp', 'Enter', ' ', 'Home', 'End'].includes(key)) {
                event.preventDefault();
                const options = enabled();
                open(key === 'Home' ? options[0] : key === 'End' ? options.at(-1) : null);
            } else if (key.length === 1 && !event.altKey) {
                typeahead(key);
            }

            return;
        }

        switch (key) {
            case 'ArrowDown':
                setActive(move(active, 1));
                break;
            case 'ArrowUp':
                if (event.altKey) {
                    if (multiple()) {
                        close();
                    } else {
                        choose(active);
                    }
                } else {
                    setActive(move(active, -1));
                }

                break;
            case 'Home':
                setActive(enabled()[0]);
                break;
            case 'End':
                setActive(enabled().at(-1));
                break;
            case ' ':
                // Mid-word, Space is part of what's being typed, as in "New York".
                if (typed && Date.now() - typedAt <= 500) {
                    typeahead(key);
                    return;
                }

                choose(active);
                break;
            case 'Enter':
                choose(active);
                break;
            case 'Escape':
                close();
                break;
            case 'Tab':
                // Tab keeps the active option, as a native select does, then moves on.
                if (!multiple()) {
                    choose(active);
                }

                close();
                return;
            default:
                if (key.length === 1 && !event.altKey) {
                    typeahead(key);
                }

                return;
        }

        event.preventDefault();
    };
    // Pressing the combobox closes an open list before the click arrives, so that click mustn't reopen it.
    const onPointerDown = () => {
        wasOpen = isOpen();
    };
    const onControlClick = event => {
        // The clear button and the options, which sit inside the control too, handle their own clicks.
        if (event.target.closest('button') || listbox.contains(event.target)) {
            return;
        }

        combobox.focus();
        if (wasOpen || isOpen()) {
            close();
        } else {
            open();
        }

        wasOpen = false;
    };
    const onListboxClick = event => {
        const option = event.target.closest('[role="option"]');
        if (option && option.getAttribute('aria-disabled') !== 'true') {
            if (!multiple()) {
                close();
            }

            combobox.focus();
        }
    };
    const onListboxPointerMove = event => {
        const option = event.target.closest('[role="option"]:not([aria-disabled="true"])');
        if (option && option !== active) {
            setActive(option);
        }
    };
    const onLabelClick = () => combobox.focus();

    listbox.addEventListener('beforetoggle', onBeforeToggle);
    listbox.addEventListener('toggle', onToggle);
    listbox.addEventListener('click', onListboxClick);
    listbox.addEventListener('pointermove', onListboxPointerMove);
    combobox.addEventListener('keydown', onKeyDown);
    control.addEventListener('pointerdown', onPointerDown, { capture: true });
    control.addEventListener('click', onControlClick);
    label?.addEventListener('click', onLabelClick);

    return {
        // Returns whether the list ended up open, since a disabled select won't open.
        setOpen(value) {
            if (value) {
                open();
            } else {
                close();
            }

            return isOpen();
        },
        dispose() {
            close();
            position?.dispose();
            listbox.removeEventListener('beforetoggle', onBeforeToggle);
            listbox.removeEventListener('toggle', onToggle);
            listbox.removeEventListener('click', onListboxClick);
            listbox.removeEventListener('pointermove', onListboxPointerMove);
            combobox.removeEventListener('keydown', onKeyDown);
            control.removeEventListener('pointerdown', onPointerDown, { capture: true });
            control.removeEventListener('click', onControlClick);
            label?.removeEventListener('click', onLabelClick);
        },
    };
}
