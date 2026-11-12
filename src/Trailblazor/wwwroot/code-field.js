// Any decimal digit becomes its ASCII digit, as TbCodeField does. Each run of digits in Unicode counts 0 to 9 in order,
// so a digit's value is its distance from the start of its run, modulo 10.
const isDigit = character => /\p{Nd}/u.test(character);
const asciiDigits = text => text.replace(/\p{Nd}/gu, digit => {
    const point = digit.codePointAt(0);
    let start = point;
    while (isDigit(String.fromCodePoint(start - 1))) {
        start--;
    }

    return String((point - start) % 10);
});

// Drop characters the code doesn't take, such as the dash in "123-456", before maxlength cuts the code short.
export function attachCodeField(input) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!input) {
        return null;
    }

    const keep = text => (asciiDigits(text).match(input.dataset.characters === 'digits' ? /[0-9]/g : /[0-9a-z]/gi)
        ?? []).join('');
    const insert = text => {
        const start = input.selectionStart ?? input.value.length;
        const end = input.selectionEnd ?? start;
        const room = input.maxLength - (input.value.length - (end - start));
        input.setRangeText(keep(text).slice(0, Math.max(room, 0)), start, end, 'end');
        input.dispatchEvent(new Event('input', { bubbles: true }));
    };
    const onBeforeInput = event => {
        if (event.inputType === 'insertText' && event.data && keep(event.data) !== event.data) {
            event.preventDefault();
            if (keep(event.data)) {
                insert(event.data);
            }
        }
    };
    const onPaste = event => {
        const pasted = event.clipboardData?.getData('text') ?? '';
        if (input.readOnly || input.disabled || keep(pasted) === pasted) {
            return;
        }

        event.preventDefault();
        insert(pasted);
    };
    // Text from an input method can't be canceled in beforeinput, so the value is cleaned once composing ends.
    const onCompositionEnd = () => {
        const kept = keep(input.value).slice(0, input.maxLength);
        if (kept !== input.value) {
            input.value = kept;
            input.dispatchEvent(new Event('input', { bubbles: true }));
        }
    };

    input.addEventListener('beforeinput', onBeforeInput);
    input.addEventListener('paste', onPaste);
    input.addEventListener('compositionend', onCompositionEnd);
    input.dataset.ready = '';
    return {
        dispose() {
            input.removeEventListener('beforeinput', onBeforeInput);
            input.removeEventListener('paste', onPaste);
            input.removeEventListener('compositionend', onCompositionEnd);
        },
    };
}
