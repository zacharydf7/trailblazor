import { focusOrigin } from './pointer-origin.js';

const players = 'audio[controls], video[controls]';
const focusableSelector = 'a[href], area[href], button, input:not([type="hidden"]), select, textarea, iframe, '
    + `${players}, [contenteditable]:not([contenteditable="false"]), [tabindex]`;

// Tab reaches these, but without a tabindex attribute an editable region's tabIndex reads -1, and in WebKit so does a
// player's. Only the outermost editable element is a stop: the browser can't focus one inside it.
function isStopWithoutTabIndex(element) {
    return !element.hasAttribute('tabindex')
        && (element.matches(players) || element.isContentEditable && !element.parentElement?.isContentEditable);
}

// Only the most recently created trapped scope traps; the ones below it resume when it is disposed.
const trappedScopes = [];

export function createFocusScope(container, options) {
    // Safari leaves focus on the page when a button opens the scope, so the button pressed is where focus goes back.
    const previouslyFocused = focusOrigin();
    let lastFocusedInside = null;
    // A temporary stop beside a player whose own controls took the Tab; reaching it wraps to the scope's other end.
    let guard = null;
    let guardWrapsForward = true;
    const scope = { dispose, focusMarkedIfUnmoved };

    if (options.trapped) {
        trappedScopes.push(scope);
    }

    document.addEventListener('keydown', onKeyDown);
    document.addEventListener('focusin', onFocusIn);

    if (options.autoFocus && !container.contains(document.activeElement)) {
        (autofocusTarget(container) ?? getTabbables(container)[0] ?? container).focus();
    }

    const settled = document.activeElement;
    return scope;

    // A field marked to take focus asked before the scope started; it takes focus unless the user has moved it.
    function focusMarkedIfUnmoved() {
        if (document.activeElement === settled) {
            autofocusTarget(container)?.focus();
        }
    }

    function isActiveTrap() {
        return trappedScopes.at(-1) === scope;
    }

    function onKeyDown(event) {
        if (event.key !== 'Tab' || !isActiveTrap()) {
            return;
        }

        removeGuard();
        const tabbables = getTabbables(container);
        if (tabbables.length === 0) {
            event.preventDefault();
            return;
        }

        const first = tabbables[0];
        const last = tabbables.at(-1);
        const focused = document.activeElement;
        const focusIsOutside = !container.contains(focused);
        // Focus stays on a player while Tab moves through its own controls, so the browser takes the key.
        if (!focusIsOutside && focused.matches(players) && focused === (event.shiftKey ? first : last)) {
            placeGuard(focused, !event.shiftKey);
        } else if (event.shiftKey && (focusIsOutside || sameStop(focused, first))) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && (focusIsOutside || sameStop(focused, last))) {
            event.preventDefault();
            first.focus();
        }
    }

    // Moves between a player's own controls send no focus events, so the guard stays until focus goes anywhere else.
    function placeGuard(player, forward) {
        guard = document.createElement('span');
        guard.tabIndex = 0;
        guard.style.position = 'fixed';
        guardWrapsForward = forward;
        if (forward) {
            player.after(guard);
        } else {
            player.before(guard);
        }
    }

    function removeGuard() {
        guard?.remove();
        guard = null;
    }

    function onFocusIn(event) {
        const reachedGuard = guard !== null && event.target === guard;
        removeGuard();
        if (reachedGuard) {
            const tabbables = getTabbables(container);
            (guardWrapsForward ? tabbables[0] : tabbables.at(-1))?.focus();
        } else if (container.contains(event.target)) {
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
        removeGuard();
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

// The control marked autofocus in the container, or null. A field component carries the attribute on its wrapper,
// so a wrapper gives its first control instead.
export function autofocusTarget(container) {
    const named = container?.querySelector('[autofocus], [data-tb-autofocus]');
    return named && markedTarget(named);
}

// A marked group sends focus where Tab would.
export function markedTarget(named) {
    return isTabbable(named) ? named : firstTabbable(named);
}

// The first control Tab would reach in the container, or null.
export function firstTabbable(container) {
    return getTabbables(container)[0] ?? null;
}

// A radio group is one stop: its checked radio, or its first when none is checked.
function getTabbables(container) {
    const tabbables = [...container.querySelectorAll(focusableSelector)].filter(isTabbable);
    return tabbables.filter(element => {
        if (element.type !== 'radio') {
            return true;
        }

        const group = tabbables.filter(other => sameStop(other, element));
        return element === (group.find(other => other.checked) ?? group[0]);
    });
}

// Radios with the same name in the same form take one stop between them.
function sameStop(one, other) {
    return one === other || one.type === 'radio' && other.type === 'radio' && one.name !== ''
        && one.name === other.name && one.form === other.form;
}

function isTabbable(element) {
    return element.matches(focusableSelector) && (element.tabIndex >= 0 || isStopWithoutTabIndex(element))
        && !element.matches(':disabled')
        && !element.closest('[inert]') && element.checkVisibility({ visibilityProperty: true, checkVisibilityCSS: true });
}
