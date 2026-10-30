// One tab stop for a set of items: the current item has tabindex 0 and the rest -1, and the arrow keys, Home and End
// move between the enabled items. `items` selects them, `vertical()` says which arrows move, and `preferred()`
// names the item that takes the tab stop when focus hasn't been inside yet, such as the pressed one.
export function createRovingFocus(container, { items, vertical = () => false, preferred = () => null }) {
    let current = null;

    const all = () => [...container.querySelectorAll(items)];
    const enabled = () => all().filter(item => !item.disabled && item.getAttribute('aria-disabled') !== 'true');

    const sync = () => {
        const list = enabled();
        if (!list.includes(current)) {
            current = null;
        }

        const stop = current ?? (list.includes(preferred()) ? preferred() : list[0]);
        for (const item of all()) {
            item.tabIndex = item === stop ? 0 : -1;
        }
    };
    const moveTo = item => {
        if (item) {
            current = item;
            sync();
            item.focus();
        }
    };

    const onKeyDown = event => {
        const list = enabled();
        const index = list.indexOf(event.target);
        if (index < 0 || event.altKey || event.ctrlKey || event.metaKey) {
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
        const item = enabled().find(candidate => candidate === event.target);
        if (item && item !== current) {
            current = item;
            sync();
        }
    };

    // Items come and go, turn off, or become the preferred one as the server re-renders.
    const observer = new MutationObserver(sync);
    observer.observe(container, {
        childList: true, subtree: true, attributes: true, attributeFilter: ['disabled', 'aria-disabled', 'aria-pressed'],
    });
    container.addEventListener('keydown', onKeyDown);
    container.addEventListener('focusin', onFocusIn);
    sync();

    return {
        dispose() {
            observer.disconnect();
            container.removeEventListener('keydown', onKeyDown);
            container.removeEventListener('focusin', onFocusIn);
        },
    };
}
