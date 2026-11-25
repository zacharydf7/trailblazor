import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { watchSizes } from '../Trailblazor.Primitives/pages.js';
import { textHolders } from './tooltip.js';

const characters = new Intl.Segmenter();

// Whether the text sits in something that takes focus, such as a link, and is the only text in it that the script
// watches. That focus then shows the text's tooltip, so the text needs no tab stop of its own; with two such texts it
// couldn't show both, so each keeps its own.
const isHeld = text => {
    const holder = text.parentElement?.closest(textHolders);
    return holder != null && [...holder.querySelectorAll('.tb-text[data-measures]')]
        .filter(other => other.parentElement.closest(textHolders) === holder).length === 1;
};

// Tells .NET whether the text's box cuts it off, each time that changes as the box or the text does, and whether it
// is held, so focus on its holder shows the tooltip instead of a tab stop on the text. A tooltip's bubble inside the
// text shows outside the box and takes no room in it. With `middle`, it also cuts the text in the middle to fit,
// keeping as much of its start and end as the box holds. Without `measure`, it only says whether the text is held.
export function watchTextCut(text, events, measure, middle) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!text) {
        return null;
    }

    const held = isHeld(text);
    if (!measure) {
        callDotNet(events, 'OnCut', false, held);
        return null;
    }

    const overflows = () => text.scrollHeight > text.clientHeight || text.scrollWidth > text.clientWidth;
    const full = text.querySelector(':scope > [data-part="full-text"]');
    const shown = text.querySelector(':scope > [data-part="middle"]');
    let cut = null;
    const check = () => {
        const now = middle ? cutMiddle(text, full.textContent, shown, overflows) : overflows();
        if (now !== cut) {
            cut = now;
            callDotNet(events, 'OnCut', now, held);
        }
    };
    const sizes = watchSizes(check);
    sizes.observe(text);
    // The middle cut changes what the text holds, so only the whole text, which the render keeps, is watched then.
    const changes = new MutationObserver(check);
    changes.observe(middle ? full : text, { childList: true, characterData: true, subtree: true });
    if (middle) {
        // From here the cut that fits shows in place of the one made as the page rendered.
        text.dataset.middleShown = '';
    }

    check();

    return {
        dispose() {
            sizes.disconnect();
            changes.disconnect();
            delete text.dataset.middleShown;
        },
    };
}

// Shows the whole text, or, when it doesn't fit, the most characters that fit, half from its start and half from its
// end with an ellipsis between, and says whether it cut. A box too narrow for any keeps one character and the ellipsis.
// The widths come from one layout, so a grid's column of names doesn't lay the page out again for each try.
function cutMiddle(text, whole, shown, overflows) {
    shown.textContent = whole;
    if (!overflows()) {
        return false;
    }

    const parts = Array.from(characters.segment(whole), part => part.segment);
    const kept = count => parts.slice(0, Math.ceil(count / 2)).join('').trimEnd() + '…'
        + parts.slice(parts.length - Math.floor(count / 2)).join('').trimStart();
    shown.textContent = '…' + whole;
    const range = text.ownerDocument.createRange();
    let at = 0;
    const next = length => {
        range.setStart(shown.firstChild, at);
        range.setEnd(shown.firstChild, at += length);
        return range.getBoundingClientRect().width;
    };
    const ellipsis = next(1);
    const widths = parts.map(part => next(part.length));
    const style = getComputedStyle(text);
    const room = text.clientWidth - parseFloat(style.paddingInlineStart) - parseFloat(style.paddingInlineEnd);
    const widthOf = count => ellipsis + widths.slice(0, Math.ceil(count / 2)).reduce((sum, width) => sum + width, 0)
        + widths.slice(parts.length - Math.floor(count / 2)).reduce((sum, width) => sum + width, 0);
    let count = parts.length - 1;
    while (count > 1 && widthOf(count) > room) {
        count--;
    }

    // Characters side by side can take a little more or less room than apart, so the cut is checked once laid out.
    shown.textContent = kept(count);
    while (count > 1 && overflows()) {
        shown.textContent = kept(--count);
    }

    return true;
}
