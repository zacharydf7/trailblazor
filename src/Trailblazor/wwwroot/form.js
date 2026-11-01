import { focusCheckbox, focusRadio } from './field.js';

// A field only marked Invalid has no message to fix, so focus goes to the first one an error message describes.
const describedByError = field => (field.getAttribute('aria-describedby') ?? '').split(/\s+/)
    .some(id => document.getElementById(id)?.matches('[data-part="error"]'));

// A radio or checkbox group marks its fieldset invalid, which can't take focus, so the input its FocusAsync picks does.
export function focusFirstError(id) {
    const field = [...(document.getElementById(id)?.querySelectorAll('[aria-invalid="true"]') ?? [])]
        .find(describedByError);
    if (!field?.matches('fieldset')) {
        field?.focus();
    } else if (field.querySelector('input[type=radio]')) {
        focusRadio(field, false);
    } else {
        focusCheckbox(field, false);
    }
}
