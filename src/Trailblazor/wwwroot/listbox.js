import { createTypeahead } from './typeahead.js';
import { watchForUnnamedOptions } from './unnamed-options.js';

// Keys and the active option stay in the browser; choosing clicks the option, which reaches the server.
export function attachListBox(listbox) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!listbox) {
        return null;
    }

    // Marks that the keys work, which tests wait for.
    listbox.dataset.ready = '';
    const unnamed = watchForUnnamedOptions(listbox);
    const label = document.getElementById(listbox.getAttribute('aria-labelledby'));
    let active = null;
    const typing = createTypeahead();

    const disabled = () => listbox.getAttribute('aria-disabled') === 'true';
    const multiple = () => listbox.getAttribute('aria-multiselectable') === 'true';
    const enabled = () => [...listbox.querySelectorAll('[role="option"]:not([aria-disabled="true"])')];
    const selected = () => listbox.querySelector('[role="option"][aria-selected="true"]:not([aria-disabled="true"])');
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
        if ((!multiple() || extend) && !isSelected(option)) {
            option.click();
        }
    };
    const step = by => {
        const options = enabled();
        // An option the server removed is gone from the list, so the move starts over from the top.
        const index = options.indexOf(active);
        return options[Math.min(Math.max(index + by, 0), options.length - 1)];
    };

    // Focus arrives before the click, so the option under the pointer becomes active first and focus doesn't
    // scroll the list away from it.
    const onPointerDown = event => {
        const option = event.target.closest('[role="option"]:not([aria-disabled="true"])');
        if (option && !disabled()) {
            setActive(option);
        }
    };
    const onFocus = () => {
        if (!disabled() && (!active || !listbox.contains(active))) {
            setActive(selected() ?? enabled()[0]);
        }
    };
    const onKeyDown = event => {
        const key = event.key;
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
    listbox.addEventListener('focus', onFocus);
    listbox.addEventListener('keydown', onKeyDown);
    label?.addEventListener('click', onLabelClick);
    return {
        dispose() {
            unnamed.disconnect();
            listbox.removeEventListener('pointerdown', onPointerDown);
            listbox.removeEventListener('focus', onFocus);
            listbox.removeEventListener('keydown', onKeyDown);
            label?.removeEventListener('click', onLabelClick);
        },
    };
}
