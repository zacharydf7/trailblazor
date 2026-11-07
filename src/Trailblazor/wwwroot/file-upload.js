// A browser would open a file dropped on a disabled or read-only field and leave the page, so the drop is refused.
const refuseDrop = event => {
    // A drag can target the text of the zone's prompt, which isn't an element.
    const element = event.target.closest ? event.target : event.target.parentElement;
    if (element?.closest('[data-part="zone"]')?.querySelector(':scope > input:disabled')) {
        event.preventDefault();
        event.dataTransfer.dropEffect = 'none';
    }
};

document.addEventListener('dragover', refuseDrop);
document.addEventListener('drop', refuseDrop);

// A browser opens a file picker only shortly after a user's click or key press.
// The value is emptied first, so choosing the same file again still reports a change.
export function openPicker(input) {
    if (!input) {
        return;
    }

    input.value = '';
    input.click();
}

// A browser reports no change for the file an input already holds, so a cleared field lets it go.
export function clearPicker(input) {
    if (input) {
        input.value = '';
    }
}
