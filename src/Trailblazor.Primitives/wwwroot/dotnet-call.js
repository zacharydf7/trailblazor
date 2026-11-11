// Calls a .NET method from a script. Once the circuit is gone, as when the server has dropped it, Blazor rejects every
// call, so the call is dropped quietly instead of becoming an error in the page; any other failure still shows.
export function callDotNet(target, method, ...args) {
    return target.invokeMethodAsync(method, ...args).catch(error => {
        if (!circuitGone(error)) {
            throw error;
        }
    });
}

const circuitGone = error =>
    /The circuit associated with this dispatcher is no longer available/.test(error?.message ?? '');
