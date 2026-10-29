// Blazor runs this initializer itself, so consumers add no script tags.
import './pointer-origin.js';

// Blazor Web Apps call afterWebStarted; standalone WebAssembly and older hosts call afterStarted.
export function afterWebStarted(blazor) {
    registerEvents(blazor);
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
