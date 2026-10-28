// Layers stack in creation order; only the topmost one reacts to a dismiss gesture.
const layers = [];

export function createDismissableLayer(element, trigger, dotNetReference) {
    const layer = { element, trigger, dotNetReference, dismissPending: false, dispose };
    if (layers.length === 0) {
        document.addEventListener('keydown', onKeyDown);
        document.addEventListener('pointerdown', onPointerDown, { capture: true });
        document.addEventListener('focusin', onFocusIn, { capture: true });
    }

    layers.push(layer);
    return layer;

    function dispose() {
        layers.splice(layers.indexOf(layer), 1);
        if (layers.length === 0) {
            document.removeEventListener('keydown', onKeyDown);
            document.removeEventListener('pointerdown', onPointerDown, { capture: true });
            document.removeEventListener('focusin', onFocusIn, { capture: true });
        }
    }
}

function onKeyDown(event) {
    if (event.key !== 'Escape' || event.defaultPrevented) {
        return;
    }

    // Stops a native <dialog> underneath from treating the same key press as its own close request.
    event.preventDefault();
    requestDismiss(layers.at(-1), 'EscapeKey');
}

function onPointerDown(event) {
    const layer = layers.at(-1);
    if (!isInside(layer, event.target)) {
        requestDismiss(layer, 'PointerDownOutside');
    }
}

function onFocusIn(event) {
    const layer = layers.at(-1);
    if (!isInside(layer, event.target)) {
        requestDismiss(layer, 'FocusOutside');
    }
}

function isInside(layer, target) {
    return layer.element.contains(target) || (layer.trigger?.contains(target) ?? false);
}

// A pointer press outside also moves focus outside; one gesture must reach .NET as one dismiss request.
function requestDismiss(layer, reason) {
    if (layer.dismissPending) {
        return;
    }

    layer.dismissPending = true;
    layer.dotNetReference.invokeMethodAsync('OnDismissRequested', reason).finally(() => layer.dismissPending = false);
}
