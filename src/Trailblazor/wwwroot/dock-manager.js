import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { nextTab, scrollTabIntoRow } from './tabs.js';

const sides = ['x', 'y', 'width', 'height'];
const names = { x: 'x', y: 'y', width: 'w', height: 'h' };

// The dock renders its groups, panels and separators as siblings, each placed by a box in percent of the dock. This
// script keeps each panel after its group's tabs, so Tab and screen readers meet them in reading order; moves
// between tabs with the arrow keys; closes a tab with Delete, Backspace or a middle click; and moves the separators,
// by dragging or with the keys, showing the new boxes at once and reporting the move when it stops. `reporter` hears
// it all.
export function attachDock(root, reporter) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    const parts = name => [...root.children].filter(child => child.dataset.part === name);
    // The dock's own tab or separator under a target, not one of a dock, tabs or a splitter inside a panel.
    const ownPart = (target, name) => {
        const part = target.closest?.(`.tb-dock__${name}`);
        return part?.closest('.tb-dock') === root ? part : null;
    };

    // Blazor never moves a panel, since a moved element loses its scroll positions, its focus and what its iframes
    // loaded. A panel that joins another group is moved here, with moveBefore where the browser has it, which keeps
    // the iframes. Scroll positions and focus are put back either way, since not every moveBefore keeps them.
    const place = (panel, before) => {
        const scrolled = [panel, ...panel.querySelectorAll('*')]
            .filter(element => element.scrollTop || element.scrollLeft)
            .map(element => [element, element.scrollTop, element.scrollLeft]);
        const focused = panel.contains(document.activeElement) ? document.activeElement : null;
        if (root.moveBefore) {
            root.moveBefore(panel, before);
        } else {
            root.insertBefore(panel, before);
        }

        for (const [element, top, left] of scrolled) {
            element.scrollTop = top;
            element.scrollLeft = left;
        }

        if (focused && document.activeElement !== focused) {
            focused.focus({ preventScroll: true });
        }
    };
    const order = () => {
        const panels = parts('panel');
        parts('group').forEach((group, index) => {
            let last = group;
            for (const panel of panels.filter(each => Number(each.dataset.dockGroup) === index)) {
                if (last.nextElementSibling !== panel) {
                    place(panel, last.nextElementSibling);
                }

                last = panel;
            }
        });
    };
    // A tab selected from code may sit outside a row that overflows. Scrolling only the row leaves the page alone.
    // A tab is revealed only as it becomes its row's selected one, so a row the user has since scrolled stays put.
    const revealed = new WeakMap();
    // A row with no width yet can't be measured, so it isn't taken as revealed.
    const reveal = row => {
        const tab = row.querySelector(':scope > [aria-selected="true"]');
        if (tab && row.clientWidth > 0 && revealed.get(row) !== tab.id) {
            revealed.set(row, tab.id);
            scrollTabIntoRow(row, tab);
        }
    };
    // A tab selected in place changes its attribute. One that arrives already selected changes none, but its panel
    // arrives or moves with it, which changes the dock's children, and every row is looked at again then.
    const selected = new MutationObserver(records => {
        for (const row of new Set(records.map(record => record.target.parentElement))) {
            reveal(row);
        }
    });
    const watchGroups = () => {
        for (const group of parts('group')) {
            selected.observe(group.firstElementChild,
                { subtree: true, attributes: true, attributeFilter: ['aria-selected'] });
            reveal(group.firstElementChild);
        }
    };
    const added = new MutationObserver(() => {
        order();
        watchGroups();
    });
    added.observe(root, { childList: true });
    const regrouped = new MutationObserver(order);
    regrouped.observe(root, { subtree: true, attributes: true, attributeFilter: ['data-dock-group'] });
    order();
    watchGroups();

    // The boxes the separators have moved to since the app last rendered. The app's render of an earlier move would
    // put them back, so they show until data-report says the app has rendered the last move reported.
    const moved = new Map();
    const movedSides = new Map();
    let reports = 0;
    const boxOf = element => moved.get(element) ?? Object.fromEntries(sides.map(side =>
        [side, Number(element.style.getPropertyValue(`--tb-dock-${names[side]}`))]));
    const sidesOf = separator => movedSides.get(separator) ?? Object.fromEntries(['before', 'after'].map(side => {
        const [x, y, width, height] = separator.dataset[side].split(' ').map(Number);
        return [side, { x, y, width, height }];
    }));
    const show = () => {
        for (const [element, box] of moved) {
            for (const side of sides) {
                element.style.setProperty(`--tb-dock-live-${names[side]}`, String(box[side]));
            }
        }
    };
    const settle = () => {
        if (Number(root.dataset.report ?? 0) < reports) {
            show();
            return;
        }

        for (const element of moved.keys()) {
            for (const side of sides) {
                element.style.removeProperty(`--tb-dock-live-${names[side]}`);
            }
        }

        moved.clear();
        movedSides.clear();
    };
    const rendered = new MutationObserver(settle);
    rendered.observe(root, { attributes: true, attributeFilter: ['data-report'] });

    const vertical = separator => separator.getAttribute('aria-orientation') === 'horizontal';
    const within = (box, region) => box.x >= region.x - 0.01 && box.y >= region.y - 0.01
        && box.x + box.width <= region.x + region.width + 0.01 && box.y + box.height <= region.y + region.height + 0.01;
    // Moves a separator's edge to a place along its split, in percent of the dock, as far as the smallest group on
    // each side allows. Everything inside the two parts keeps its share of its part. Gives the share the part before
    // has of the two, or null when nothing can move.
    const moveTo = (separator, to) => {
        const [start, length] = vertical(separator) ? ['y', 'height'] : ['x', 'width'];
        const { before, after } = sidesOf(separator);
        const others = [...root.children].filter(child => child !== separator
            && ['group', 'panel', 'separator'].includes(child.dataset.part));
        const least = Number(root.dataset.minSize) / (vertical(separator) ? root.clientHeight : root.clientWidth) * 100;
        // The smallest a part can be: the size at which its narrowest group, scaled with the part, is down to the
        // least size.
        const floor = part => least / Math.min(...others
            .filter(element => element.dataset.part === 'group' && within(boxOf(element), part))
            .map(element => boxOf(element)[length] / part[length]));
        const low = before[start] + floor(before);
        const high = after[start] + after[length] - floor(after);
        if (!(low <= high)) {
            return null;
        }

        const edge = Math.min(Math.max(to, low), high);
        const parts = [
            [before, { ...before, [length]: edge - before[start] }],
            [after, { ...after, [start]: edge, [length]: after[start] + after[length] - edge }],
        ];
        const scale = (box, [from, into]) => ({
            ...box,
            [start]: into[start] + (box[start] - from[start]) * into[length] / from[length],
            [length]: box[length] * into[length] / from[length],
        });
        for (const part of parts) {
            for (const element of others.filter(each => within(boxOf(each), part[0]))) {
                // A separator inside the part, or along its far edge, has a side of its own that the part holds.
                if (element.dataset.part === 'separator') {
                    const own = sidesOf(element);
                    movedSides.set(element, {
                        before: within(own.before, part[0]) ? scale(own.before, part) : own.before,
                        after: within(own.after, part[0]) ? scale(own.after, part) : own.after,
                    });
                }

                moved.set(element, scale(boxOf(element), part));
            }
        }

        moved.set(separator, { ...boxOf(separator), [start]: edge });
        movedSides.set(separator, { before: parts[0][1], after: parts[1][1] });
        show();
        const share = parts[0][1][length] / (parts[0][1][length] + parts[1][1][length]);
        separator.setAttribute('aria-valuenow', String(Math.round(share * 100)));
        return share;
    };
    const report = (separator, share) => {
        if (share !== null) {
            reports++;
            callDotNet(reporter, 'OnResize', Number(separator.dataset.index), share, reports);
        }
    };
    // Where the pointer is along a separator's split, in percent of the dock from the side its boxes count from.
    const along = (separator, event) => {
        const box = root.getBoundingClientRect();
        if (vertical(separator)) {
            return (event.clientY - box.top) / box.height * 100;
        }

        return getComputedStyle(root).direction === 'rtl' ? (box.right - event.clientX) / box.width * 100
            : (event.clientX - box.left) / box.width * 100;
    };
    const edgeOf = separator => boxOf(separator)[vertical(separator) ? 'y' : 'x'];

    let drag = null;
    const onPointerDown = event => {
        const separator = ownPart(event.target, 'separator');
        if (!separator || event.button !== 0) {
            return;
        }

        event.preventDefault();
        // Focus from script counts as keyboard focus, so the mark keeps a press from showing the focus highlight.
        separator.dataset.pointerFocus = '';
        separator.focus();
        separator.setPointerCapture(event.pointerId);
        // A panel's iframe would take the pointer as it crosses, so the panels let it through while a drag runs.
        root.dataset.resizing = '';
        drag = { separator, grab: along(separator, event) - edgeOf(separator), share: null };
    };
    const onPointerMove = event => {
        if (drag) {
            drag.share = moveTo(drag.separator, along(drag.separator, event) - drag.grab) ?? drag.share;
        }
    };
    const onPointerUp = () => {
        if (drag) {
            delete root.dataset.resizing;
            report(drag.separator, drag.share);
            drag = null;
        }
    };

    const onKeyDown = event => {
        if (event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        const separator = ownPart(event.target, 'separator');
        if (separator === event.target) {
            delete separator.dataset.pointerFocus;
            const rtl = !vertical(separator) && getComputedStyle(root).direction === 'rtl';
            const grow = vertical(separator) ? 'ArrowDown' : rtl ? 'ArrowLeft' : 'ArrowRight';
            const shrink = vertical(separator) ? 'ArrowUp' : rtl ? 'ArrowRight' : 'ArrowLeft';
            // A step is 1% of what the two parts take together, or 10% with Shift, so aria-valuenow moves by as much.
            const { before, after } = sidesOf(separator);
            const length = vertical(separator) ? 'height' : 'width';
            const step = (event.shiftKey ? 10 : 1) * (before[length] + after[length]) / 100;
            const edge = edgeOf(separator);
            const to = { [grow]: edge + step, [shrink]: edge - step, Home: 0, End: 100 }[event.key];
            if (to !== undefined) {
                event.preventDefault();
                report(separator, moveTo(separator, to));
            }

            return;
        }

        const tab = ownPart(event.target, 'tab');
        if (tab !== event.target) {
            return;
        }

        if ((event.key === 'Delete' || event.key === 'Backspace') && tab.hasAttribute('aria-keyshortcuts')) {
            event.preventDefault();
            // A held key repeats, and each repeat would ask to close the tab again while the first close runs.
            if (!event.repeat) {
                callDotNet(reporter, 'OnClose', tab.dataset.panel, true);
            }

            return;
        }

        const next = nextTab(tab.parentElement, event);
        if (next) {
            event.preventDefault();
            next.focus();
            callDotNet(reporter, 'OnSelect', next.dataset.panel);
        }
    };
    const onFocusOut = event => delete ownPart(event.target, 'separator')?.dataset.pointerFocus;
    // A middle click closes a closable tab, as in a browser. Its press would start the browser's scrolling mode.
    const closable = event => event.button === 1 && ownPart(event.target, 'tab')?.hasAttribute('aria-keyshortcuts')
        ? ownPart(event.target, 'tab') : null;
    // A press on a close mark would focus its tab in some browsers and not in others, so in all of them the focus
    // stays where it is, as for a middle click.
    const onMouseDown = event => {
        if (closable(event) || (event.button === 0 && ownPart(event.target, 'close'))) {
            event.preventDefault();
        }
    };
    const holdsFocus = tab => tab === document.activeElement
        || Boolean(document.getElementById(tab.getAttribute('aria-controls'))?.contains(document.activeElement));
    const onAuxClick = event => {
        const tab = closable(event);
        if (tab) {
            event.preventDefault();
            // The click left the focus where it was, so it moves only if the closing tab or its panel holds it.
            callDotNet(reporter, 'OnClose', tab.dataset.panel, holdsFocus(tab));
        }
    };

    root.addEventListener('pointerdown', onPointerDown);
    root.addEventListener('pointermove', onPointerMove);
    root.addEventListener('pointerup', onPointerUp);
    root.addEventListener('pointercancel', onPointerUp);
    root.addEventListener('keydown', onKeyDown);
    root.addEventListener('focusout', onFocusOut);
    root.addEventListener('mousedown', onMouseDown);
    root.addEventListener('auxclick', onAuxClick);

    // Marks that the keys and drags work, which tests wait for.
    root.dataset.ready = '';
    return {
        // Whether the tab with this id, or its panel, holds the focus. The dock asks when the tab's close mark is
        // clicked.
        holdsFocus(id) {
            const tab = document.getElementById(id);
            return Boolean(tab) && holdsFocus(tab);
        },
        dispose() {
            added.disconnect();
            regrouped.disconnect();
            selected.disconnect();
            rendered.disconnect();
            root.removeEventListener('pointerdown', onPointerDown);
            root.removeEventListener('pointermove', onPointerMove);
            root.removeEventListener('pointerup', onPointerUp);
            root.removeEventListener('pointercancel', onPointerUp);
            root.removeEventListener('keydown', onKeyDown);
            root.removeEventListener('focusout', onFocusOut);
            root.removeEventListener('mousedown', onMouseDown);
            root.removeEventListener('auxclick', onAuxClick);
        },
    };
}
