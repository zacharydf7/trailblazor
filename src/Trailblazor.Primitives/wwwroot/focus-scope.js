const focusableSelector = 'a[href], area[href], button, input:not([type="hidden"]), select, textarea, iframe, '
    + 'audio[controls], video[controls], [contenteditable]:not([contenteditable="false"]), [tabindex]';

// Only the most recently created trapped scope traps; the ones below it resume when it is disposed.
const trappedScopes = [];

export function createFocusScope(container, options) {
    const previouslyFocused = document.activeElement;
    let lastFocusedInside = null;
    const scope = { dispose };

    if (options.trapped) {
        trappedScopes.push(scope);
    }

    document.addEventListener('keydown', onKeyDown);
    document.addEventListener('focusin', onFocusIn);

    if (options.autoFocus && !container.contains(document.activeElement)) {
        (getTabbables(container)[0] ?? container).focus();
    }

    return scope;

    function isActiveTrap() {
        return trappedScopes.at(-1) === scope;
    }

    function onKeyDown(event) {
        if (event.key !== 'Tab' || !isActiveTrap()) {
            return;
        }

        const tabbables = getTabbables(container);
        if (tabbables.length === 0) {
            event.preventDefault();
            return;
        }

        const first = tabbables[0];
        const last = tabbables.at(-1);
        const focusIsOutside = !container.contains(document.activeElement);
        if (event.shiftKey && (focusIsOutside || document.activeElement === first)) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && (focusIsOutside || document.activeElement === last)) {
            event.preventDefault();
            first.focus();
        }
    }

    function onFocusIn(event) {
        if (container.contains(event.target)) {
            lastFocusedInside = event.target;
        } else if (isActiveTrap()) {
            // The last focused element may have been removed by a re-render since it had focus.
            const target = container.contains(lastFocusedInside) ? lastFocusedInside : getTabbables(container)[0];
            (target ?? container).focus();
        }
    }

    function dispose() {
        document.removeEventListener('keydown', onKeyDown);
        document.removeEventListener('focusin', onFocusIn);
        const index = trappedScopes.indexOf(scope);
        if (index !== -1) {
            trappedScopes.splice(index, 1);
        }

        // Don't take focus back if the user has already moved it somewhere else on purpose.
        const focused = document.activeElement;
        const focusIsInsideOrLost = container.contains(focused) || focused === document.body;
        if (options.restoreFocus && focusIsInsideOrLost && previouslyFocused?.isConnected) {
            previouslyFocused.focus();
        }
    }
}

function getTabbables(container) {
    return [...container.querySelectorAll(focusableSelector)].filter(element => element.tabIndex >= 0
        && !element.matches(':disabled') && !element.closest('[inert]') && element.checkVisibility());
}
