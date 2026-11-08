import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { createTypeahead } from './typeahead.js';
import { watchForUnnamedOptions } from './unnamed-options.js';

// Keys, typing and the open list stay in the browser; choosing clicks the option, which reaches the server.
export function createSelect(combobox, listbox, toggle) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!combobox || !listbox) {
        return null;
    }

    const control = combobox.parentElement;
    // A filterable select's popup holds a filter box and a status above and below the listbox.
    const popup = listbox.closest('[popover]');
    const filter = popup.querySelector('[data-part="filter"]');
    const status = popup.querySelector('[data-part="no-results"]');
    const label = document.getElementById(combobox.getAttribute('aria-labelledby'));
    // Marks that the keys work, which tests wait for.
    combobox.dataset.ready = '';
    const unnamed = watchForUnnamedOptions(listbox);
    let position = null;
    let active = null;
    let wasOpen = false;
    const typing = createTypeahead();

    const isOpen = () => popup.matches(':popover-open');
    // Choosing in a multiple select toggles the option and leaves the list open for more.
    const multiple = () => listbox.getAttribute('aria-multiselectable') === 'true';
    const enabled = () => [...listbox.querySelectorAll('[role="option"]:not([aria-disabled="true"]):not([hidden])')];
    const selected = () => listbox.querySelector('[role="option"][aria-selected="true"]:not([aria-disabled="true"])');

    const setActive = option => {
        active?.removeAttribute('data-active');
        active = option ?? null;
        // Either the combobox or the filter has focus, and each names the active option.
        for (const owner of [combobox, filter].filter(Boolean)) {
            if (active) {
                owner.setAttribute('aria-activedescendant', active.id);
            } else {
                owner.removeAttribute('aria-activedescendant');
            }
        }

        if (active) {
            active.setAttribute('data-active', '');
            active.scrollIntoView({ block: 'nearest' });
        }
    };
    // Matching ignores case and accents, so "eagle" finds "Éagle".
    const fold = text => text.normalize('NFD').replace(/\p{M}/gu, '').toLocaleLowerCase();
    const applyFilter = () => {
        const query = fold(filter.value.trim());
        for (const option of listbox.querySelectorAll('[role="option"]')) {
            // Choosing every option means nothing while only some are shown, and an empty list's message always shows.
            const hide = option.dataset.part === 'status' ? false : option.dataset.part === 'select-all' ? query !== ''
                : !fold(option.dataset.text || option.textContent).includes(query);
            if (option.hidden !== hide) {
                option.hidden = hide;
            }
        }

        for (const group of listbox.querySelectorAll('[role="group"]')) {
            const hide = !group.querySelector('[role="option"]:not([hidden])');
            if (group.hidden !== hide) {
                group.hidden = hide;
            }
        }

        const text = query && !listbox.querySelector('[role="option"]:not([hidden])') ? status.dataset.text : '';
        if (status.textContent !== text) {
            status.textContent = text;
        }
    };
    // Opening from code leaves focus where it is; the user opening it moves focus to the filter.
    const open = (target, focus = true) => {
        if (combobox.getAttribute('aria-disabled') === 'true' || combobox.getAttribute('aria-readonly') === 'true') {
            return;
        }

        if (!isOpen()) {
            popup.showPopover();
        }

        if (focus) {
            filter?.focus();
        }

        setActive(target ?? selected() ?? enabled()[0]);
    };
    const close = () => {
        if (isOpen()) {
            if (popup.contains(document.activeElement)) {
                combobox.focus();
            }

            popup.hidePopover();
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
    const rowsShown = () => Math.max(1, Math.floor(listbox.clientHeight / (enabled()[0]?.offsetHeight || 1)));
    const typeahead = key => {
        // Typing finds an option by its name, so it passes over "Select all".
        const match = typing.find(enabled().filter(option => option.dataset.part !== 'select-all'),
            active ?? selected(), key);
        if (match) {
            open(match);
        }
    };

    const onBeforeToggle = event => {
        if (event.newState !== 'open') {
            // Cleared here, since a quick close and reopen merges into one toggle event that only says open.
            if (filter) {
                filter.value = '';
                applyFilter();
            }

            return;
        }

        // toggle fires a task later, possibly after a paint, so the list is placed in the next frame, before it paints.
        requestAnimationFrame(() => {
            if (isOpen() && !position) {
                position = createAnchoredPosition(popup, control, { side: 'Bottom', align: 'Start', offset: 4 });
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
    const chips = () => [...combobox.querySelectorAll('[data-part="chip"]')];
    // The server finds the chip's option by its id and says the text the chip shows.
    const removeChip = chip => toggle.invokeMethodAsync('OnRemoveChip', chip.dataset.option, chip.textContent.trim());
    const onKeyDown = event => {
        const key = event.key;
        if (event.ctrlKey || event.metaKey) {
            return;
        }

        // Backspace removes the last chip that can go, as in a field of tags; WebKit would otherwise go back a page.
        if (key === 'Backspace') {
            const removable = chips().findLast(chip => chip.querySelector('[data-part="chip-remove"]'));
            if (removable) {
                event.preventDefault();
                removeChip(removable);
            }

            return;
        }

        if (!isOpen()) {
            if (['ArrowDown', 'ArrowUp', 'Enter', ' ', 'Home', 'End'].includes(key)) {
                event.preventDefault();
                const options = enabled();
                open(key === 'Home' ? options[0] : key === 'End' ? options.at(-1) : null);
            } else if (key.length === 1 && !event.altKey) {
                // With a filter, typing on the closed select starts the filter rather than jumping to an option.
                if (filter) {
                    event.preventDefault();
                    open();
                    if (isOpen()) {
                        filter.value = key;
                        onFilterInput();
                    }
                } else {
                    typeahead(key);
                }
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
            case 'PageDown':
                setActive(move(active, rowsShown()));
                break;
            case 'PageUp':
                setActive(move(active, -rowsShown()));
                break;
            case ' ':
                if (typing.typing()) {
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
    const onFilterInput = () => {
        applyFilter();
        setActive(enabled()[0]);
    };
    // The list keys work as on the combobox; Home, End and the other editing keys stay with the text box.
    const onFilterKeyDown = event => {
        if (['ArrowDown', 'ArrowUp', 'PageDown', 'PageUp', 'Enter', 'Escape', 'Tab'].includes(event.key)) {
            onKeyDown(event);
        }
    };
    // Options can arrive after the user has typed, such as when they load as the list opens.
    const observer = filter ? new MutationObserver(applyFilter) : null;
    observer?.observe(listbox, { childList: true, subtree: true });
    // Pressing the combobox closes an open list before the click arrives, so that click mustn't reopen it.
    const onPointerDown = () => {
        wasOpen = isOpen();
    };
    const onControlClick = event => {
        const remove = event.target.closest('[data-part="chip-remove"]');
        if (remove) {
            removeChip(remove.closest('[data-part="chip"]'));
            return;
        }

        // The clear button and the popup, which sit inside the control too, handle their own clicks.
        if (event.target.closest('button') || popup.contains(event.target)) {
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
    // A press on a disabled option or a group heading would otherwise drop focus to the page, and the keys with it.
    const onPopupPointerDown = event => {
        if (event.target !== filter) {
            event.preventDefault();
        }
    };
    const onListboxClick = event => {
        const option = event.target.closest('[role="option"]');
        if (option && option.getAttribute('aria-disabled') !== 'true') {
            if (!multiple()) {
                close();
            }

            (isOpen() ? filter ?? combobox : combobox).focus();
        }
    };
    const onListboxPointerMove = event => {
        const option = event.target.closest('[role="option"]:not([aria-disabled="true"])');
        if (option && option !== active) {
            setActive(option);
        }
    };
    const onLabelClick = () => combobox.focus();

    popup.addEventListener('beforetoggle', onBeforeToggle);
    popup.addEventListener('toggle', onToggle);
    popup.addEventListener('pointerdown', onPopupPointerDown);
    filter?.addEventListener('input', onFilterInput);
    filter?.addEventListener('keydown', onFilterKeyDown);
    listbox.addEventListener('click', onListboxClick);
    listbox.addEventListener('pointermove', onListboxPointerMove);
    combobox.addEventListener('keydown', onKeyDown);
    control.addEventListener('pointerdown', onPointerDown, { capture: true });
    control.addEventListener('click', onControlClick);
    label?.addEventListener('click', onLabelClick);

    return {
        // Returns whether the list ended up open, since a disabled or read-only select won't open.
        setOpen(value) {
            if (value) {
                open(null, false);
            } else {
                close();
            }

            return isOpen();
        },
        dispose() {
            close();
            position?.dispose();
            observer?.disconnect();
            unnamed.disconnect();
            popup.removeEventListener('beforetoggle', onBeforeToggle);
            popup.removeEventListener('toggle', onToggle);
            popup.removeEventListener('pointerdown', onPopupPointerDown);
            filter?.removeEventListener('input', onFilterInput);
            filter?.removeEventListener('keydown', onFilterKeyDown);
            listbox.removeEventListener('click', onListboxClick);
            listbox.removeEventListener('pointermove', onListboxPointerMove);
            combobox.removeEventListener('keydown', onKeyDown);
            control.removeEventListener('pointerdown', onPointerDown, { capture: true });
            control.removeEventListener('click', onControlClick);
            label?.removeEventListener('click', onLabelClick);
        },
    };
}
