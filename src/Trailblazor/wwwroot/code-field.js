// Drop characters the code doesn't take, such as the dash in "123-456", before maxlength cuts the code short.
export function attachCodeField(input) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!input) {
        return null;
    }

    const keep = text => (text.match(input.dataset.characters === 'digits' ? /[0-9]/g : /[0-9a-z]/gi) ?? []).join('');
    const onBeforeInput = event => {
        if (event.inputType === 'insertText' && event.data && keep(event.data) !== event.data) {
            event.preventDefault();
        }
    };
    const onPaste = event => {
        const pasted = event.clipboardData?.getData('text') ?? '';
        if (input.readOnly || input.disabled || keep(pasted) === pasted) {
            return;
        }

        event.preventDefault();
        const start = input.selectionStart ?? input.value.length;
        const end = input.selectionEnd ?? start;
        const room = input.maxLength - (input.value.length - (end - start));
        input.setRangeText(keep(pasted).slice(0, Math.max(room, 0)), start, end, 'end');
        input.dispatchEvent(new Event('input', { bubbles: true }));
    };

    input.addEventListener('beforeinput', onBeforeInput);
    input.addEventListener('paste', onPaste);
    input.dataset.ready = '';
    return {
        dispose() {
            input.removeEventListener('beforeinput', onBeforeInput);
            input.removeEventListener('paste', onPaste);
        },
    };
}
