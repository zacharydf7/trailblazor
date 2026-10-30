export function focusFirstError(id) {
    document.getElementById(id)?.querySelector('[aria-invalid="true"]')?.focus();
}
