import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { flush } from './debounced-input.js';

// Down Arrow in a tree select's filter box moves into the tree, so it's the only one of the box's keys that reaches the
// server. It first sends any text still waiting out the input delay, so the tree moves to a match of all of it. The JS
// initializer imports this at startup.
document.addEventListener('keydown', event => {
    if (event.key === 'ArrowDown' && !isComposing(event) && event.target.matches?.('.tb-tree-select__filter')) {
        flush(event.target);
        event.target.dispatchEvent(new CustomEvent('tb-tree-filter-down', { bubbles: true }));
    }
});
