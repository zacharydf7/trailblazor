import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { frames, watchSizes } from '../Trailblazor.Primitives/pages.js';
// Enter in a compact pagination's box goes to the typed page, and mustn't also submit a form around it. Canceling the key
// also cancels the change event that commits typed text, so this sends one itself. The JS initializer imports this at
// startup, so static pages are covered too.
document.addEventListener('keydown', event => {
    if (event.key === 'Enter' && !isComposing(event) && !event.defaultPrevented
        && event.target.closest?.('[data-part="page-box"]')) {
        event.preventDefault();
        event.target.dispatchEvent(new Event('change', { bubbles: true }));
    }
});

// A column gap wider than any container, so a list using it always wants more width than its pager can get.
const unbounded = 100000;

// Tells the pager how many buttons fit on one row, so it shows fewer pages before its list would wrap. The room is
// measured with a gap between the buttons that no container holds: the pager then takes all the width it may have,
// whether it fills a block, shares a row with other content or shrinks to fit what it holds.
export function fitPagination(pager, list, fit) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!pager || !list) {
        return null;
    }

    // The compact form has no page buttons to measure, so the width last measured is kept.
    let button = 0;
    let slots = null;
    // How many buttons fit, or null where nothing limits the pager's width. A hidden pager has no room to measure,
    // and keeps what it had.
    const read = () => {
        const pages = [...list.querySelectorAll('[data-part="page"]')];
        if (pages.length > 0) {
            button = Math.max(...pages.map(page => page.getBoundingClientRect().width));
        }

        if (button === 0 || pager.clientWidth === 0) {
            return slots;
        }

        const gap = parseFloat(getComputedStyle(list).columnGap) || 0;
        // The gap lies between items, so a list of one borrows a second for the measurement.
        const second = list.children.length < 2 ? list.appendChild(list.ownerDocument.createElement('li')) : null;
        const own = list.style.columnGap;
        list.style.columnGap = `${unbounded}px`;
        const style = getComputedStyle(pager);
        // Measured in fractions of a pixel, as the buttons are, so a pager half a pixel short isn't rounded up.
        const room = pager.getBoundingClientRect().width - parseFloat(style.paddingLeft)
            - parseFloat(style.paddingRight) - parseFloat(style.borderLeftWidth) - parseFloat(style.borderRightWidth);
        list.style.columnGap = own;
        second?.remove();

        // A pager that grew to hold the gap is in nothing that limits its width.
        return room >= unbounded ? null : Math.floor((room + gap) / (button + gap));
    };
    slots = read();

    // Counts the fits the pager hasn't shown yet, so the mark a test waits out stays until the last one shows.
    let pending = 0;
    const settle = () => {
        if (pending === 0) {
            delete pager.dataset.fitting;
        }
    };
    const refit = () => {
        const fits = read();
        if (fits === slots) {
            settle();
            return;
        }

        slots = fits;
        pending++;
        const focused = list.contains(pager.ownerDocument.activeElement);
        callDotNet(fit, 'OnFit', slots).then(() => {
            pending--;
            settle();
            // The focused button may be one that went, and focus mustn't fall to the page.
            if (focused && !list.contains(pager.ownerDocument.activeElement)) {
                list.querySelector('[aria-current="page"], [data-part="page-box"] input')?.focus();
            }
        });
    };

    const frame = frames(pager, refit);
    // Marks a fit that is still to come, which a test waits out before choosing a page.
    const fitSoon = () => {
        pager.dataset.fitting = '';
        frame.request();
    };
    // Only a width changes the room. The pager's own height changes when its list wraps, so that counts too.
    const widths = new WeakMap();
    const sizes = watchSizes(entries => {
        const changed = entries.filter(entry => {
            const was = widths.get(entry.target);
            widths.set(entry.target, entry.contentRect.width);
            return entry.target === pager || was !== entry.contentRect.width;
        });
        if (changed.length > 0) {
            fitSoon();
        }
    });
    // The room changes with any box around the pager, and with what shares its row, whenever that comes or goes.
    for (let box = pager; box; box = box.parentElement) {
        sizes.observe(box);
    }

    const watchRow = () => {
        for (const beside of pager.parentElement?.children ?? []) {
            sizes.observe(beside);
        }
    };
    const row = new MutationObserver(() => {
        watchRow();
        fitSoon();
    });
    if (pager.parentElement) {
        row.observe(pager.parentElement, { childList: true });
    }

    watchRow();

    return {
        slots: () => slots,
        dispose() {
            frame.cancel();
            sizes.disconnect();
            row.disconnect();
            delete pager.dataset.fitting;
        },
    };
}
