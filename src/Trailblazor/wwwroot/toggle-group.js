import { createRovingFocus } from '../Trailblazor.Primitives/roving-focus.js';

// The group is one tab stop, starting on the first pressed item; the arrow keys move between its items.
export function attachToggleGroup(group) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!group) {
        return null;
    }

    // Inside a TbToolbar the toolbar's keys move between every control, the group's items among them; inside a popup
    // in the toolbar, or another library's toolbar, the group keeps its own.
    if (group.parentElement?.closest('.tb-toolbar, [popover]')?.classList.contains('tb-toolbar')) {
        group.dataset.ready = '';
        return { dispose() {} };
    }

    const focus = createRovingFocus(group, {
        items: '[data-part="item"]',
        vertical: () => group.classList.contains('tb-toggle-group--vertical'),
        preferred: () => group.querySelector('[data-part="item"][aria-pressed="true"]'),
    });
    // Marks that the keys work, which tests wait for.
    group.dataset.ready = '';
    return focus;
}
