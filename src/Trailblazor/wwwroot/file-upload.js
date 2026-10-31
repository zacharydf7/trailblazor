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
