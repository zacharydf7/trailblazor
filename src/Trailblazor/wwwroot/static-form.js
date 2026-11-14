import { focusFirstError } from './form.js';

// On a static page there's no circuit to set the submit button's Loading, so a native post marks it busy here, and a
// second submit is dropped while the first is on its way. Blazor handles the posts it enhances.
const marked = new Set();
let clearing = null;

function clearMarks() {
    clearTimeout(clearing);
    for (const element of marked) {
        delete element.dataset.tbSubmitting;
        if (element.matches('button')) {
            element.removeAttribute('aria-busy');
            element.removeAttribute('aria-disabled');
            element.querySelector(':scope > [data-part="spinner"][data-tb-added]')?.remove();
        }
    }

    marked.clear();
}

function mark(form, button) {
    form.dataset.tbSubmitting = '';
    marked.add(form);
    if (button?.matches('.tb-button') && button.getAttribute('aria-busy') !== 'true'
        && button.getAttribute('aria-disabled') !== 'true') {
        button.dataset.tbSubmitting = '';
        button.setAttribute('aria-busy', 'true');
        button.setAttribute('aria-disabled', 'true');
        const spinner = document.createElement('span');
        spinner.dataset.part = 'spinner';
        spinner.dataset.tbAdded = '';
        spinner.className = 'tb-button__spinner';
        button.prepend(spinner);
        marked.add(button);
    }

    // A post that leaves the page in place, such as one the user stops or one answered with a download, clears its
    // marks after 10 seconds.
    clearTimeout(clearing);
    clearing = setTimeout(clearMarks, 10000);
}

document.addEventListener('submit', event => {
    const form = event.target;
    if (event.defaultPrevented || !form.matches?.('form.tb-form[data-tb-static]')) {
        return;
    }

    if ('tbSubmitting' in form.dataset) {
        event.preventDefault();
        return;
    }

    // A post into another window leaves this page as it is. The form's own attribute is read, since a field named
    // target would stand in for form.target.
    const target = event.submitter?.formTarget || form.getAttribute('target');
    if (target && target !== '_self') {
        return;
    }

    // Marked once the event is over, so a listener after this one can still cancel the post.
    setTimeout(() => {
        if (!event.defaultPrevented) {
            mark(form, event.submitter);
        }
    });
});

// A page the browser brings back from its cache keeps the marks of the post that left it.
window.addEventListener('pageshow', event => {
    if (event.persisted) {
        clearMarks();
    }
});

// A post that fails validation renders the form with data-tb-focus-error, so its first error takes focus as the page
// loads, and again after each enhanced navigation, which calls this. The form's id is read as its attribute, since a
// field named id would stand in for form.id.
export function focusMarkedForms() {
    for (const form of document.querySelectorAll('form[data-tb-focus-error]')) {
        form.removeAttribute('data-tb-focus-error');
        focusFirstError(form.getAttribute('id'));
    }
}

focusMarkedForms();
