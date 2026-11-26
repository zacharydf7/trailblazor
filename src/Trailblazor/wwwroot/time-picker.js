import { createAutocomplete } from './autocomplete.js';

// The list of times works as an autocomplete's list does from the box's keys. The clock button opens and closes it
// for a pointer, and keeps focus in the box.
export function createTimePicker(input, listbox, events) {
    const list = createAutocomplete(input, listbox, events);
    if (!list) {
        return null;
    }

    const control = input.closest('[data-part="control"]') ?? input.parentElement;
    // A picked time goes in the box before the server hears of it: when it is the time already chosen, the server
    // draws nothing new, and the box would keep what was typed.
    const onListboxClick = event => {
        const option = event.target.closest('[role="option"]:not([aria-disabled="true"])');
        if (option) {
            input.value = option.textContent.trim();
        }
    };
    const onPointerDown = event => {
        if (event.target.closest('[data-part="toggle"]')) {
            event.preventDefault();
        }
    };
    const onClick = event => {
        if (!event.target.closest('[data-part="toggle"]')) {
            return;
        }

        input.focus();
        if (list.setOpen(!listbox.matches(':popover-open'))) {
            listbox.querySelector('[data-start]')?.scrollIntoView({ block: 'nearest' });
        }
    };
    control.addEventListener('pointerdown', onPointerDown);
    control.addEventListener('click', onClick);
    listbox.addEventListener('click', onListboxClick);

    return {
        dispose() {
            list.dispose();
            control.removeEventListener('pointerdown', onPointerDown);
            control.removeEventListener('click', onClick);
            listbox.removeEventListener('click', onListboxClick);
        },
    };
}
