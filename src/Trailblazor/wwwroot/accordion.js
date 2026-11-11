// Focus inside a panel that turns inert would fall to the page, so it moves to the panel's header instead. The JS
// initializer imports this at startup, since an item on its own has no accordion to load it.
new MutationObserver(records => {
    for (const { target: panel } of records) {
        if (panel.inert && panel.matches('.tb-accordion__panel') && panel.contains(document.activeElement)) {
            document.getElementById(panel.getAttribute('aria-labelledby')).focus();
        }
    }
}).observe(document.documentElement, { attributeFilter: ['inert'], subtree: true });

// Up and Down move between the accordion's own headers, wrapping at the ends, and Home and End go to the first and
// last; each header stays in the tab order, as the WAI-ARIA accordion pattern keeps it.
export function attachAccordion(accordion) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!accordion) {
        return null;
    }

    // Only header buttons count, not a menu's trigger in a panel, and an item in a panel isn't this accordion's.
    const ownHeader = element => element.matches('.tb-accordion__trigger')
        && element.closest('.tb-accordion__item').parentElement.closest('.tb-accordion, .tb-accordion__item')
            === accordion;
    const headers = () => [...accordion.querySelectorAll('.tb-accordion__trigger')]
        .filter(header => ownHeader(header) && !header.disabled);

    const onKeyDown = event => {
        const header = event.target;
        if (!ownHeader(header) || event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        const list = headers();
        const index = list.indexOf(header);
        const next = {
            ArrowDown: list[(index + 1) % list.length],
            ArrowUp: list[(index - 1 + list.length) % list.length],
            Home: list[0],
            End: list.at(-1),
        }[event.key];
        if (next) {
            event.preventDefault();
            next.focus();
        }
    };

    accordion.addEventListener('keydown', onKeyDown);
    accordion.dataset.ready = '';
    return {
        dispose() {
            accordion.removeEventListener('keydown', onKeyDown);
        },
    };
}
