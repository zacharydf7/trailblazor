import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { firstTabbable } from '../Trailblazor.Primitives/focus-scope.js';

// While editing, Enter saves and Escape cancels. Both are kept from the page, so a form around the editor doesn't
// submit and a dialog around it doesn't close.
export function attachInPlaceEditor(root, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    const onKeyDown = event => {
        if (!event.target.closest('[data-part="editor"]') || event.defaultPrevented || isComposing(event)) {
            return;
        }

        if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            callDotNet(events, 'OnCancel');
        } else if (event.key === 'Enter' && !event.shiftKey && !event.altKey && !event.ctrlKey && !event.metaKey
            && !event.target.matches('textarea, button, [contenteditable="true"]')) {
            event.preventDefault();
            // A field commits its text on change, which Enter would have raised, so raise it for the draft to hold it.
            if (event.target.matches('input, select')) {
                event.target.dispatchEvent(new Event('change', { bubbles: true }));
            }

            callDotNet(events, 'OnSave');
        }
    };
    root.addEventListener('keydown', onKeyDown);
    root.dataset.ready = '';

    return {
        // The editor's first control takes focus, so typing starts at once.
        focusEditor() {
            const editor = root.querySelector('[data-part="editor"]');
            const first = editor && firstTabbable(editor);
            first?.focus();
            first?.select?.();
        },
        dispose() {
            root.removeEventListener('keydown', onKeyDown);
        },
    };
}
