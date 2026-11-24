import { focusOrigin } from './pointer-origin.js';

const players = 'audio[controls], video[controls]';
const summary = 'details > summary:first-of-type';
const focusableSelector = 'a[href], area[href], button, input:not([type="hidden"]), select, textarea, iframe, '
    + `${players}, ${summary}, [contenteditable]:not([contenteditable="false"]), [tabindex]`;

// Tab reaches these, but without a tabindex attribute an editable region's tabIndex reads -1, and in WebKit so do a
// player's and a summary's. Only the outermost editable element is a stop: the browser can't focus one inside it.
function isStopWithoutTabIndex(element) {
    return !element.hasAttribute('tabindex') && (element.matches(players) || element.matches(summary)
        || element.isContentEditable && !element.parentElement?.isContentEditable);
}

// Only the scope that started trapping last traps; the ones below it resume when it stops trapping or is disposed.
const trappedScopes = [];

export function createFocusScope(container, options) {
    // Safari leaves focus on the page when a button opens the scope, so the button pressed is where focus goes back.
    let previouslyFocused = focusOrigin(container.ownerDocument);
    // What had the focus as the newest hold on this scope started, and whether that was inside the scope.
    let heldFrom = previouslyFocused;
    let heldInside = container.contains(heldFrom);
    let lastFocusedInside = null;
    // A temporary stop beside a player whose own controls took the Tab; reaching it wraps to the scope's other end.
    let guard = null;
    let guardWrapsForward = true;
    const scope = { dispose, focusMarkedIfUnmoved, setTrapped, noteFocus, holdEnded, container };
    // Asked for each time: the container can move to another window, which has a focused element of its own.
    const focusedNow = () => container.ownerDocument.activeElement;

    setTrapped(options.trapped);
    document.addEventListener('keydown', onKeyDown);
    document.addEventListener('focusin', onFocusIn);

    if (options.autoFocus && !container.contains(focusedNow())) {
        (autofocusTarget(container) ?? getTabbables(container)[0] ?? container).focus();
    }

    if (options.marksReady) {
        container.dataset.ready = '';
    }

    const settled = focusedNow();
    return scope;

    // Starts or stops trapping while the scope runs, and marks the container while it traps. A scope that starts
    // trapping is the newest trap from then on.
    function setTrapped(trapped) {
        container.toggleAttribute('data-trapped', trapped);
        const index = trappedScopes.indexOf(scope);
        if (trapped && index === -1) {
            trappedScopes.push(scope);
        } else if (!trapped && index !== -1) {
            trappedScopes.splice(index, 1);
        }
    }

    // Notes what has focus as a hold starts on a scope that is already there, to give focus back to when the hold
    // ends. A control outside the scope is also where focus goes when the scope does.
    function noteFocus() {
        const page = container.ownerDocument;
        heldFrom = focusOrigin(page);
        heldInside = container.contains(heldFrom);
        if (!heldInside && heldFrom !== page.body) {
            previouslyFocused = heldFrom;
        }
    }

    // A hold has ended and the scope stays. Focus goes back to the control outside the scope that had it as the hold
    // started. When none did, because the hold started from inside the scope or its control went as it started,
    // focus that is still inside stays where it is, and focus that was lost goes to the control inside that started
    // the hold, or else to the scope's first control.
    function holdEnded() {
        const page = container.ownerDocument;
        if (!heldInside && heldFrom !== page.body && heldFrom?.isConnected) {
            giveFocusBack(heldFrom);
        } else if (focusedNow() === page.body) {
            giveFocusBack(heldInside && heldFrom.isConnected ? heldFrom : getTabbables(container)[0] ?? container);
        }
    }

    // Don't take focus back if the user has already moved it somewhere else on purpose.
    function giveFocusBack(target) {
        const focused = focusedNow();
        const focusIsInsideOrLost = container.contains(focused) || focused === container.ownerDocument.body;
        if (options.restoreFocus && focusIsInsideOrLost && target?.isConnected) {
            target.focus();
        }
    }

    // A field marked to take focus asked before the scope started; it takes focus unless the user has moved it.
    function focusMarkedIfUnmoved() {
        if (focusedNow() === settled) {
            autofocusTarget(container)?.focus();
        }
    }

    // The newest trap that can be used is the one at work. A trap stands down while its container is inside
    // something inert, or behind a modal dialog that doesn't hold it: it has nothing Tab could go to, and would
    // only keep the key from what is in front of it.
    function isActiveTrap() {
        return trappedScopes.findLast(each => !each.container.closest('[inert]')
            && ![...each.container.ownerDocument.querySelectorAll('dialog:modal')]
                .some(modal => !modal.contains(each.container))) === scope;
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
        const focused = focusedNow();
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
        setTrapped(false);
        giveFocusBack(previouslyFocused);
    }
}

// The control marked autofocus in the container, or null. A field component carries the attribute on its wrapper,
// so a wrapper gives its first control instead.
export function autofocusTarget(container) {
    const named = container?.querySelector('[autofocus], [data-tb-autofocus]');
    return named && markedTarget(named);
}

// A marked group sends focus where Tab would. A marked control takes it itself, even after a data grid has taken it
// out of the tab order (`data-tb-tab`), which the grid may do before the mark is seen.
export function markedTarget(named) {
    return isTabbable(named) || 'tbTab' in named.dataset ? named : firstTabbable(named);
}

// Whether a control outside a popover has taken focus since it closed. Its toggle event comes a task after it closes,
// so focus goes back to the trigger only when this is false. Focus on the body, anywhere inside the popover, or on what
// had focus as it opened (where the browser puts focus back by itself) doesn't count.
export function focusWentElsewhere(popover, openedFrom) {
    const page = popover.ownerDocument;
    const active = page.activeElement;
    return active !== null && active !== page.body && !popover.contains(active) && active !== openedFrom;
}

// The first control Tab would reach in the container, or null.
export function firstTabbable(container) {
    return getTabbables(container)[0] ?? null;
}

// The controls Tab reaches in the container, in order. A radio group is one stop: its checked radio, or its first when
// none is checked.
export function getTabbables(container) {
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

export function isTabbable(element) {
    return element.matches(focusableSelector) && (element.tabIndex >= 0 || isStopWithoutTabIndex(element))
        && !element.matches(':disabled')
        && !element.closest('[inert]') && element.checkVisibility({ visibilityProperty: true, checkVisibilityCSS: true });
}
