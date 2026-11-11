// Focus inside a panel that turns inert would fall to the page, so it moves to the panel's header instead. The JS
// initializer imports this at startup, since an item on its own has no accordion to load it.
new MutationObserver(records => {
    for (const { target: panel } of records) {
        if (panel.inert && panel.matches('.tb-accordion__panel') && panel.contains(document.activeElement)) {
            document.getElementById(panel.getAttribute('aria-labelledby')).focus();
        }
    }
}).observe(document.documentElement, { attributeFilter: ['inert'], subtree: true });

// The accordion or item whose panel holds this item; only an accordion's own items close one another.
const ownerOf = item => item.parentElement.closest('.tb-accordion, .tb-accordion__item');

// Until the page is interactive, a header opens and closes its item here, and find-in-page opens the item whose panel
// holds a match. Blazor renders an interactive header with data-ready and handles it from then on.
document.addEventListener('click', event => {
    const trigger = event.target.closest?.('.tb-accordion__trigger:not([data-ready], :disabled)');
    if (trigger) {
        expand(trigger, trigger.getAttribute('aria-expanded') !== 'true');
    }
});
document.addEventListener('beforematch', event => {
    if (event.target.matches('.tb-accordion__panel')) {
        expand(document.getElementById(event.target.getAttribute('aria-labelledby')), true);
    }
}, true);
// The browser may show a match before this script runs, so an item whose panel it showed opens now.
const closedTriggers = '.tb-accordion__trigger:not([data-ready], :disabled)[aria-expanded="false"]';
for (const trigger of document.querySelectorAll(closedTriggers)) {
    if (document.getElementById(trigger.getAttribute('aria-controls'))?.hidden === false) {
        expand(trigger, true);
    }
}

function expand(trigger, open) {
    const item = trigger.closest('.tb-accordion__item');
    const owner = ownerOf(item);
    if (open && owner?.matches('.tb-accordion:not([data-multiple])')) {
        for (const other of owner.querySelectorAll('.tb-accordion__trigger[aria-expanded="true"]')) {
            if (ownerOf(other.closest('.tb-accordion__item')) === owner) {
                expand(other, false);
            }
        }
    }

    item.classList.toggle('tb-accordion__item--expanded', open);
    trigger.setAttribute('aria-expanded', open);
    const panel = document.getElementById(trigger.getAttribute('aria-controls'));
    if (open) {
        panel.removeAttribute('hidden');
    } else if (trigger.disabled) {
        // Like the server's markup, a disabled item's closed panel is inert, so find skips it.
        panel.inert = true;
    } else {
        panel.setAttribute('hidden', 'until-found');
    }
}

// Up and Down move between the accordion's own headers, wrapping at the ends, and Home and End go to the first and
// last; each header stays in the tab order, as the WAI-ARIA accordion pattern keeps it.
export function attachAccordion(accordion) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!accordion) {
        return null;
    }

    // Only header buttons count, not a menu's trigger in a panel, and an item in a panel isn't this accordion's.
    const ownHeader = element => element.matches('.tb-accordion__trigger')
        && ownerOf(element.closest('.tb-accordion__item')) === accordion;
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
