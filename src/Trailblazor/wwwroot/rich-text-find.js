import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { isApple } from '../Trailblazor.Primitives/platform.js';

// A rich text editor's find bar. It searches each paragraph's text, marks the matches with the CSS Custom Highlight
// API without changing the value, and replaces through the editor's own edits, so undo takes a replacement back.
// Blazor draws the bar, hidden, with its words in data attributes; its state is all here, so finding makes no round
// trip. scope is the editor's outer element, which the bar and the toolbar are in.
// The host gives texts(), each block's text to search, or null for a block that holds none; ranges(matches), the
// page's range for each match; caret(), where the editor's selection starts; selectedText(), the text selected
// within one block, or ''; focus(match), which focuses the editor with that match selected, or with the selection it
// had; replace(matches, text), which replaces them in one edit, after which the editor calls refresh(); area(), the
// element that scrolls the text; and readOnly().
export function createFind(scope, host, signal) {
    const view = scope.ownerDocument.defaultView;
    const bar = () => scope.querySelector(':scope > [data-part="find"]');
    const part = name => bar().querySelector(`[data-find="${name}"]`);
    const toolbarButton = () => scope.querySelector('[data-command="find"]');
    const pressed = name => part(name).getAttribute('aria-pressed') === 'true';
    // The bar while it shows, and null while it doesn't.
    let shown = null;
    let matches = [];
    let current = -1;
    // The ranges this editor has in the page's highlights, each with its highlight's name.
    let marked = [];
    // Where the search goes on from: the caret as the bar opened, then the current match.
    let from = { block: 0, offset: 0 };

    // Every editor on the page shares one highlight of each name, and adds and removes its own ranges. A browser
    // without highlights still counts, steps and replaces; it marks nothing.
    const highlight = name => {
        if (!view.CSS?.highlights) return null;
        if (!view.CSS.highlights.has(name)) {
            const made = new view.Highlight();
            made.priority = name === 'tb-find-current' ? 1 : 0;
            view.CSS.highlights.set(name, made);
        }
        return view.CSS.highlights.get(name);
    };

    const unmark = () => {
        for (const [name, range] of marked) highlight(name)?.delete(range);
        marked = [];
    };

    // The text is matched as typed, not as a pattern. A whole word has no letter, digit or underscore on either side.
    const pattern = () => {
        const text = part('text').value;
        if (!text) return null;
        const literal = text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        const wordCharacter = '[\\p{L}\\p{N}_]';
        return new RegExp(pressed('word') ? `(?<!${wordCharacter})${literal}(?!${wordCharacter})` : literal,
            pressed('case') ? 'gu' : 'giu');
    };

    const search = () => {
        const sought = pattern();
        if (!sought) return [];
        return host.texts().flatMap((text, block) => text === null ? [] : [...text.matchAll(sought)]
            .map(found => ({ block, start: found.index, end: found.index + found[0].length })));
    };

    const say = text => {
        const count = part('count');
        if (count.textContent !== text) count.textContent = text;
    };

    // The bar's data holds its words, with {0} and {1} where the numbers go.
    const words = (name, ...numbers) => (shown.dataset[name] ?? '').replace(/\{(\d)\}/g, (_, at) => numbers[at]);

    // Scrolls the page, and the editing area when it scrolls itself, so the current match shows.
    const reveal = () => {
        if (!matches.length) return;
        const range = marked[current][1];
        range.startContainer.parentElement?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
        const area = host.area();
        const [box, frame] = [range.getBoundingClientRect(), area.getBoundingClientRect()];
        if (box.top < frame.top) area.scrollTop -= frame.top - box.top;
        else if (box.bottom > frame.bottom) area.scrollTop += box.bottom - frame.bottom;
    };

    // Marks the matches and says where the search is.
    const show = () => {
        unmark();
        marked = host.ranges(matches).map((range, at) => [at === current ? 'tb-find-current' : 'tb-find', range]);
        for (const [name, range] of marked) highlight(name)?.add(range);
        say(!part('text').value ? '' : matches.length ? words('count', current + 1, matches.length) : words('none'));
        for (const name of ['previous', 'next']) part(name).disabled = !matches.length;
        part('with').disabled = host.readOnly();
        for (const name of ['replace', 'all']) part(name).disabled = !matches.length || host.readOnly();
    };

    // Searches again, and makes the current match the first one at or after where the search goes on from. The editor
    // calls this as its text changes, which must not scroll the view away from where the user is typing.
    const refresh = () => {
        if (!shown) return;
        matches = search();
        const next = matches.findIndex(match => match.block > from.block
            || (match.block === from.block && match.end > from.offset));
        current = matches.length ? Math.max(next, 0) : -1;
        show();
    };

    const go = step => {
        if (!matches.length) return;
        current = (current + step + matches.length) % matches.length;
        from = { block: matches[current].block, offset: matches[current].start };
        show();
        reveal();
    };

    const open = () => {
        const selected = host.selectedText();
        from = host.caret();
        shown = bar();
        shown.hidden = false;
        toolbarButton()?.setAttribute('aria-pressed', 'true');
        const text = part('text');
        if (selected) text.value = selected;
        text.focus();
        text.select();
        refresh();
        reveal();
    };

    // Forgets the search, without moving the focus.
    const reset = () => {
        unmark();
        [shown, matches, current] = [null, [], -1];
        toolbarButton()?.setAttribute('aria-pressed', 'false');
    };

    // The editor gets the focus back, with the current match selected when there is one.
    const close = () => {
        const match = matches[current];
        say('');
        shown.hidden = true;
        reset();
        host.focus(match);
    };

    // The search goes on after the new text, so text that holds what was sought isn't matched again.
    const replaceCurrent = () => {
        const match = matches[current];
        if (!match || host.readOnly()) return;
        const text = part('with').value;
        from = { block: match.block, offset: match.start + text.length };
        host.replace([match], text);
        reveal();
        if (!matches.length) part('text').focus();
    };

    const replaceAll = () => {
        if (!matches.length || host.readOnly()) return;
        const count = matches.length;
        from = { block: matches[0].block, offset: matches[0].start };
        host.replace(matches, part('with').value);
        say(words('replaced', count));
        part('text').focus();
    };

    const actions = {
        previous: () => go(-1), next: () => go(1), close, replace: replaceCurrent, all: replaceAll,
    };

    const onClick = event => {
        const button = event.target.closest('[data-find]');
        if (!button || button.disabled || !shown?.contains(button)) return;
        const name = button.dataset.find;
        if (name === 'case' || name === 'word') {
            button.setAttribute('aria-pressed', String(!pressed(name)));
            refresh();
            reveal();
        } else {
            actions[name]?.();
        }
    };

    // Clicking one of the bar's buttons leaves the focus in the bar's field, so Enter and Escape still reach the bar;
    // WebKit would otherwise leave the focus nowhere.
    const onMouseDown = event => {
        if (shown?.contains(event.target) && event.target.closest('button')) event.preventDefault();
    };

    const onInput = event => {
        if (!shown || event.target !== part('text')) return;
        refresh();
        reveal();
    };

    const onKeyDown = event => {
        if (!shown?.contains(event.target) || isComposing(event)) return;
        const field = event.target.dataset.find;
        if (event.key === 'Escape') {
            // The bar closes, and nothing around the editor, such as a dialog, hears the key.
            event.preventDefault();
            event.stopPropagation();
            close();
        } else if (event.key === 'Enter' && (field === 'text' || field === 'with')) {
            // Enter in a form would submit it.
            event.preventDefault();
            if (field === 'with') replaceCurrent();
            else go(event.shiftKey ? -1 : 1);
        } else if (event.key.toLowerCase() === 'f' && (isApple() ? event.metaKey : event.ctrlKey) && !event.shiftKey) {
            // The bar is open already, so the key only goes back to its Find field; the search stays where it is.
            event.preventDefault();
            part('text').focus();
            part('text').select();
        }
    };

    scope.addEventListener('mousedown', onMouseDown, { signal });
    scope.addEventListener('click', onClick, { signal });
    scope.addEventListener('input', onInput, { signal });
    scope.addEventListener('keydown', onKeyDown, { signal });
    // Blazor takes the bar away while the HTML source shows, and when the app turns find off.
    const gone = new view.MutationObserver(() => {
        if (shown && !shown.isConnected) reset();
    });
    gone.observe(scope, { childList: true });

    return {
        // Whether the editor has a find bar now, which it doesn't while its HTML source shows.
        available: () => !!bar(),
        open,
        toggle: () => shown ? close() : open(),
        refresh,
        dispose: () => {
            unmark();
            gone.disconnect();
        },
    };
}
