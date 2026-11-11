import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { createTypeahead } from './typeahead.js';
import { watchForUnnamedOptions } from './unnamed-options.js';
import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';

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
    // A long list from Items is virtualized: only the options in view are in the page, each with its place.
    const virtual = () => 'count' in listbox.dataset;
    const count = () => Number(listbox.dataset.count);
    const place = option => option?.dataset.index === undefined ? null : Number(option.dataset.index);
    const atPlace = index => listbox.querySelector(`[role="option"][data-index="${index}"]`);
    // Scrolls toward an option out of the page, from a drawn option's place and height.
    const scrollToward = index => {
        const near = listbox.querySelector('[role="option"][data-index]');
        if (near) {
            listbox.scrollTop += (index - place(near)) * near.offsetHeight
                + near.getBoundingClientRect().top - listbox.getBoundingClientRect().top;
        }
    };
    // Brings an option that isn't in the page into view and waits for the server to draw it. A list just opened has
    // none drawn to measure from, and options of different heights can make a guess miss, so each drawing scrolls
    // closer, a few times at most. A newer search stops it.
    const optionWait = 2000;
    const render = (index, ticket) => new Promise(resolve => {
        let tries = 5;
        const done = option => {
            watch.disconnect();
            clearTimeout(timer);
            resolve(option);
        };
        const watch = new MutationObserver(() => {
            if (ticket !== reaching || atPlace(index)) {
                done(atPlace(index));
            } else if (--tries > 0) {
                scrollToward(index);
            }
        });
        watch.observe(listbox, { childList: true, subtree: true });
        const timer = setTimeout(() => done(atPlace(index)), optionWait);
        scrollToward(index);
    });
    // The first enabled option at or past a place, in the step's direction, or null. A newer search, or a key that
    // moves without one, replaces it, and then it gives undefined.
    let reaching = 0;
    const reach = async (index, step) => {
        const ticket = ++reaching;
        for (let at = index; at >= 0 && at < count(); at += step) {
            const option = atPlace(at) ?? await render(at, ticket);
            if (ticket !== reaching) {
                return undefined;
            }

            if (!option) {
                return null;
            }

            if (option.getAttribute('aria-disabled') !== 'true') {
                return option;
            }
        }

        return null;
    };
    const reachThen = (index, step, then) => reach(index, step).then(option => option && isOpen() && then(option));
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
            // The server filters a virtualized list.
            const hide = option.dataset.part === 'status' || place(option) !== null ? false
                : option.dataset.part === 'select-all' ? query !== ''
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

        const text = query && !listbox.querySelector('[role="option"]:not([hidden])') && !(virtual() && count() > 0)
            ? status.dataset.text : '';
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

        // A virtualized list renders its options a moment after it opens, and its chosen one may be far down.
        const chosen = listbox.dataset.chosen;
        const first = enabled()[0];
        if (!target && !selected() && virtual() && (chosen !== undefined || !first)) {
            setActive(first);
            reachThen(chosen === undefined ? 0 : Number(chosen), 1, setActive);
            return;
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
    // In a virtualized list the next option may not be in the page. Options before the list, such as "Select all",
    // aren't virtualized.
    const moveVirtual = key => {
        const before = enabled().filter(option => place(option) === null);
        const from = place(active);
        const last = count() - 1;
        const rows = rowsShown();
        const [index, step] = {
            ArrowDown: [from === null ? 0 : from + 1, 1],
            ArrowUp: [from === null ? -1 : from - 1, -1],
            Home: [before.length ? -1 : 0, 1],
            End: [last, -1],
            PageDown: [Math.min((from ?? -1) + rows, last), 1],
            PageUp: [from === null ? -1 : Math.max(from - rows, 0), -1],
        }[key];
        const top = () => {
            reaching++;
            setActive(before[0] ?? active);
        };
        if (index < 0) {
            top();
            return;
        }

        // Moving down past the last enabled option stays on it; moving up past the first reaches "Select all".
        reach(index, step)
            .then(option => option !== null || step < 0 ? option : reach(Math.min(index, last), -1))
            .then(option => {
                if (option !== undefined && isOpen()) {
                    option ? setActive(option) : top();
                }
            });
    };
    const rowsShown = () => Math.max(1, Math.floor(listbox.clientHeight / (enabled()[0]?.offsetHeight || 1)));
    const typeahead = key => {
        if (virtual()) {
            const typed = typing.type(key);
            const from = place(active ?? selected()) ?? Number(listbox.dataset.chosen ?? -1);
            // A key pressed while the server looks replaces this search, as it would any other.
            const ticket = ++reaching;
            callDotNet(toggle, 'OnFind', from, typed).then(index => {
                if (ticket !== reaching || index < 0) {
                    return;
                }

                // A closed list renders no options, so it opens first, unless focus has moved on.
                if (!isOpen()) {
                    if (document.activeElement !== combobox) {
                        return;
                    }

                    open(null, false);
                }

                reachThen(index, 1, setActive);
            });
            return;
        }

        // Typing finds an option by its name, so it passes over "Select all".
        const match = typing.find(enabled().filter(option => option.dataset.part !== 'select-all'),
            active ?? selected(), key);
        if (match) {
            open(match);
        }
    };

    const onBeforeToggle = event => {
        if (event.newState !== 'open') {
            // A search still waiting on the server ends with the list, however it closed.
            reaching++;
            // Cleared here, since a quick close and reopen merges into one toggle event that only says open.
            if (filter) {
                const filtered = filter.value !== '';
                filter.value = '';
                applyFilter();
                if (virtual() && filtered) {
                    callDotNet(toggle, 'OnFilter', '');
                }
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
        callDotNet(toggle, 'OnToggle', opened);
        if (!opened) {
            setActive(null);
            position?.dispose();
            position = null;
        }
    };
    const chips = () => [...combobox.querySelectorAll('[data-part="chip"]')];
    // The server finds the chip's option by its id and says the text the chip shows.
    const removeChip = chip => callDotNet(toggle, 'OnRemoveChip', chip.dataset.option, chip.textContent.trim());
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
                if (virtual() && (key === 'Home' || key === 'End')) {
                    open();
                    moveVirtual(key);
                } else {
                    open(key === 'Home' ? options[0] : key === 'End' ? options.at(-1) : null);
                }
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

        if (virtual() && ['ArrowDown', 'ArrowUp', 'Home', 'End', 'PageDown', 'PageUp'].includes(key) && !event.altKey) {
            event.preventDefault();
            moveVirtual(key);
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
        if (virtual()) {
            const ticket = ++reaching;
            callDotNet(toggle, 'OnFilter', filter.value.trim())
                .then(() => ticket === reaching && reachThen(0, 1, setActive));
        }

        setActive(enabled()[0]);
    };
    // The list keys work as on the combobox; Home, End and the other editing keys stay with the text box.
    const onFilterKeyDown = event => {
        if (['ArrowDown', 'ArrowUp', 'PageDown', 'PageUp', 'Enter', 'Escape', 'Tab'].includes(event.key)
            && !isComposing(event)) {
            onKeyDown(event);
        }
    };
    // Options can arrive after the user has typed, such as when they load as the list opens.
    const observer = filter ? new MutationObserver(applyFilter) : null;
    observer?.observe(listbox, { childList: true, subtree: true });
    // A virtualized list draws its options again as it measures and scrolls, so the active one is found by its id.
    const redrawn = new MutationObserver(() => {
        const again = active && !active.isConnected ? document.getElementById(active.id) : null;
        if (again) {
            active = again;
            again.setAttribute('data-active', '');
        }
    });
    redrawn.observe(listbox, { childList: true, subtree: true });
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
    // select-early-click.js marks a select that was clicked, or got an opening key, before this script attached.
    if ('openWhenReady' in combobox.dataset) {
        delete combobox.dataset.openWhenReady;
        combobox.focus();
        open();
    }

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
            redrawn.disconnect();
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
