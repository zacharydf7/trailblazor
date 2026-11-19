import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { createGripDrag } from '../Trailblazor.Primitives/grip-drag.js';
import { createPointerDrag } from '../Trailblazor.Primitives/pointer-drag.js';
import { nextTab, scrollTabIntoRow } from './tabs.js';

const sides = ['x', 'y', 'width', 'height'];
const names = { x: 'x', y: 'y', width: 'w', height: 'h' };

// The dock renders its groups, panels and separators as siblings, each placed by a box in percent of the dock. This
// script keeps each panel after its group's tabs, so Tab and screen readers meet them in reading order; moves
// between tabs with the arrow keys; closes a tab with Delete, Backspace or a middle click; and moves the separators,
// by dragging or with the keys, showing the new boxes at once and reporting the move when it stops; moves a panel
// where its tab is dragged to; has a double-clicked tab's group fill the dock; and moves and resizes the floating
// windows. `reporter` hears it all.
export function attachDock(root, reporter) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    const parts = name => [...root.children].filter(child => child.dataset.part === name);
    const storageKey = key => `tb-dock:${key}`;
    // The dock's own tab, separator or grip under a target, not one of a dock, tabs or a splitter inside a panel.
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
    // The groups' rows by their numbers: the docked groups, then the windows. A row needn't have been rendered in
    // that place, since a window can be older than a docked group.
    const groupsInOrder = () => parts('group').sort((a, b) => Number(a.dataset.group) - Number(b.dataset.group));
    // How high a part draws: a window's layer, and below every window for what is docked.
    const layerOf = part => part?.hasAttribute('data-floating')
        ? Number(part.style.getPropertyValue('--tb-dock-z')) : -1;
    // What the dock holds, in reading order: each group's row, its panels, then the separator after the group and
    // that separator's grip.
    const inReadingOrder = () => {
        const [panels, separators, grips, corners] = ['panel', 'separator', 'grip', 'window-grip'].map(parts);
        const edges = [...parts('slide-separator'), ...parts('slide-grip')];
        const after = index => [separators, grips].map(list => list.find(each => Number(each.dataset.index) === index));
        return groupsInOrder().flatMap(group => [group,
            ...panels.filter(panel => panel.dataset.dockGroup === group.dataset.group),
            ...after(Number(group.dataset.group)).filter(Boolean),
            ...corners.filter(corner => corner.dataset.group === group.dataset.group),
            ...(group.hasAttribute('data-edge') ? edges : [])]);
    };
    // Where the browser has no moveBefore, a move reloads an iframe, so a panel that holds one costs far more to move
    // than another panel, and a panel more than a row of tabs or a separator.
    const cost = part => part.dataset.part !== 'panel' ? 1 : part.querySelector('iframe') ? 1000 : 10;
    // Puts the parts in reading order by moving the cheapest set of them: the costliest run of parts already in
    // order stays where it is, and each other part goes before the part that follows it.
    const order = () => {
        const wanted = inReadingOrder();
        const now = [...root.children];
        const places = wanted.map(part => now.indexOf(part));
        const kept = wanted.map(cost);
        const before = wanted.map(() => -1);
        wanted.forEach((part, at) => {
            for (let earlier = 0; earlier < at; earlier++) {
                if (places[earlier] < places[at] && kept[earlier] + cost(part) > kept[at]) {
                    kept[at] = kept[earlier] + cost(part);
                    before[at] = earlier;
                }
            }
        });
        const stays = new Set();
        for (let at = kept.indexOf(Math.max(...kept)); at >= 0; at = before[at]) {
            stays.add(wanted[at]);
        }

        let next = null;
        for (const part of [...wanted].reverse()) {
            if (!stays.has(part)) {
                place(part, next);
            }

            next = part;
        }
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
            const row = group.querySelector('.tb-dock__tabs');
            selected.observe(row, { subtree: true, attributes: true, attributeFilter: ['aria-selected'] });
            reveal(row);
        }
    };
    const added = new MutationObserver(() => {
        order();
        watchGroups();
    });
    added.observe(root, { childList: true });
    const regrouped = new MutationObserver(order);
    regrouped.observe(root, { subtree: true, attributes: true, attributeFilter: ['data-dock-group', 'data-group'] });
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
    // A grip sits on its separator's line, so it goes where the separator goes.
    const withGrip = element => element.dataset.part !== 'separator' ? [element]
        : [element, ...parts('grip').filter(grip => grip.dataset.index === element.dataset.index)];
    const show = () => {
        for (const [element, box] of moved) {
            for (const shown of withGrip(element)) {
                for (const side of sides) {
                    shown.style.setProperty(`--tb-dock-live-${names[side]}`, String(box[side]));
                }
            }
        }
    };
    const settle = () => {
        if (Number(root.dataset.report ?? 0) < reports) {
            show();
            return;
        }

        for (const shown of [...moved.keys()].flatMap(withGrip)) {
            for (const side of sides) {
                shown.style.removeProperty(`--tb-dock-live-${names[side]}`);
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
        // A window floats over the docked parts and keeps its box as they resize.
        const others = [...root.children].filter(child => child !== separator
            && ['group', 'panel', 'separator'].includes(child.dataset.part) && !child.hasAttribute('data-floating'));
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
    // The place a separator's edge moves to with a step of this many percent of what its two parts take together, so
    // aria-valuenow moves by as much.
    const stepped = (separator, percent) => {
        const { before, after } = sidesOf(separator);
        const length = vertical(separator) ? 'height' : 'width';
        return edgeOf(separator) + percent * (before[length] + after[length]) / 100;
    };
    // The separator that a press on it, or on its grip's button, drags.
    const pressed = event => {
        const separator = ownPart(event.target, 'separator');
        const grip = separator ? null : ownPart(event.target, 'grip');
        const trigger = grip && event.target.closest('[data-part="trigger"]');
        if (separator || !trigger) {
            return separator ? { separator, grip: null } : null;
        }

        return { separator: parts('separator').find(each => each.dataset.index === grip.dataset.index), grip: trigger };
    };

    // A window moves by the button that starts its row or by the empty part of the row, and resizes by the grip in
    // its corner. Its row, its panels and its grip take the new box at once, as the parts beside a separator do.
    const isRtl = () => getComputedStyle(root).direction === 'rtl';
    const between = (value, low, high) => Math.min(Math.max(value, low), Math.max(low, high));
    const rowOf = index => groupsInOrder().find(each => each.dataset.group === String(index));
    // A window's row, its panels and its grip, which all carry the row's number.
    const windowParts = row => [...root.children].filter(child => child.hasAttribute('data-floating')
        && (child.dataset.group ?? child.dataset.dockGroup) === row.dataset.group);
    // The box after a move, or after a resize, by this many percent of the dock: whole inside the dock, and no
    // smaller than a group may be or than a layout keeps a window.
    const changed = (start, dx, dy, resize) => {
        if (!resize) {
            return { ...start, x: between(start.x + dx, 0, 100 - start.width),
                y: between(start.y + dy, 0, 100 - start.height) };
        }

        const least = Number(root.dataset.minSize);
        return { ...start,
            width: between(start.width + dx, Math.max(10, least / root.clientWidth * 100), 100 - start.x),
            height: between(start.height + dy, Math.max(10, least / root.clientHeight * 100), 100 - start.y) };
    };
    const placeWindow = (row, box) => {
        for (const part of windowParts(row)) {
            moved.set(part, box);
        }

        show();
    };
    const reportWindow = (row, box, resize) => {
        reports++;
        callDotNet(reporter, 'OnFloat', tabsOf(row)[0].dataset.panel, box.x, box.y, box.width, box.height, resize,
            reports);
    };
    // Left and Right are sides of the screen, while a window's box counts from the side where text starts.
    const stepWindow = (row, step, percent) => {
        const [dx, dy] = { left: [-1, 0], right: [1, 0], up: [0, -1], down: [0, 1], wider: [1, 0], narrower: [-1, 0],
            taller: [0, 1], shorter: [0, -1] }[step];
        const resize = ['wider', 'narrower', 'taller', 'shorter'].includes(step);
        const sideways = (step === 'left' || step === 'right') && isRtl() ? -dx : dx;
        const box = changed(boxOf(row), sideways * percent, dy * percent, resize);
        placeWindow(row, box);
        reportWindow(row, box, resize);
    };
    // A slid-out panel resizes by its inner edge, the one away from its strip. Its box grows from the dock's edge by
    // a percent of the dock, and stays between nine tenths of the dock and a tenth, or the smallest a group may be if
    // that is more.
    const slideRow = () => parts('group').find(row => row.hasAttribute('data-edge'));
    const upright = edge => edge === 'start' || edge === 'end';
    const slid = (start, edge, by) => {
        const least = Number(root.dataset.minSize) / (upright(edge) ? root.clientWidth : root.clientHeight) * 100;
        const size = between((upright(edge) ? start.width : start.height) + by, Math.max(10, least), 90);
        return upright(edge) ? { ...start, width: size, x: edge === 'end' ? 100 - size : 0 }
            : { ...start, height: size, y: edge === 'bottom' ? 100 - size : 0 };
    };
    // How far a move across the screen takes the edge away from its strip.
    const outward = (edge, dx, dy) =>
        ({ start: isRtl() ? -dx : dx, end: isRtl() ? dx : -dx, top: dy, bottom: -dy })[edge];
    const reportSlide = (row, box) => {
        reports++;
        callDotNet(reporter, 'OnSlide', tabsOf(row)[0].dataset.panel,
            upright(row.dataset.edge) ? box.width : box.height, reports);
    };
    const stepSlide = by => {
        const row = slideRow();
        if (row) {
            const box = slid(boxOf(row), row.dataset.edge, by);
            placeWindow(row, box);
            reportSlide(row, box);
        }
    };
    // The edge a press is on: the separator itself, which drags at once, or the button of its grip's menu.
    const slideHandle = target => {
        const part = ownPart(target, 'slide-separator') ?? ownPart(target, 'slide-grip');
        const handle = part?.dataset.part === 'slide-grip' ? target.closest('[data-part="trigger"]') : part;
        return handle && slideRow() ? { row: slideRow(), edge: part.dataset.edge, handle, atOnce: handle === part }
            : null;
    };
    // The handle a press or a key is on: the button of a window's Move menu or of its grip, not the menu's items.
    const windowHandle = target => {
        const corner = ownPart(target, 'window-grip');
        const mover = corner ? null : ownPart(target, 'window-move');
        const handle = (corner ?? mover) && target.closest('[data-part="trigger"]');
        return handle ? { row: corner ? rowOf(corner.dataset.group) : ownPart(mover, 'group'),
            resize: Boolean(corner), handle } : null;
    };
    // The window a press moves or resizes: by a handle, or by the empty part of its row.
    const windowPress = event => {
        const row = ownPart(event.target, 'group');
        return slideHandle(event.target) ?? windowHandle(event.target)
            ?? (row?.hasAttribute('data-floating') && !row.hasAttribute('data-edge')
            && event.target.matches('.tb-dock__group, .tb-dock__tab-area, .tb-dock__tabs')
            ? { row, resize: false, handle: null } : null);
    };

    let drag = null;
    let windowDrag = null;
    const grips = createGripDrag(root);
    const onPointerDown = event => {
        grips.pressed();
        const lifted = event.button === 0 ? windowPress(event) : null;
        if (lifted) {
            if (!lifted.handle) {
                // The row's empty part would otherwise start a selection of the tabs' text.
                event.preventDefault();
            }

            if (lifted.atOnce) {
                // As on a separator between groups: the drag selects no text, and the focus shows no highlight.
                event.preventDefault();
                lifted.handle.dataset.pointerFocus = '';
                lifted.handle.focus();
            }

            // Captured where it was pressed, inside the tab menu's area, so that menu still hears the press end.
            (lifted.handle ?? event.target).setPointerCapture(event.pointerId);
            windowDrag = { ...lifted, x: event.clientX, y: event.clientY, start: boxOf(lifted.row), box: null,
                dragged: false };
            return;
        }

        const press = event.button === 0 ? pressed(event) : null;
        if (!press?.separator) {
            return;
        }

        const { separator, grip } = press;
        if (!grip) {
            event.preventDefault();
            // Focus from script counts as keyboard focus, so the mark keeps a press from showing the focus highlight.
            separator.dataset.pointerFocus = '';
            separator.focus();
        }

        (grip ?? separator).setPointerCapture(event.pointerId);
        drag = { separator, grip, grab: along(separator, event) - edgeOf(separator), share: null,
            start: vertical(separator) ? event.clientY : event.clientX, dragged: false };
    };
    const onPointerMove = event => {
        if (windowDrag) {
            const [dx, dy] = [event.clientX - windowDrag.x, event.clientY - windowDrag.y];
            windowDrag.dragged ||= windowDrag.atOnce || grips.isDrag(Math.hypot(dx, dy), event);
            if (windowDrag.dragged && windowDrag.row.isConnected) {
                // A panel's iframe would take the pointer as it crosses.
                root.dataset.resizing = '';
                const [across, down] = [dx / root.clientWidth * 100, dy / root.clientHeight * 100];
                windowDrag.box = windowDrag.edge
                    ? slid(windowDrag.start, windowDrag.edge, outward(windowDrag.edge, across, down))
                    : changed(windowDrag.start, isRtl() ? -across : across, down, windowDrag.resize);
                placeWindow(windowDrag.row, windowDrag.box);
            }

            return;
        }

        if (!drag) {
            return;
        }

        const moved = (vertical(drag.separator) ? event.clientY : event.clientX) - drag.start;
        drag.dragged ||= !drag.grip || grips.isDrag(moved, event);
        if (drag.dragged) {
            // A panel's iframe would take the pointer as it crosses, so the panels let it through while a drag runs.
            root.dataset.resizing = '';
            drag.share = moveTo(drag.separator, along(drag.separator, event) - drag.grab) ?? drag.share;
        }
    };
    const onPointerUp = () => {
        if (windowDrag) {
            const { row, handle, box, resize, dragged, edge } = windowDrag;
            windowDrag = null;
            grips.released(dragged ? handle : null);
            delete root.dataset.resizing;
            // The app can take the window away while it is dragged.
            if (box && row.isConnected) {
                (edge ? reportSlide : reportWindow)(row, box, resize);
            }
        }

        if (drag) {
            grips.released(drag.dragged ? drag.grip : null);
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
            const step = event.shiftKey ? 10 : 1;
            const to = { [grow]: stepped(separator, step), [shrink]: stepped(separator, -step), Home: 0,
                End: 100 }[event.key];
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
    // The arrow keys on a window's Move button move it, and on its grip resize it, by a percent of the dock, or by
    // ten with Shift. This runs before the menu's own keys, which would open it on Up and Down.
    const onWindowKey = event => {
        const way = { ArrowLeft: 'left', ArrowRight: 'right', ArrowUp: 'up', ArrowDown: 'down' }[event.key];
        const pressed = way && !event.altKey && !event.ctrlKey && !event.metaKey && windowHandle(event.target);
        if (!pressed) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        // On the grip, the arrows pull the corner, which sits where the window's box ends.
        const step = !pressed.resize ? way : { left: isRtl() ? 'wider' : 'narrower',
            right: isRtl() ? 'narrower' : 'wider', up: 'shorter', down: 'taller' }[way];
        stepWindow(pressed.row, step, event.shiftKey ? 10 : 1);
    };
    // On a slid-out panel's edge, the arrows that cross it move it by 1% of the dock, or 10% with Shift, and Home and
    // End take the panel to its smallest and its largest.
    const onSlideKey = event => {
        const separator = ownPart(event.target, 'slide-separator');
        if (separator !== event.target || event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        delete separator.dataset.pointerFocus;
        const edge = separator.dataset.edge;
        const arrows = upright(edge) ? { ArrowLeft: outward(edge, -1, 0), ArrowRight: outward(edge, 1, 0) }
            : { ArrowUp: outward(edge, 0, -1), ArrowDown: outward(edge, 0, 1) };
        const by = { Home: -100, End: 100 }[event.key] ?? (arrows[event.key] ?? 0) * (event.shiftKey ? 10 : 1);
        if (by) {
            event.preventDefault();
            stepSlide(by);
        }
    };
    // The browser would take a finger's drag on the empty part of a window's row for scrolling, and end it.
    const onTouchMove = event => {
        if (windowDrag) {
            event.preventDefault();
        }
    };
    const onFocusOut = event =>
        delete (ownPart(event.target, 'separator') ?? ownPart(event.target, 'slide-separator'))?.dataset.pointerFocus;
    // A middle click closes a closable tab, as in a browser. Its press would start the browser's scrolling mode.
    const closable = event => event.button === 1 && ownPart(event.target, 'tab')?.hasAttribute('aria-keyshortcuts')
        ? ownPart(event.target, 'tab') : null;
    // A press on a close mark would focus its tab in some browsers and not in others, so in all of them the focus
    // stays where it is, as for a middle click, and for the right press that opens a tab's menu.
    const onMouseDown = event => {
        if (closable(event) || (event.button === 0 && ownPart(event.target, 'close'))
            || (event.button === 2 && ownPart(event.target, 'tab-area'))) {
            event.preventDefault();
        }
    };
    // A double click on a tab has its group fill the dock, or shows every group again. The first click can close a
    // tab and bring the next one under the pointer, so both clicks have to land on the one tab.
    let clicked = [null, null];
    const onClick = event => {
        clicked = [clicked[1], ownPart(event.target, 'tab')];
    };
    const onDoubleClick = () => {
        const [first, second] = clicked;
        if (second && first === second) {
            callDotNet(reporter, 'OnMaximize', second.dataset.panel);
        }
    };
    // Dragging a tab moves its panel: beside another tab, to a new group on one side of a group or along an edge of
    // the dock, or out into a window of its own. The mark shows where the panel would land, and nothing changes
    // until the drop.
    const mark = parts('drop-mark')[0];
    const edgeReach = 8;
    // Where the pointer is on the row of a window a tab is pulled out into, in pixels from where the row starts and
    // from its top, and the window's width and height in percent of the dock, which are those Float gives.
    const pulledOut = { along: 48, down: 16, size: 50 };
    let carried = null;
    let landing = null;
    const inside = (box, x, y) => x >= box.left && x <= box.right && y >= box.top && y <= box.bottom;
    const tabsOf = group => [...group.querySelectorAll('.tb-dock__tabs > .tb-dock__tab')];
    // The side of a box that a point is nearest, when it is within reach of it.
    const nearSide = (box, x, y, reachX, reachY) => {
        const gaps = [['left', (x - box.left) / reachX], ['right', (box.right - x) / reachX],
            ['top', (y - box.top) / reachY], ['bottom', (box.bottom - y) / reachY]];
        const [side, gap] = gaps.reduce((nearest, each) => each[1] < nearest[1] ? each : nearest);
        return gap < 1 ? side : null;
    };
    // This share of a box, along one of its sides.
    const sideOf = (box, side, share) => {
        const [width, height] = [box.width * share, box.height * share];
        return {
            left: side === 'right' ? box.right - width : box.left,
            top: side === 'bottom' ? box.bottom - height : box.top,
            width: side === 'left' || side === 'right' ? width : box.width,
            height: side === 'top' || side === 'bottom' ? height : box.height,
        };
    };
    const positionOn = side => ({ left: isRtl() ? 'End' : 'Start', right: isRtl() ? 'Start' : 'End', top: 'Top',
        bottom: 'Bottom' })[side];
    // The window a tab pulled out of its group would float in: whole inside the dock, with the pointer on its row a
    // little way in from where the row starts.
    const windowAt = (x, y) => {
        const dock = root.getBoundingClientRect();
        const [left, top] = [dock.left + root.clientLeft, dock.top + root.clientTop];
        const along = isRtl() ? left + root.clientWidth - x : x - left;
        const float = changed({ x: (along - pulledOut.along) / root.clientWidth * 100,
            y: (y - top - pulledOut.down) / root.clientHeight * 100, width: pulledOut.size,
            height: pulledOut.size }, 0, 0, false);
        const fromLeft = isRtl() ? 100 - float.x - float.width : float.x;
        return { float, box: { left: left + fromLeft / 100 * root.clientWidth,
            top: top + float.y / 100 * root.clientHeight, width: float.width / 100 * root.clientWidth,
            height: float.height / 100 * root.clientHeight } };
    };
    // Where the carried tab's panel would land from a point: beside a target panel, along an edge of the dock when
    // there is no target, or in a window of its own, with the box its mark takes. Null where a drop would change
    // nothing.
    const landingAt = (x, y) => {
        const dock = root.getBoundingClientRect();
        const groups = parts('group');
        if (!inside(dock, x, y)) {
            return null;
        }

        // The pointer is over the part drawn highest there: a window in front, or else a docked row or panel.
        const over = [...groups, ...parts('panel').filter(panel => !panel.classList.contains('tb-dock__panel--hidden'))]
            .filter(part => inside(part.getBoundingClientRect(), x, y))
            .sort((a, b) => layerOf(b) - layerOf(a))[0];
        // An edge would give the only docked panel the group it has.
        const docked = groups.filter(group => layerOf(group) < 0).flatMap(tabsOf);
        const edge = layerOf(over) < 0 && !(docked.length === 1 && docked[0] === carried)
            && nearSide(dock, x, y, edgeReach, edgeReach);
        if (edge) {
            // With nothing docked, the new group takes the whole dock.
            return { target: null, position: positionOn(edge), box: sideOf(dock, edge, docked.length ? 0.25 : 1) };
        }

        const own = carried.closest('.tb-dock__group');
        const row = over?.dataset.part === 'group' ? over : null;
        if (row) {
            // The tab goes after the last tab whose middle the pointer has passed, or before the first one.
            const all = tabsOf(row);
            const others = all.filter(tab => tab !== carried);
            const passed = others.filter(tab => {
                const box = tab.getBoundingClientRect();
                return isRtl() ? x < box.left + box.width / 2 : x > box.left + box.width / 2;
            }).length;
            if (!others.length || (row === own && passed === all.indexOf(carried))) {
                return null;
            }

            const beside = others[Math.max(passed - 1, 0)];
            const box = beside.getBoundingClientRect();
            const atLeft = (passed === 0) !== isRtl();
            return { target: beside.dataset.panel, position: passed === 0 ? 'Before' : 'After', line: true,
                box: { left: (atLeft ? box.left : box.right) - 1, top: box.top, width: 2, height: box.height } };
        }

        const shown = over;
        const group = shown && groups.find(each => each.dataset.group === shown.dataset.dockGroup);
        const others = group ? tabsOf(group).filter(tab => tab !== carried) : [];
        // A tab alone in its window is where a drop here would put it.
        if (!group || (!others.length && layerOf(group) >= 0)) {
            return null;
        }

        // The group's box is its row and the panel under it.
        const [top, body] = [group.getBoundingClientRect(), shown.getBoundingClientRect()];
        const whole = { left: body.left, right: body.right, top: top.top, bottom: body.bottom, width: body.width,
            height: body.bottom - top.top };
        // A window has no sides to split off, so all of it takes the panel as a tab, and a tab alone in its group has
        // no tabs to split off from.
        const side = others.length && layerOf(group) < 0 && nearSide(body, x, y, body.width / 4, body.height / 4);
        if (side) {
            const beside = others.find(tab => tab.getAttribute('aria-selected') === 'true') ?? others[0];
            return { target: beside.dataset.panel, position: positionOn(side), box: sideOf(whole, side, 0.5) };
        }

        // The middle of the tab's own group pulls its panel out into a window there, and the middle of another
        // group takes the panel as its last tab.
        return group === own ? windowAt(x, y)
            : { target: others.at(-1).dataset.panel, position: 'After', box: whole };
    };
    const showMark = () => {
        if (!landing) {
            delete mark.dataset.show;
            return;
        }

        const dock = root.getBoundingClientRect();
        mark.style.left = `${landing.box.left - dock.left - root.clientLeft}px`;
        mark.style.top = `${landing.box.top - dock.top - root.clientTop}px`;
        mark.style.width = `${landing.box.width}px`;
        mark.style.height = `${landing.box.height}px`;
        mark.dataset.show = landing.line ? 'line' : 'box';
    };
    const endCarry = () => {
        if (carried) {
            delete carried.dataset.dragging;
        }

        carried = landing = null;
        delete root.dataset.moving;
        showMark();
    };
    const tabDrag = createPointerDrag(root, {
        itemSelector: '.tb-dock__tab',
        ignoreSelector: '[data-part="close"]',
        ownsPress: target => Boolean(ownPart(target, 'tab')),
        scrolls: false,
        onStart: tab => {
            carried = tab;
            tab.dataset.dragging = '';
            // A panel's iframe would take the pointer as it crosses, so the panels let it through meanwhile.
            root.dataset.moving = '';
        },
        onMove: event => {
            landing = landingAt(event.clientX, event.clientY);
            showMark();
        },
        onDrop: () => {
            const [panel, to] = [carried.dataset.panel, landing];
            endCarry();
            if (to?.float) {
                callDotNet(reporter, 'OnPullOut', panel, to.float.x, to.float.y, to.float.width, to.float.height);
            } else if (to) {
                callDotNet(reporter, 'OnMove', panel, to.target, to.position);
            }
        },
        onCancel: endCarry,
    });

    // The tab menu that last opened and what held the focus then. The menu gives the focus back a moment after it
    // closes, and in WebAssembly the dock asks who holds the focus before that, so until the menu has closed this is
    // the answer. A menu whose group has gone never tells the dock that it closed, so it only counts while it's on
    // the page.
    let beforeMenu = null;
    const ownTabMenu = menu =>
        menu.matches('.tb-dock__tab-area > [role="menu"]') && menu.closest('.tb-dock') === root;
    // A finger resting longer than a pickup opens the tab's menu, which takes the press from the drag.
    const onMenuOpening = event => {
        if (event.newState === 'open' && ownTabMenu(event.target)) {
            beforeMenu = { menu: event.target, holder: document.activeElement };
            tabDrag.cancel();
        }
    };
    const onMenuToggled = event => {
        if (event.newState === 'closed' && ownTabMenu(event.target)) {
            beforeMenu = null;
        }
    };

    const holdsFocus = tab => {
        const holder = beforeMenu?.menu.isConnected ? beforeMenu.holder : document.activeElement;
        return tab === holder || Boolean(document.getElementById(tab.getAttribute('aria-controls'))?.contains(holder));
    };
    // A press or the focus inside a window that isn't in front brings it forward.
    const onEngage = event => {
        // A slid-out panel and the panels the strips keep hidden are drawn as windows, in front of them all.
        const floating = [...root.children].filter(child => child.hasAttribute('data-floating')
            && !child.hasAttribute('data-edge') && !child.classList.contains('tb-dock__panel--hidden'));
        const part = floating.find(child => child.contains(event.target));
        if (part && layerOf(part) < Math.max(...floating.map(layerOf))) {
            const group = groupsInOrder().find(each =>
                each.dataset.group === (part.dataset.dockGroup ?? part.dataset.group));
            callDotNet(reporter, 'OnRaise', tabsOf(group)[0].dataset.panel);
        }
    };
    // A slid-out panel slides back on Escape, and when a press or the focus goes anywhere but it and the strips.
    const slidOut = () => [...root.children].filter(child => child.hasAttribute('data-edge'));
    const onRevealedKey = event => {
        if (event.key === 'Escape' && !event.defaultPrevented && !root.hasAttribute('data-moving')
            && !event.target.closest('[role="menu"]') && slidOut().some(part => part.contains(event.target))) {
            callDotNet(reporter, 'OnConceal', true);
        }
    };
    const onElsewhere = event => {
        const parts = slidOut();
        const strip = event.target.closest?.('[data-part="strip"]');
        if (parts.length && !parts.some(part => part.contains(event.target))
            && strip?.parentElement !== root.parentElement) {
            callDotNet(reporter, 'OnConceal', false);
        }
    };
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
    root.addEventListener('keydown', onWindowKey, { capture: true });
    root.addEventListener('touchmove', onTouchMove, { passive: false });
    root.addEventListener('focusout', onFocusOut);
    root.addEventListener('mousedown', onMouseDown);
    root.addEventListener('auxclick', onAuxClick);
    root.addEventListener('click', onClick);
    root.addEventListener('keydown', onRevealedKey);
    root.addEventListener('keydown', onSlideKey);
    document.addEventListener('pointerdown', onElsewhere, { capture: true });
    document.addEventListener('focusin', onElsewhere, { capture: true });
    root.addEventListener('dblclick', onDoubleClick);
    root.addEventListener('pointerdown', onEngage, { capture: true });
    root.addEventListener('focusin', onEngage, { capture: true });
    // A popover's toggle events don't bubble, so they are heard on their way down.
    root.addEventListener('beforetoggle', onMenuOpening, { capture: true });
    root.addEventListener('toggle', onMenuToggled, { capture: true });

    // Marks that the keys and drags work, which tests wait for.
    root.dataset.ready = '';
    return {
        // The layout saved under the key, as text. Storage can be turned off.
        saved(key) {
            try {
                return localStorage.getItem(storageKey(key));
            } catch {
                return null;
            }
        },
        // Storage can be full or turned off, and then the layout just isn't kept.
        save(key, layout) {
            try {
                localStorage.setItem(storageKey(key), layout);
            } catch {
            }
        },
        // A grip's menu moves its separator by this many percent, as its keys do.
        step(index, percent) {
            const separator = parts('separator').find(each => Number(each.dataset.index) === index);
            if (separator) {
                report(separator, moveTo(separator, stepped(separator, percent)));
            }
        },
        // The menu of the grip on a slid-out panel's edge moves the edge by this many percent, as its keys do.
        stepSlide,
        // A window's Move menu and its grip's menu move or resize it by a tenth of the dock.
        stepWindow(index, step) {
            const row = rowOf(index);
            if (row) {
                stepWindow(row, step, 10);
            }
        },
        // Whether the tab with this id, or its panel, holds the focus. The dock asks when the tab's close mark is
        // clicked, or Close is chosen in its own menu.
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
            root.removeEventListener('keydown', onWindowKey, { capture: true });
            root.removeEventListener('touchmove', onTouchMove);
            root.removeEventListener('focusout', onFocusOut);
            root.removeEventListener('mousedown', onMouseDown);
            root.removeEventListener('auxclick', onAuxClick);
            root.removeEventListener('click', onClick);
            root.removeEventListener('keydown', onRevealedKey);
            root.removeEventListener('keydown', onSlideKey);
            document.removeEventListener('pointerdown', onElsewhere, { capture: true });
            document.removeEventListener('focusin', onElsewhere, { capture: true });
            root.removeEventListener('dblclick', onDoubleClick);
            root.removeEventListener('pointerdown', onEngage, { capture: true });
            root.removeEventListener('focusin', onEngage, { capture: true });
            root.removeEventListener('beforetoggle', onMenuOpening, { capture: true });
            root.removeEventListener('toggle', onMenuToggled, { capture: true });
            grips.dispose();
            tabDrag.dispose();
        },
    };
}
