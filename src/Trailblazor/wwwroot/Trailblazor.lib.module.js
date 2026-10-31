// Blazor runs this initializer itself, so consumers add no script tags.
import './pointer-origin.js';
import './read-only.js';

// Blazor Web Apps call afterWebStarted; standalone WebAssembly and older hosts call afterStarted.
export function afterWebStarted(blazor) {
    registerEvents(blazor);
    // An enhanced navigation copies the server's <html>, which has no theme, so the one the page showed goes back.
    blazor.addEventListener('enhancedload', () => {
        if (globalThis.trailblazorTheme) {
            Object.assign(document.documentElement.dataset, globalThis.trailblazorTheme);
        }
    });
}

export function afterStarted(blazor) {
    registerEvents(blazor);
}

let registered = false;

function registerEvents(blazor) {
    if (registered) {
        return;
    }

    registered = true;

    // A number input reports text it can't parse as an empty value; badInput tells that apart from a cleared field.
    // Going to or from such text in an empty field fires no change event, so leaving the field reports too.
    const numberEventArgs = event => ({ value: event.target.value, badInput: event.target.validity.badInput });
    blazor.registerCustomEventType('tbnumberchange', { browserEventName: 'change', createEventArgs: numberEventArgs });
    blazor.registerCustomEventType('tbnumberleave', { browserEventName: 'focusout', createEventArgs: numberEventArgs });
}
