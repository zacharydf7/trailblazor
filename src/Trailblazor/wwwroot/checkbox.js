// A checkbox's mixed state is a DOM property with no HTML attribute, so it can only be set from script.
// The input is null when the checkbox was removed before this call arrived.
export function setIndeterminate(input, indeterminate) {
    if (input) {
        input.indeterminate = indeterminate;
    }
}
