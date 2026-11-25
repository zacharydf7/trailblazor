// Whether a press landed on a link, field, label or other control the app put in a field's start or end slot, which
// keeps its own clicks rather than opening the field's list.
export const inAppControl = target => {
    const slot = target.closest?.('[data-part="start"], [data-part="end"]');
    const pressed = target.closest?.('a[href], input, select, textarea, label, summary, [tabindex], [contenteditable]');
    return slot != null && pressed != null && slot.contains(pressed);
};
