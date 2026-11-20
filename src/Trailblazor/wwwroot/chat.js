import { getTabbables } from '../Trailblazor.Primitives/focus-scope.js';

// A chat's feed keys (WAI-ARIA feed pattern), Enter to send, and the view that stays on the newest message while the
// user is there.
export function createChat(root) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    const feed = root.querySelector(':scope > [data-part=messages]');
    const newest = root.querySelector(':scope > [data-part=newest]');
    const messages = () => [...feed.querySelectorAll(':scope > [data-part=message]')];

    // One message is the feed's tab stop: the one focus was last on, or else the newest, whichever that is now.
    let focused = null;
    const holdTabStop = () => {
        const all = messages();
        const stop = all.includes(focused) ? focused : all.at(-1) ?? null;
        for (const message of all) {
            const wanted = message === stop ? 0 : -1;
            if (message.tabIndex !== wanted) {
                message.tabIndex = wanted;
            }
        }
    };

    // The user is at the newest message while all of it is in view; a pixel to spare covers zoom's fractions.
    let away = false;
    const atBottom = () => {
        const last = messages().at(-1);
        return !last || last.getBoundingClientRect().bottom <= feed.getBoundingClientRect().bottom + 1;
    };
    const showNewest = () => {
        root.toggleAttribute('data-away', away);
    };
    const toBottom = () => {
        feed.scrollTop = feed.scrollHeight;
    };
    const onScroll = () => {
        away = !atBottom();
        showNewest();
    };
    // A new message or a streamed reply growing keeps the newest message in view while the user is there.
    const renders = new MutationObserver(() => {
        holdTabStop();
        if (!away) {
            toBottom();
        }
    });

    const onKeyDown = event => {
        if (event.defaultPrevented) {
            return;
        }

        // Enter sends and Shift+Enter starts a new line; the Enter that ends an input method's composition does
        // neither.
        if (event.target === root.querySelector(':scope > [data-part=composer] [data-part=input]')) {
            if (event.key === 'Enter' && !event.shiftKey && !event.isComposing && event.keyCode !== 229) {
                event.preventDefault();
                event.target.form?.requestSubmit();
            }

            return;
        }

        const message = event.target.closest?.('[data-part=message]');
        if (!message || !feed.contains(message)) {
            return;
        }

        if (event.ctrlKey && (event.key === 'End' || event.key === 'Home')) {
            event.preventDefault();
            leave(event.key === 'End');
        } else if (!event.ctrlKey && !event.altKey && !event.metaKey
            && (event.key === 'PageDown' || event.key === 'PageUp')) {
            event.preventDefault();
            const all = messages();
            const next = all[all.indexOf(message) + (event.key === 'PageDown' ? 1 : -1)];
            next?.focus();
        }
    };
    // Ctrl+End goes to the first control after the feed, such as the message box, and Ctrl+Home to the last one before.
    // A key can come before the scroll event of the move it follows, so the newest-messages button is settled first.
    const leave = forward => {
        onScroll();
        const outside = getTabbables(feed.ownerDocument.body).filter(element => !feed.contains(element));
        const following = element => feed.compareDocumentPosition(element) & Node.DOCUMENT_POSITION_FOLLOWING;
        const target = forward ? outside.find(following) : outside.filter(element => !following(element)).at(-1);
        target?.focus();
    };
    const onFocusIn = event => {
        const message = event.target.closest?.('[data-part=message]');
        if (message && feed.contains(message)) {
            focused = message;
            holdTabStop();
        }
    };
    const onNewest = () => {
        away = false;
        toBottom();
        showNewest();
    };

    root.addEventListener('keydown', onKeyDown);
    feed.addEventListener('focusin', onFocusIn);
    feed.addEventListener('scroll', onScroll, { passive: true });
    newest.addEventListener('click', onNewest);
    renders.observe(feed, { childList: true, subtree: true, characterData: true });
    holdTabStop();
    toBottom();
    showNewest();

    return {
        dispose() {
            renders.disconnect();
            root.removeEventListener('keydown', onKeyDown);
            feed.removeEventListener('focusin', onFocusIn);
            feed.removeEventListener('scroll', onScroll);
            newest.removeEventListener('click', onNewest);
        },
    };
}
