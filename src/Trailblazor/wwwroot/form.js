import { focusCheckbox, focusRadio } from './field.js';

// A radio or checkbox group marks its fieldset invalid, which can't take focus, so the input its FocusAsync picks does.
export function focusFirstError(id) {
    const field = document.getElementById(id)?.querySelector('[aria-invalid="true"]');
    if (!field?.matches('fieldset')) {
        field?.focus();
    } else if (field.querySelector('input[type=radio]')) {
        focusRadio(field, false);
    } else {
        focusCheckbox(field, false);
    }
}
