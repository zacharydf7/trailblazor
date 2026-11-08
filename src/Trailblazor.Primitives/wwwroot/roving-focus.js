// The arrow keys, Home and End move the caret in these, so they stay with the field.
const textEntry = 'input:not([type="checkbox"], [type="radio"], [type="button"], [type="submit"], [type="reset"]), '
    + 'textarea, select, [role="combobox"], [contenteditable]:not([contenteditable="false"])';

// One tab stop for a set of items: the current item has tabindex 0 and the rest -1, and the arrow keys, Home and End
// move between those that can take focus, including one marked only aria-disabled. `items` selects them and
// `filter` drops some, `vertical()` says which arrows move, and `preferred()` names the item that takes the tab stop
// when focus hasn't been inside yet, such as the pressed one.
export function createRovingFocus(container, { items, filter = () => true, vertical = () => false,
    preferred = () => null }) {
    let current = null;
    let managed = [];

    const all = () => [...container.querySelectorAll(items)].filter(filter);
    const focusable = () => all().filter(item => !item.disabled);

    const sync = () => {
        const list = focusable();
        // A filter that drops hidden items empties the list while the container is hidden, so the last focused item
        // is kept for when it shows.
        if (!list.includes(current) && (container.checkVisibility() || !current?.isConnected)) {
            current = null;
        }

        const stop = list.includes(current) ? current : list.includes(preferred()) ? preferred() : list[0];
        const inSet = all();
        // An item that left the set, such as a link turned off, would otherwise stay a tab stop. Leave a -1 alone,
        // since it may be the item's own and keeps focus on it.
        const left = managed.filter(item => !inSet.includes(item) && item.tabIndex === 0 && item.isConnected);
        for (const item of left.filter(item => item.checkVisibility())) {
            item.removeAttribute('tabindex');
        }

        for (const item of inSet) {
            item.tabIndex = item === stop ? 0 : -1;
        }

        // Keep tracking a hidden one that left, since it may come back as it was; a later sync checks it again.
        managed = [...inSet, ...left.filter(item => !item.checkVisibility())];
    };
    const moveTo = item => {
        if (item) {
            current = item;
            sync();
            item.focus();
        }
    };

    const onKeyDown = event => {
        const list = focusable();
        const index = list.indexOf(event.target);
        if (index < 0 || event.altKey || event.ctrlKey || event.metaKey || event.target.matches(textEntry)) {
            return;
        }

        const rtl = getComputedStyle(container).direction === 'rtl';
        const next = vertical() ? 'ArrowDown' : rtl ? 'ArrowLeft' : 'ArrowRight';
        const previous = vertical() ? 'ArrowUp' : rtl ? 'ArrowRight' : 'ArrowLeft';
        const target = {
            [next]: list[(index + 1) % list.length],
            [previous]: list[(index - 1 + list.length) % list.length],
            Home: list[0],
            End: list.at(-1),
        }[event.key];
        if (target) {
            event.preventDefault();
            moveTo(target);
        }
    };
    const onFocusIn = event => {
        const item = focusable().find(candidate => candidate === event.target);
        if (item && item !== current) {
            current = item;
            sync();
        }
    };

    // Items come and go, turn off, or become the preferred one as the server re-renders. A busy link stays
    // aria-disabled, so turning it off or back on changes only its href.
    const observer = new MutationObserver(sync);
    observer.observe(container, {
        childList: true, subtree: true, attributes: true,
        attributeFilter: ['disabled', 'aria-disabled', 'aria-pressed', 'data-kept-focusable', 'href'],
    });
    container.addEventListener('keydown', onKeyDown);
    container.addEventListener('focusin', onFocusIn);
    sync();

    return {
        // Call after changing what `filter` lets through.
        sync,
        focusable,
        dispose() {
            observer.disconnect();
            container.removeEventListener('keydown', onKeyDown);
            container.removeEventListener('focusin', onFocusIn);
        },
    };
}
