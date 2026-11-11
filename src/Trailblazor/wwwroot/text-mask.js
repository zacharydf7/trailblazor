// Fits a field's text to its mask as it is typed, the way TextMask.cs fits it on the server: 0 takes a digit, a a
// letter and * either, \ makes the next character literal, and every other character is shown as it is.
const tokens = { '0': /^[0-9]$/, 'a': /^\p{L}$/u, '*': /^[\p{L}\p{Nd}]$/u };
// A mask swapped for another can be disposed after its replacement attaches, so only the input's current mask
// clears its data-ready.
const current = new WeakMap();

const parse = pattern => {
    const parts = [];
    for (let index = 0; index < pattern.length; index++) {
        const character = pattern[index];
        if (character === '\\' && index + 1 < pattern.length) {
            parts.push({ literal: pattern[++index] });
        } else {
            parts.push(tokens[character] ? { token: tokens[character] } : { literal: character });
        }
    }

    return parts;
};

// Each slot takes the next character that fits it, and literals show once a slot after them is filled, or the last
// slot is. Text passes over as much of a run of literals as it starts with, so "+447700" skips the "+44" but a typed 4
// still fills the slot after a literal "+44 ". It says where each slot's character came from and went, for the caret.
const fit = (parts, text, hasLiterals) => {
    let shown = '';
    let pending = '';
    let at = 0;
    const slots = [];
    for (let index = 0; index < parts.length; index++) {
        const part = parts[index];
        if (part.literal !== undefined) {
            let run = '';
            while (index < parts.length && parts[index].literal !== undefined) {
                run += parts[index++].literal;
            }

            index--;
            pending += run;
            for (let matched = 0; hasLiterals && matched < run.length && text[at] === run[matched]; matched++) {
                at++;
            }

            continue;
        }

        while (at < text.length && !part.token.test(text[at])) {
            at++;
        }

        if (at === text.length) {
            break;
        }

        shown += pending;
        pending = '';
        slots.push({ from: at, to: shown.length });
        shown += text[at++];
    }

    if (slots.length === parts.filter(part => part.token).length) {
        shown += pending;
    }

    return { shown, slots };
};

export function createMask(input, pattern) {
    const parts = parse(pattern);
    let before = '';
    const onBeforeInput = () => {
        before = input.value;
    };
    // Runs before Blazor hears the input, so the value it reads is already fitted.
    const onInput = event => {
        const caret = input.selectionEnd ?? input.value.length;
        let { shown, slots } = fit(parts, input.value, true);
        let filled = fit(parts, input.value.slice(0, caret), true).slots.length;
        // Backspace after a literal only removes the literal, which fitting puts back, so it takes the slot before it.
        if (event.inputType === 'deleteContentBackward' && shown === before && filled > 0) {
            const kept = slots.map(slot => input.value[slot.from]);
            kept.splice(filled - 1, 1);
            ({ shown, slots } = fit(parts, kept.join(''), false));
            filled--;
        }

        if (shown !== input.value) {
            input.value = shown;
            const place = filled === 0 ? 0 : slots[filled - 1].to + 1;
            input.setSelectionRange(place, place);
        }
    };

    // WebKit sends no change for text a script rewrote, so leaving the field sends one when the text changed.
    let focused = '';
    let changed = false;
    const onFocus = () => {
        focused = input.value;
        changed = false;
    };
    const onChange = () => {
        changed = true;
    };
    const onBlur = () => {
        if (!changed && input.value !== focused) {
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }
    };

    input.addEventListener('beforeinput', onBeforeInput);
    input.addEventListener('input', onInput);
    input.addEventListener('focus', onFocus);
    input.addEventListener('change', onChange);
    input.addEventListener('blur', onBlur);
    input.dataset.ready = '';
    const mask = {
        dispose() {
            if (current.get(input) === mask) {
                current.delete(input);
                delete input.dataset.ready;
            }

            input.removeEventListener('beforeinput', onBeforeInput);
            input.removeEventListener('input', onInput);
            input.removeEventListener('focus', onFocus);
            input.removeEventListener('change', onChange);
            input.removeEventListener('blur', onBlur);
        },
    };
    current.set(input, mask);
    return mask;
}
