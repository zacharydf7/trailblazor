// Safari ends a composition before the keydown of the Enter that commits it, marking that key only with keyCode 229.
export const isComposing = event => event.isComposing || event.keyCode === 229;
