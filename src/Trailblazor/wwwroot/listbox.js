import { createTypeahead } from './typeahead.js';
import { watchForUnnamedOptions } from './unnamed-options.js';
import { filterOptions } from './option-filter.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { isComposing } from '../Trailblazor.Primitives/composing.js';

// How many option ids go to the server in one call, to stay within a circuit's message size limit.
const rangeBatch = 100;

// Keys and the active option stay in the browser; choosing clicks the option, which reaches the server.
export function attachListBox(listbox, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!listbox) {
        return null;
    }

    // Marks that the keys work, which tests wait for.
    listbox.dataset.ready = '';
    const unnamed = watchForUnnamedOptions(listbox);
    // An option the server removes, such as one moved to another list, can't stay the active one, and one it adds is
    // filtered like the rest.
    const removals = new MutationObserver(() => {
        if (active && !listbox.contains(active)) {
            setActive(null);
        }

        applyFilter();
    });
    removals.observe(listbox, { childList: true, subtree: true });
    const label = document.getElementById(listbox.getAttribute('aria-labelledby'));
    let active = null;
    // Where a range from a Shift+click starts.
    let rangeFrom = null;
    // A press sets the active option before focus arrives, so onFocus keeps it there.
    let pressed = false;
    // What was chosen when focus left, to tell whether code has changed it since.
    let chosenWhenLeft = null;
    // The option last chosen in a single listbox, by a key, the pointer or assistive technology. Until the server's
    // render arrives, aria-selected can be left over from an earlier choice, so only a move onto this option that
    // shows it chosen can skip choosing it.
    let lastChosen = null;
    const typing = createTypeahead();

    const disabled = () => listbox.getAttribute('aria-disabled') === 'true';
    const multiple = () => listbox.getAttribute('aria-multiselectable') === 'true';
    // The keys reach only the options a filter shows.
    const enabled = () => [...listbox.querySelectorAll('[role="option"]:not([aria-disabled="true"]):not([hidden])')];
    const selected = () => listbox.querySelector(
        '[role="option"][aria-selected="true"]:not([aria-disabled="true"]):not([hidden])');
    const isSelected = option => option.getAttribute('aria-selected') === 'true';

    const setActive = option => {
        active?.removeAttribute('data-active');
        active = option ?? null;
        if (active) {
            active.setAttribute('data-active', '');
            listbox.setAttribute('aria-activedescendant', active.id);
            active.scrollIntoView({ block: 'nearest' });
        } else {
            listbox.removeAttribute('aria-activedescendant');
        }
    };
    // A single listbox chooses the option it moves to, as a native list does; Shift extends a multiple one.
    const moveTo = (option, extend) => {
        if (!option) {
            return;
        }

        setActive(option);
        if (lastChosen && !listbox.contains(lastChosen)) {
            lastChosen = null;
        }

        const chosen = isSelected(option) && (lastChosen === null || lastChosen === option);
        if ((!multiple() || extend) && !chosen) {
            option.click();
        }
    };
    const step = by => {
        const options = enabled();
        // An option the server removed is gone from the list, so the move starts over from the top.
        const index = options.indexOf(active);
        return options[Math.min(Math.max(index + by, 0), options.length - 1)];
    };
    // The filter box comes and goes with Filterable, so it's looked up each time; the listbox's parent stays put.
    const body = listbox.parentElement;
    const filterBox = () => body.querySelector(':scope > [data-part="filter"]');
    const query = () => filterBox()?.value.trim() ?? '';
    // Matching works as a select's filter does, and an empty list's message always shows. Once the filter box is
    // removed the pass runs one last time to show what it hid, so a list that never filters keeps its empty groups.
    let filtered = false;
    const applyFilter = () => {
        if (!filtered && !filterBox()) {
            return;
        }

        filtered = !!filterBox();
        filterOptions(listbox, query(), body.querySelector(':scope > [data-part="no-results"]'),
            option => option.dataset.part === 'status' ? false : null);
        if (!filtered) {
            for (const group of listbox.querySelectorAll('[role="group"][hidden]')) {
                group.hidden = false;
            }
        }

        if (active?.hidden) {
            setActive(null);
        }
    };
    const onFilterInput = event => {
        if (event.target === filterBox()) {
            applyFilter();
        }
    };
    // Down Arrow moves from the filter into the list, where the other keys work.
    const onFilterKeyDown = event => {
        if (event.target === filterBox() && event.key === 'ArrowDown' && !event.altKey && !isComposing(event)) {
            event.preventDefault();
            listbox.focus();
        }
    };
    // Adding or removing the filter box filters the options again right away.
    const filterShown = new MutationObserver(applyFilter);
    filterShown.observe(body, { childList: true });

    const rowsShown = () => Math.max(1, Math.floor(listbox.clientHeight / (enabled()[0]?.offsetHeight || 1)));
    // A range reaches the server as one change to the value, not as a click on each option.
    // A range is a Shift+click, or Ctrl+A over the options a filter shows.
    const chooseRange = async (options, all) => {
        const ids = options.map(option => option.id);
        for (let start = 0; start < ids.length; start += rangeBatch) {
            const last = start + rangeBatch >= ids.length;
            await callDotNet(events, 'OnChooseRange', ids.slice(start, start + rangeBatch), last, all);
        }
    };

    // Focus arrives before the click, so the option under the pointer becomes active first and focus doesn't
    // scroll the list away from it.
    const onPointerDown = event => {
        const option = event.target.closest('[role="option"]:not([aria-disabled="true"])');
        if (option && !disabled()) {
            pressed = true;
            rangeFrom = active;
            setActive(option);
        }
    };
    // Runs first, so the option's own click doesn't toggle it as well.
    const onClick = event => {
        const option = event.target.closest('[role="option"]:not([aria-disabled="true"])');
        if (option && !multiple() && !disabled()) {
            lastChosen = option;
        }

        if (option && event.shiftKey && multiple() && !disabled()) {
            event.stopPropagation();
            const options = enabled();
            const to = options.indexOf(option);
            const from = rangeFrom ? options.indexOf(rangeFrom) : -1;
            chooseRange(options.slice(Math.min(from < 0 ? to : from, to), Math.max(from, to) + 1), false);
        }
    };
    const onFocus = () => {
        if (disabled()) {
            return;
        }

        const chosen = selected();
        if (!multiple() && !pressed && chosen && chosen !== chosenWhenLeft) {
            setActive(chosen);
        } else if (!active || !listbox.contains(active)) {
            setActive(selected() ?? enabled()[0]);
        }
    };
    // Blur ends a press, but one that became a scroll never took focus, so pointercancel has to end it too.
    const forgetPress = () => {
        pressed = false;
    };
    const onBlur = () => {
        forgetPress();
        chosenWhenLeft = selected();
    };
    const onKeyDown = event => {
        const key = event.key;
        if (multiple() && !disabled() && (event.ctrlKey || event.metaKey) && !event.altKey && !event.shiftKey
            && key.toLowerCase() === 'a') {
            event.preventDefault();
            // While a filter narrows the list, only the options it shows are chosen.
            if (query()) {
                chooseRange(enabled(), true);
            } else {
                callDotNet(events, 'OnChooseAll');
            }

            return;
        }

        if (disabled() || event.ctrlKey || event.metaKey || event.altKey) {
            return;
        }

        switch (key) {
            case 'ArrowDown':
                moveTo(step(1), event.shiftKey);
                break;
            case 'ArrowUp':
                moveTo(step(-1), event.shiftKey);
                break;
            case 'Home':
                moveTo(enabled()[0]);
                break;
            case 'End':
                moveTo(enabled().at(-1));
                break;
            case 'PageDown':
                moveTo(step(rowsShown()));
                break;
            case 'PageUp':
                moveTo(step(-rowsShown()));
                break;
            case ' ':
                if (typing.typing()) {
                    moveTo(typing.find(enabled(), active, key));
                } else if (active && multiple()) {
                    active.click();
                } else {
                    moveTo(active);
                }

                break;
            default:
                if (key.length !== 1) {
                    return;
                }

                moveTo(typing.find(enabled(), active ?? selected(), key));
                break;
        }

        event.preventDefault();
    };
    // The label names the listbox without a for attribute, so a click on it doesn't focus the listbox by itself.
    const onLabelClick = () => {
        if (!disabled()) {
            listbox.focus();
        }
    };

    listbox.addEventListener('pointerdown', onPointerDown);
    listbox.addEventListener('click', onClick, { capture: true });
    listbox.addEventListener('focus', onFocus);
    listbox.addEventListener('blur', onBlur);
    listbox.addEventListener('pointercancel', forgetPress);
    listbox.addEventListener('keydown', onKeyDown);
    label?.addEventListener('click', onLabelClick);
    body.addEventListener('input', onFilterInput);
    body.addEventListener('keydown', onFilterKeyDown);
    // Text may have been typed into the filter before the script attached.
    applyFilter();
    return {
        dispose() {
            unnamed.disconnect();
            removals.disconnect();
            listbox.removeEventListener('pointerdown', onPointerDown);
            listbox.removeEventListener('click', onClick, { capture: true });
            listbox.removeEventListener('focus', onFocus);
            listbox.removeEventListener('blur', onBlur);
            listbox.removeEventListener('pointercancel', forgetPress);
            listbox.removeEventListener('keydown', onKeyDown);
            label?.removeEventListener('click', onLabelClick);
            filterShown.disconnect();
            body.removeEventListener('input', onFilterInput);
            body.removeEventListener('keydown', onFilterKeyDown);
        },
    };
}
