import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { pointerMoved } from '../Trailblazor.Primitives/resting-pointer.js';

// The longest mention text looked back over for a trigger.
export const longestMention = 50;

// The suggestions for a mention being typed, which the mention input and the rich text editor both show: a trigger
// before the caret asks the server to search, the list opens under the trigger once suggestions land, and the keys
// move an active one and choose it. Focus never leaves the owner, which carries aria-activedescendant.
// The host gives find(), which returns the mention being typed as { trigger, text, place } or null, where place is
// the host's own mark for where the trigger is, which this code only compares, and may add kind, the kind of block
// the trigger is in, which goes to the server with the search; placeCaret(found), which moves the
// caret mark to the trigger; insert(found, option), which puts the chosen suggestion in; and onToggle(open), if it
// has a state to keep. It calls update(textChanged) when its text or caret changes, and onKeyDown(event) for each
// key.
export function createMentionSuggestions(owner, listbox, caret, events, host) {
    let query = null;
    // Each query is numbered, and the server marks its suggestions with the number of the query they answer, so only
    // the latest query's suggestions can be chosen; until they land, none from the list the new text replaced can be.
    // A script attached again, when a field swaps its input for a text area, counts on from the server's number.
    let asked = Number(listbox.dataset.resultsFor) || 0;
    let askedQuery = false;
    let waiting = false;
    // Enter or Tab pressed before the latest suggestions land chooses the first, unless the query or the focus changes.
    let chooseOnArrival = false;
    let dismissed = null;
    let active = null;
    let position = null;

    // While a search is on its way the list says so with aria-busy, since what it shows is about to change.
    const setWaiting = busy => {
        waiting = busy;
        if (busy) {
            listbox.setAttribute('aria-busy', 'true');
        } else {
            listbox.removeAttribute('aria-busy');
        }
    };
    const isOpen = () => listbox.matches(':popover-open');
    const options = () => [...listbox.querySelectorAll('[role="option"]')];
    const same = (a, b) => a?.place === b?.place && a?.text === b?.text && a?.trigger === b?.trigger
        && a?.kind === b?.kind;

    const setActive = option => {
        active?.removeAttribute('data-active');
        active = option ?? null;
        if (active) {
            active.setAttribute('data-active', '');
            owner.setAttribute('aria-activedescendant', active.id);
            active.scrollIntoView({ block: 'nearest' });
        } else {
            owner.removeAttribute('aria-activedescendant');
        }
    };
    const open = () => {
        if (!isOpen()) {
            listbox.showPopover();
            position = createAnchoredPosition(listbox, caret, { side: 'Bottom', align: 'Start', offset: 4 });
            host.onToggle?.(true);
        }

        setActive(options()[0]);
    };
    const close = () => {
        if (isOpen()) {
            listbox.hidePopover();
            position?.dispose();
            position = null;
            host.onToggle?.(false);
        }

        setActive(null);
    };
    const move = step => {
        const list = options();
        const index = list.indexOf(active);
        setActive(list[(index + step + list.length) % list.length]);
    };

    // A change to the text lets a dismissed trigger suggest again; a move of the caret alone doesn't.
    const update = textChanged => {
        if (textChanged) {
            dismissed = null;
        }

        const next = host.find();
        if (same(next, query)) {
            return;
        }

        query = next && next.place !== dismissed ? next : null;
        chooseOnArrival = false;
        if (query) {
            // The mark sits where the trigger is, so the list opens under it, as it would under a word being typed.
            host.placeCaret(query);
            askedQuery = true;
            setWaiting(true);
            callDotNet(events, 'OnQuery', query.trigger, query.text, ++asked, query.kind ?? null);
        } else {
            close();
            if (askedQuery) {
                askedQuery = false;
                callDotNet(events, 'OnQuery', null, '', ++asked, null);
            }
        }
    };

    // The server is told first, while it still has the suggestions, and the host then puts the choice in.
    const choose = option => {
        const index = options().indexOf(option);
        if (!query || index < 0 || waiting) {
            return;
        }

        callDotNet(events, 'OnChosen', index, asked);
        const chosen = query;
        query = null;
        askedQuery = false;
        close();
        host.insert(chosen, option);
    };

    // True when the key was the list's.
    const onKeyDown = event => {
        if (!isOpen() || isComposing(event)) {
            return false;
        }

        switch (event.key) {
            case 'ArrowDown':
                move(1);
                break;
            case 'ArrowUp':
                move(-1);
                break;
            case 'Enter':
            case 'Tab':
                if (waiting) {
                    chooseOnArrival = true;
                } else {
                    choose(active);
                }

                break;
            case 'Escape':
                // Escape closes the list until the text changes, without leaving a dialog the owner is in.
                dismissed = query?.place ?? null;
                query = null;
                close();
                break;
            default:
                return false;
        }

        event.preventDefault();
        event.stopPropagation();
        return true;
    };
    const onFocusOut = event => {
        if (!listbox.contains(event.relatedTarget)) {
            chooseOnArrival = false;
            close();
        }
    };
    // Pressing a suggestion would take focus from the owner, which closes the list before the click arrives.
    const onListboxPointerDown = event => event.preventDefault();
    const onListboxClick = event => {
        const option = event.target.closest('[role="option"]');
        if (option) {
            choose(option);
        }
    };
    const onListboxPointerMove = event => {
        const option = event.target.closest('[role="option"]');
        if (option && option !== active && pointerMoved(event)) {
            setActive(option);
        }
    };
    // The latest suggestions have landed when the server marks them with the latest query's number: the list opens
    // with some, and closes without.
    const results = new MutationObserver(() => {
        if (Number(listbox.dataset.resultsFor) !== asked) {
            return;
        }

        setWaiting(false);
        if (query && options().length > 0 && owner.ownerDocument.activeElement === owner) {
            open();
            if (chooseOnArrival) {
                choose(active);
            }
        } else {
            close();
        }
    });

    results.observe(listbox, { attributes: true, attributeFilter: ['data-results-for'] });
    owner.addEventListener('focusout', onFocusOut);
    listbox.addEventListener('pointerdown', onListboxPointerDown);
    listbox.addEventListener('click', onListboxClick);
    listbox.addEventListener('pointermove', onListboxPointerMove);

    return {
        update,
        onKeyDown,
        dispose() {
            close();
            setWaiting(false);
            results.disconnect();
            owner.removeEventListener('focusout', onFocusOut);
            listbox.removeEventListener('pointerdown', onListboxPointerDown);
            listbox.removeEventListener('click', onListboxClick);
            listbox.removeEventListener('pointermove', onListboxPointerMove);
        },
    };
}
