export function focusRadio(group, preventScroll) {
    const radio = group.querySelector('input[type=radio]:checked:enabled')
        ?? group.querySelector('input[type=radio]:enabled');
    radio?.focus({ preventScroll });
}

export function focusCheckbox(group, preventScroll) {
    group.querySelector('input[type=checkbox]:enabled')?.focus({ preventScroll });
}

export function selectText(input) {
    input.focus();
    input.select();
}
