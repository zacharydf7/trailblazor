import { createPointerDrag } from '../Trailblazor.Primitives/pointer-drag.js';
import { createTypeahead } from './typeahead.js';

// Alt+Shift with an arrow moves the focused row: past its neighbors, into the row above, or out of its parent.
const moveKeys = { ArrowUp: 'MoveUp', ArrowDown: 'MoveDown', ArrowRight: 'Indent', ArrowLeft: 'Outdent' };

// Focus moves among the rows here, at once; the keys that open, close, select or check a row go to the server.
export function createTreeView(tree, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!tree) {
        return null;
    }

    const typing = createTypeahead();
    // A tree inside one of this tree's rows has rows, keys and drags of its own.
    const owns = element => element.closest('.tb-tree') === tree;
    const own = selector => [...tree.querySelectorAll(selector)].filter(owns);
    const rows = () => own('[role="treeitem"][data-key]');
    // The arrows also reach a branch's loading or failed row, which has no key of its own.
    const stops = () => own('[role="treeitem"]');
    const statusRow = '[role="treeitem"]:is([data-part="loading"], [data-part="failed"])';
    const ownerOf = row => {
        const list = stops();
        return list.slice(0, list.indexOf(row)).findLast(other => level(other) < level(row));
    };
    const level = row => Number(row.getAttribute('aria-level'));
    // A virtualized tree renders only some rows, so a row next to another in the page may stand beyond rows left out.
    // It is the true neighbor only when every row between them is rendered; otherwise the server finds the row.
    // Read each time, since Virtualize can turn on after the tree attaches.
    const virtual = () => 'virtual' in tree.dataset;
    const place = row => Number(row.dataset.index);
    const total = () => Number(tree.dataset.rows);
    const joined = (from, to) => {
        const shown = stops();
        return Math.abs(place(to) - place(from)) === Math.abs(shown.indexOf(to) - shown.indexOf(from));
    };

    // Only the tab stop's own end-slot controls are in the tab order, so the tree stays one stop and Tab from a row
    // reaches its actions.
    const syncEnds = () => {
        const stop = own('[role="treeitem"][tabindex="0"]')[0];
        own(':is([data-part="end"], [data-part="insert"], [data-part="failed"]) '
            + ':is(a[href], button, input, select, textarea, [tabindex])')
            .forEach(control => {
                // Writing only real changes keeps the observer below from hearing its own writes forever.
                const wanted = control.closest('[role="treeitem"]') === stop ? 0 : -1;
                if (control.tabIndex !== wanted) {
                    control.tabIndex = wanted;
                }
            });
    };
    const makeStop = target => {
        own('[role="treeitem"][tabindex="0"]').forEach(other => { other.tabIndex = -1; });
        target.tabIndex = 0;
        syncEnds();
    };
    const focusRow = target => {
        makeStop(target);
        if (document.activeElement !== target) {
            target.focus();
        }
    };
    let order = '';
    // Blazor moves a keyed tree item's markup without telling the tree, so the order goes to the server on a change.
    const reportOrder = () => {
        const ids = own('[data-tree-item]').map(item => item.dataset.treeItem);
        if (ids.join() !== order) {
            order = ids.join();
            events.invokeMethodAsync('OnItemOrder', ids);
        }
    };
    // A loading or failed row goes once its branch loads or tries again, so focus in it, or the tab stop if focus has
    // left the tree, moves on to the branch's first row, or back to the row it was for.
    let focusedStatus = null;
    const keepStatusFocus = () => {
        if (!focusedStatus || focusedStatus.row.isConnected) {
            return;
        }

        const { owner } = focusedStatus;
        focusedStatus = null;
        if (!owner?.isConnected) {
            return;
        }

        const list = stops();
        const after = list[list.indexOf(owner) + 1];
        const target = after && level(after) > level(owner) ? after : owner;
        if (document.activeElement === document.body || !document.activeElement) {
            focusRow(target);
        } else {
            makeStop(target);
        }
    };
    const observer = new MutationObserver(() => {
        keepStatusFocus();
        syncEnds();
        reportOrder();
        scheduleView();
    });
    // Every way focus reaches a row, a click included, makes it the tree's one tab stop.
    const onFocusIn = event => {
        if (!owns(event.target)) {
            return;
        }

        const row = event.target.closest(statusRow);
        focusedStatus = row && { row, owner: ownerOf(row) };
        if (event.target.matches('[role="treeitem"][data-key]')) {
            focusRow(event.target);
            events.invokeMethodAsync('OnFocus', event.target.dataset.key);
        } else if (event.target === row) {
            focusRow(row);
        }
    };
    const onKeyDown = event => {
        // Keys pressed in a row's own controls, such as a field in its end slot, are theirs.
        const current = event.target;
        const status = current.matches(statusRow);
        if ((!current.matches('[role="treeitem"][data-key]') && !status) || !owns(current)) {
            return;
        }

        // Right to left, the arrows mirror as the chevrons do, so the server only hears the logical key.
        const mirrored = { ArrowLeft: 'ArrowRight', ArrowRight: 'ArrowLeft' };
        const key = getComputedStyle(tree).direction === 'rtl' ? mirrored[event.key] ?? event.key : event.key;
        if (event.altKey && event.shiftKey && moveKeys[key] && current.matches('[data-draggable]')) {
            event.preventDefault();
            events.invokeMethodAsync('OnKey', current.dataset.key, moveKeys[key], false, false);
            return;
        }

        if (event.altKey) {
            return;
        }

        const list = stops();
        const index = list.indexOf(current);
        const expanded = current.getAttribute('aria-expanded');
        // A status row moves for the row it loads for, whose next row in the server's list is the one after it.
        const id = status ? ownerOf(current)?.dataset.key : current.dataset.key;
        const send = key => events.invokeMethodAsync('OnKey', id, key, event.shiftKey, event.ctrlKey || event.metaKey);
        const find = key => {
            if (!virtual()) {
                return typing.find(rows(), current, key);
            }

            events.invokeMethodAsync('OnFind', current.dataset.key, typing.type(key));
            return null;
        };
        // Moves to a row this tree hasn't rendered are the server's, which renders the row and focuses it.
        const far = key => {
            send(key);
            return null;
        };
        // Only moving focus works on a status row, and Enter on a failed one tries again.
        if (status && !['ArrowDown', 'ArrowUp', 'Home', 'End', 'ArrowLeft'].includes(key)) {
            if (key === 'Enter' && current.matches('[data-part="failed"]')) {
                event.preventDefault();
                current.querySelector('button')?.click();
            }

            return;
        }

        let next = null;
        switch (key) {
            case 'ArrowDown':
                next = list[index + 1];
                if (virtual() && (next ? !joined(current, next) : place(current) < total() - 1)) {
                    next = far('Next');
                }

                break;
            case 'ArrowUp':
                next = list[index - 1];
                if (virtual() && (next ? !joined(current, next) : place(current) > 0)) {
                    next = status ? null : far('Previous');
                }

                break;
            case 'Home':
                next = list[0];
                if (virtual() && place(next) !== 0) {
                    next = far('First');
                }

                break;
            case 'End':
                next = list.at(-1);
                if (virtual() && place(next) !== total() - 1) {
                    next = far('Last');
                }

                break;
            case 'ArrowRight':
                if (expanded === 'false') {
                    send('ArrowRight');
                } else if (expanded === 'true' && list[index + 1] && level(list[index + 1]) > level(current)) {
                    next = list[index + 1];
                }

                break;
            case 'ArrowLeft':
                if (expanded === 'true') {
                    send('ArrowLeft');
                } else {
                    next = ownerOf(current);
                    if (virtual() && level(current) > 1 && !(next && joined(next, current))) {
                        next = status ? null : far('Parent');
                    }
                }

                break;
            case 'Enter':
            case '*':
                send(event.key);
                break;
            case ' ':
                if (typing.typing()) {
                    next = find(' ');
                } else {
                    send(' ');
                }

                break;
            default:
                if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'a') {
                    send('SelectAll');
                } else if (event.key.length === 1 && !event.ctrlKey && !event.metaKey
                    && !('noTypeahead' in tree.dataset)) {
                    next = find(event.key);
                } else {
                    return;
                }
        }

        event.preventDefault();
        if (next) {
            focusRow(next);
            // Shift with an arrow, Home or End extends a multiple selection to the row focus lands on, and sends
            // the row it left in case the range has to start there.
            if (event.shiftKey && ['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key) && next.dataset.key
                && tree.getAttribute('aria-multiselectable') === 'true') {
                events.invokeMethodAsync('OnExtend', next.dataset.key, id);
            } else if ('followsFocus' in tree.dataset && next.dataset.key && !event.ctrlKey && !event.metaKey) {
                events.invokeMethodAsync('OnKey', next.dataset.key, 'Follow', false, false);
            }
        }
    };

    // A drag drops before, after or inside the row under the pointer, by where on the row it is; the server says
    // whether it may, and a refusal shows its reason beside the row.
    const reason = own('[data-part="drop-reason"]')[0];
    let sources = [];
    let over = null;
    const clearDrop = () => {
        own('[data-drop]').forEach(row => {
            delete row.dataset.drop;
            delete row.dataset.dropRefused;
        });
        if (reason) {
            reason.hidden = true;
        }

        over = null;
    };
    const endDrag = () => {
        sources.forEach(row => delete row.dataset.dragging);
        sources = [];
        delete tree.dataset.dragging;
        clearDrop();
    };
    const showRefusal = (row, text) => {
        row.dataset.dropRefused = '';
        if (reason && text) {
            reason.textContent = text;
            reason.style.insetBlockStart = `${row.offsetTop + row.offsetHeight}px`;
            reason.hidden = false;
        }
    };
    const selected = row => row.getAttribute('aria-selected') === 'true';
    const pointerDrag = createPointerDrag(tree, {
        itemSelector: '[role="treeitem"][data-draggable]',
        ownsPress: owns,
        ignoreSelector: ':is([data-part="toggle"], [data-part="check"], [data-part="end"], [data-part="insert"])',
        // Dragging a selected row moves every selected row that can move. While it runs, the insert slots on the
        // rows' edges let the pointer through to the rows.
        onStart: row => {
            sources = selected(row) ? rows().filter(other => selected(other) && other.matches('[data-draggable]'))
                : [row];
            sources.forEach(source => { source.dataset.dragging = ''; });
            tree.dataset.dragging = '';
        },
        onMove: event => {
            let row = document.elementFromPoint(event.clientX, event.clientY)
                ?.closest('[role="treeitem"][data-key]');
            if (!row || !owns(row) || sources.includes(row)) {
                clearDrop();
                return;
            }

            const box = row.getBoundingClientRect();
            const at = (event.clientY - box.top) / box.height;
            let position = at < 0.25 ? 'Before' : at > 0.75 ? 'After' : 'Inside';
            // Below an open row is above its first child, so that is where the drop goes.
            const list = rows();
            const next = list[list.indexOf(row) + 1];
            if (position === 'After' && row.getAttribute('aria-expanded') === 'true' && next
                && level(next) > level(row)) {
                row = next;
                position = 'Before';
            }

            if (sources.includes(row)) {
                clearDrop();
                return;
            }
            if (over?.row === row && over.position === position) {
                return;
            }

            clearDrop();
            row.dataset.drop = position.toLowerCase();
            const current = over = { row, position };
            events.invokeMethodAsync('OnDragOver', sources.map(source => source.dataset.key), row.dataset.key,
                position).then(check => {
                if (over === current && !check.allowed) {
                    showRefusal(row, check.reason);
                }
            });
        },
        // The server checks the drop again, and says why when it refuses.
        onDrop: () => {
            const target = over;
            const moved = sources.map(source => source.dataset.key);
            endDrag();
            if (target) {
                events.invokeMethodAsync('OnDrop', moved, target.row.dataset.key, target.position);
            }
        },
        onCancel: endDrag,
    });

    // A virtualized tree tells the server which rows are in view, with some to spare either side, as the tree or the
    // page scrolls. It tells it again only once the view leaves what was asked for.
    const spare = 10;
    let view = { start: -1, count: 0 };
    let frame = 0;
    const reportView = () => {
        frame = 0;
        const size = Number(tree.dataset.rowSize);
        const box = tree.getBoundingClientRect();
        let top = Math.max(box.top, 0);
        let bottom = Math.min(box.bottom, innerHeight);
        for (let parent = tree.parentElement; parent; parent = parent.parentElement) {
            if (getComputedStyle(parent).overflowY !== 'visible') {
                const clip = parent.getBoundingClientRect();
                top = Math.max(top, clip.top);
                bottom = Math.min(bottom, clip.bottom);
            }
        }

        const origin = box.top + tree.clientTop - tree.scrollTop;
        const first = Math.max(0, Math.floor((top - origin) / size));
        const end = Math.max(first + 1, Math.ceil((bottom - origin) / size));
        if (first >= view.start && end <= view.start + view.count) {
            return;
        }

        view = { start: Math.max(0, first - spare), count: end - first + 2 * spare };
        events.invokeMethodAsync('OnView', view.start, view.count);
    };
    const scheduleView = () => {
        if (virtual() && !frame) {
            frame = requestAnimationFrame(reportView);
        }
    };

    tree.addEventListener('focusin', onFocusIn);
    tree.addEventListener('keydown', onKeyDown);
    observer.observe(tree, { childList: true, subtree: true, attributes: true, attributeFilter: ['tabindex'] });
    // A tree whose box grows, as in a pane being resized, shows more rows without anything scrolling.
    const resize = new ResizeObserver(scheduleView);
    resize.observe(tree);
    document.addEventListener('scroll', scheduleView, { capture: true, passive: true });
    addEventListener('resize', scheduleView);
    syncEnds();
    reportOrder();
    scheduleView();
    return {
        dispose() {
            cancelAnimationFrame(frame);
            resize.disconnect();
            document.removeEventListener('scroll', scheduleView, { capture: true, passive: true });
            removeEventListener('resize', scheduleView);
            pointerDrag.dispose();
            observer.disconnect();
            tree.removeEventListener('focusin', onFocusIn);
            tree.removeEventListener('keydown', onKeyDown);
        },
    };
}
