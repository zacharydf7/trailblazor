import { autofocusTarget, getTabbables } from '../Trailblazor.Primitives/focus-scope.js';
import { onPagesChange, otherPages } from '../Trailblazor.Primitives/pages.js';
import { setRegionWindow } from './dialog.js';
import { alwaysUsable, hold, release } from './inert.js';
import { collect, ownParts } from './overlay.js';

// The regions that block, oldest first. The newest one the user can reach is in force: everything outside it is
// inert, an older region included, and the one before it is in force again when it goes.
const regions = [];
// What the region in force holds inert, each marked for the stylesheet to dim, and those of them that weren't
// inert already, which are this script's own doing.
let held = [];
let own = new Set();
// What can put a region in or out of reach.
const reach = ['inert', 'hidden', 'class', 'style'];
// Without this, an element added beside the region in force or beside one of its ancestors after the block started
// would be left usable, and a region that comes into reach, as in a dock panel whose tab is chosen, would hold nothing.
const changed = new MutationObserver(() => apply());

const ancestors = element => {
    const found = [];
    for (let node = element.parentElement; node; node = node.parentElement) {
        found.push(node);
    }

    return found;
};

// A region the user can't get to holds nothing, or nothing in the app could be used: one that doesn't show, or one
// inside something made inert, as a dock panel behind another tab is. What this script made inert doesn't count: a
// newer region starts inside what an older one holds. A region with `display: contents` has no box to ask about,
// so the element around it is asked.
const boxed = region => getComputedStyle(region).display === 'contents' ? region.parentElement ?? region : region;
const reachable = region => region.isConnected && boxed(region).checkVisibility({ visibilityProperty: true })
    && !ancestors(region).some(node => node.inert && !own.has(node));

function apply() {
    const top = regions.findLast(each => reachable(each.element));
    const region = top?.element;
    const page = region?.ownerDocument;
    const elements = [];
    // An observer keeps one set of options for a node, so what is watched on each is gathered first.
    const watched = new Map();
    const watch = (node, options) => watched.set(node, { ...watched.get(node), ...options });
    for (let node = region; node && node !== page.body; node = node.parentElement) {
        for (const sibling of node.parentElement?.children ?? []) {
            if (sibling !== node) {
                collect(sibling, top.usable, elements);
            }
        }

        if (node.parentElement) {
            watch(node.parentElement, { childList: true });
        }
    }

    // One of the app's other windows that holds a region the scope allows is held piece by piece, as the scope's
    // own window is, and what joins it later beside an allowed region or beside one of that region's ancestors is
    // held too. The rest are held whole, as under a modal dialog.
    const pieced = top?.allowed
        ? [document, ...otherPages()].filter(each => each !== page && each.querySelector(top.allowed)) : [];
    for (const each of pieced) {
        for (const child of each.body.children) {
            collect(child, top.usable, elements);
        }

        for (const allowed of each.querySelectorAll(top.allowed)) {
            ancestors(allowed).forEach(node => watch(node, { childList: true }));
        }
    }

    for (const each of regions.filter(one => one.element.isConnected)) {
        for (const node of [each.element, ...ancestors(each.element)]) {
            watch(node, { attributes: true, attributeFilter: reach });
        }
    }

    // The new block is held before the old one is released, so what both cover never takes input in between.
    own = new Set(elements.filter(element => !element.inert || own.has(element)));
    hold(elements);
    for (const element of elements) {
        element.dataset.tbBlocked = '';
    }

    release(held);
    for (const element of held.filter(each => !elements.includes(each))) {
        delete element.dataset.tbBlocked;
    }

    held = elements;
    changed.disconnect();
    watched.forEach((options, node) => changed.observe(node, options));
    // The app's other windows are held as a modal dialog holds them, apart from those held piece by piece.
    setRegionWindow(region ? () => region.ownerDocument : null, pieced);
    // The focus moves in as the region first comes into force, which is later than its scope started when an older
    // region held it or it was out of reach.
    if (top && !top.entered) {
        top.entered = true;
        if (top.autoFocus && !region.contains(page.activeElement)) {
            (autofocusTarget(region) ?? getTabbables(region)[0] ?? region).focus();
        }
    }
}

// A panel that holds the region can go to a window of its own, or come back from one.
onPagesChange(apply);

// Makes everything beside the region, and beside each of its ancestors, inert, in its window and in the app's other
// windows, until the result is disposed. Blazor's own parts, the live regions, toasts, dialogs, the popups the app
// names and what this region allows stay usable. Sets data-ready on the region once it is counted, which tests wait
// for.
export function blockOutside(element, outsidePopups, autoFocus, allow) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!element) {
        return null;
    }

    const usable = [...alwaysUsable, ...ownParts, ...outsidePopups, ...allow].join(', ');
    // A selector the app got wrong throws here, before anything is held.
    element.ownerDocument.querySelector(usable);
    const region = { element, usable, allowed: allow.join(', '), autoFocus, entered: false };
    regions.push(region);
    apply();
    element.dataset.ready = '';
    return {
        dispose() {
            regions.splice(regions.indexOf(region), 1);
            apply();
        },
    };
}
