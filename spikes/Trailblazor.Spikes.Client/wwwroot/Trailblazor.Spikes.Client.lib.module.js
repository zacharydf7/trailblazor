// Prototype of the library initializer's first job: tell .NET when a popover opens or closes, even by light dismiss.
export function afterWebStarted(blazor) {
    blazor.registerCustomEventType('popovertoggle', {
        browserEventName: 'toggle',
        createEventArgs: event => ({ newState: event.newState }),
    });
}
