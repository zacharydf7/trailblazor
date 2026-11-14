import { markedTarget } from '../Trailblazor.Primitives/focus-scope.js';
import { typedInSelector } from '../Trailblazor.Primitives/pointer-origin.js';

let handled = new WeakSet();
// A checkbox or radio isn't typing, so one that reveals a field gives focus up to it.
const typing = () => document.activeElement?.matches(typedInSelector);

// A field with AutoFocus takes focus as its control appears, unless the user is typing in another field by then. A mark
// a re-render only moves, as a keyed list does when it reorders, isn't new. An opening dialog or focus scope moves focus
// to the mark itself, so marks in a closed dialog or in a scope are left to it.
function focusFirst(marks) {
    const fresh = marks.filter(mark => !handled.has(mark));
    fresh.forEach(mark => handled.add(mark));
    const target = fresh.find(mark => !mark.closest('[data-part="focus-scope"]')
        && mark.closest('dialog')?.open !== false);
    if (target && !typing()) {
        markedTarget(target)?.focus();
    }
}

new MutationObserver(records => focusFirst(records.flatMap(record => [...record.addedNodes])
    .filter(node => node.nodeType === Node.ELEMENT_NODE)
    .flatMap(node => [...(node.matches('[data-tb-autofocus]') ? [node] : []),
        ...node.querySelectorAll('[data-tb-autofocus]')])))
    .observe(document, { childList: true, subtree: true });

// A page rendered on the server has its marks before this script runs, and an enhanced navigation can patch a mark
// onto a control it keeps, which adds no node, so the first run and the run after each enhanced navigation look over
// the whole page. Each is a new page, so a control kept from the last one counts as new.
export function focusMarksOnPage() {
    handled = new WeakSet();
    focusFirst([...document.querySelectorAll('[data-tb-autofocus]')]);
}
focusMarksOnPage();
