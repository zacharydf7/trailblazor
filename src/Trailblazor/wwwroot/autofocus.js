import { markedTarget } from '../Trailblazor.Primitives/focus-scope.js';

const handled = new WeakSet();
// Controls that take text; a checkbox or radio that reveals a field gives focus up to it.
const typing = () => document.activeElement?.matches('textarea, [contenteditable]:not([contenteditable="false"]), '
    + 'input:not([type=checkbox], [type=radio], [type=range], [type=button], [type=submit], [type=reset], [type=image], '
    + '[type=file], [type=color])');

// A field with AutoFocus takes focus as its control appears, unless the user is typing in another field by then. A mark
// a re-render only moves, as a keyed list does when it reorders, isn't new. An opening dialog or focus scope moves focus
// to the mark itself, so marks in a closed dialog or in a scope are left to it.
const observer = new MutationObserver(records => {
    const marks = records.flatMap(record => [...record.addedNodes])
        .filter(node => node.nodeType === Node.ELEMENT_NODE)
        .flatMap(node => [...(node.matches('[data-tb-autofocus]') ? [node] : []),
            ...node.querySelectorAll('[data-tb-autofocus]')])
        .filter(mark => !handled.has(mark));
    marks.forEach(mark => handled.add(mark));
    const target = marks.find(mark => !mark.closest('[data-part="focus-scope"]')
        && mark.closest('dialog')?.open !== false);
    if (target && !typing()) {
        markedTarget(target)?.focus();
    }
});
observer.observe(document, { childList: true, subtree: true });
