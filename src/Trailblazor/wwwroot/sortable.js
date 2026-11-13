import { createPointerDrag } from '../Trailblazor.Primitives/pointer-drag.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';

const moveKeys = ['ArrowUp', 'ArrowDown', 'Home', 'End'];

// Moves the items of a list, or the rows of a grid's body, by dragging a handle or with the arrow keys on it. The
// server makes each move, so this only names the item, by its key, and where it went: before or after another item.
export function attachSortable(list, events, itemSelector = '[data-part="item"]') {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!list) {
        return null;
    }

    const root = list.closest('.tb-sortable, .tb-data-grid') ?? list;
    const items = (of = list) => [...of.children].filter(item => item.matches(itemSelector));
    // The lists of this list's group, which an item can move between, in the order they appear.
    const group = list.dataset.group;
    const groupLists = () => group
        ? [...document.querySelectorAll('[data-sortable]')].filter(other => other.dataset.group === group) : [];
    // A list or grid inside an item has handles and items of its own, which are its business.
    const ownItem = element => {
        const item = element?.closest('[data-sortable]') === list ? element.closest(itemSelector) : null;
        return item?.parentElement === list ? item : null;
    };
    const handleOf = element => element?.closest('[data-part="handle"]');
    const triggerOf = item => item?.querySelector('[data-part="handle"] [data-part="trigger"]');
    let source = null;
    let over = null;
    // The handle a drag was released on, whose click would open its menu. A touch drag sends no click, so the next
    // press or key clears it.
    let draggedHandle = null;

    // A grid renders its reason box only once the app handles moves, so it is looked up when needed.
    const reason = (of = root) => of.querySelector(':scope > [data-part="drop-reason"]');

    // A drag over another list of the group marks that list, or its item, and shows a refusal in its own box.
    const clearDrop = () => {
        const marked = over?.list ? [over.list, ...items(over.list)] : [];
        [...items(), ...marked].forEach(element => {
            delete element.dataset.drop;
            delete element.dataset.dropRefused;
        });
        for (const box of [reason(), over?.list && reason(over.list.closest('.tb-sortable'))]) {
            if (box) {
                box.hidden = true;
            }
        }

        over = null;
    };

    const showRefusal = (element, text, of = root) => {
        element.dataset.dropRefused = '';
        const box = reason(of);
        if (box && text) {
            box.textContent = text;
            box.style.insetBlockStart = `${element.getBoundingClientRect().bottom - of.getBoundingClientRect().top}px`;
            box.hidden = false;
        }
    };

    // Over another list of the group, the item goes before or after the item under the pointer, by the nearest gap
    // between or past its items, or into the list when it's empty.
    const dragOverList = (other, element, event) => {
        let item = element;
        while (item && item.parentElement !== other) {
            item = item.parentElement;
        }

        const middle = element => {
            const box = element.getBoundingClientRect();
            return box.top + box.height / 2;
        };
        const all = items(other);
        const target = (item?.matches(itemSelector) ? item : null)
            ?? all.find(each => event.clientY < middle(each)) ?? all.at(-1) ?? null;
        const before = !!target && event.clientY < middle(target);
        if (over?.list === other && over.item === target && over.before === before) {
            return;
        }

        clearDrop();
        const marked = target ?? other;
        marked.dataset.drop = target ? (before ? 'before' : 'after') : 'end';
        const current = over = { list: other, item: target, before };
        callDotNet(events, 'OnDragOverList', source.dataset.key, other.dataset.listId, target?.dataset.key ?? null,
            before).then(check => {
            if (over === current && check && !check.allowed) {
                showRefusal(marked, check.reason, other.closest('.tb-sortable'));
            }
        });
    };

    // Heard before the menu's own keys, which would open it on the up and down arrows.
    const onKeyDown = event => {
        draggedHandle = null;
        const item = ownItem(event.target.closest?.('[data-part="handle"] [data-part="trigger"]'));
        if (!item || !moveKeys.includes(event.key) || event.altKey || event.ctrlKey || event.metaKey
            || event.shiftKey) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        callDotNet(events, 'OnKey', item.dataset.key, event.key);
    };
    // Left and Right move the item to the list before or after this one in its group, the other way around in
    // right-to-left text. They're heard before the menu's own keys too.
    const onSideKeyDown = event => {
        const item = ownItem(event.target.closest?.('[data-part="handle"] [data-part="trigger"]'));
        if (!item || !group || !['ArrowLeft', 'ArrowRight'].includes(event.key) || event.altKey || event.ctrlKey
            || event.metaKey || event.shiftKey) {
            return;
        }

        const lists = groupLists();
        const forward = (event.key === 'ArrowRight') !== (getComputedStyle(list).direction === 'rtl');
        const target = lists[lists.indexOf(list) + (forward ? 1 : -1)];
        if (target) {
            event.preventDefault();
            event.stopPropagation();
            callDotNet(events, 'OnListKey', item.dataset.key, target.dataset.listId);
        }
    };

    // A drag's release would open the handle's menu, so that click goes nowhere.
    const onClick = event => {
        if (draggedHandle?.contains(event.target)) {
            event.preventDefault();
            event.stopPropagation();
        }

        draggedHandle = null;
    };

    const pointerDrag = createPointerDrag(list, {
        itemSelector: '[data-part="handle"] [data-part="trigger"]:not(:disabled)',
        onStart: trigger => {
            source = ownItem(trigger);
            if (source) {
                source.dataset.dragging = '';
                root.dataset.dragging = '';
            }
        },
        // The item goes before the one under the pointer when it is over that item's first half, else after it.
        onMove: event => {
            // Anywhere over one of this list's items counts, a list inside the item included.
            const element = document.elementFromPoint(event.clientX, event.clientY);
            const other = element?.closest('[data-sortable]');
            if (source && group && other && other !== list && other.dataset.group === group) {
                dragOverList(other, element, event);
                return;
            }

            let item = element;
            while (item && item.parentElement !== list) {
                item = item.parentElement;
            }

            if (!source || !item?.matches(itemSelector) || item === source) {
                clearDrop();
                return;
            }

            const box = item.getBoundingClientRect();
            const before = event.clientY < box.top + box.height / 2;
            const all = items();
            const from = all.indexOf(source);
            const slot = all.indexOf(item) + (before ? 0 : 1);
            if (slot === from || slot === from + 1) {
                clearDrop();
                return;
            }

            if (over?.item === item && over.before === before) {
                return;
            }

            clearDrop();
            item.dataset.drop = before ? 'before' : 'after';
            const current = over = { item, before };
            callDotNet(events, 'OnDragOver', source.dataset.key, item.dataset.key, before).then(check => {
                if (over === current && check && !check.allowed) {
                    showRefusal(item, check.reason);
                }
            });
        },
        onDrop: () => {
            const target = over;
            const moved = source;
            draggedHandle = handleOf(triggerOf(moved));
            endDrag();
            if (target?.list && moved) {
                callDotNet(events, 'OnDropList', moved.dataset.key, target.list.dataset.listId,
                    target.item?.dataset.key ?? null, target.before);
            } else if (target && moved) {
                callDotNet(events, 'OnDrop', moved.dataset.key, target.item.dataset.key, target.before);
            }
        },
        // Escape ends a drag before its release, whose click would open the menu.
        onCancel: () => {
            draggedHandle = handleOf(triggerOf(source));
            endDrag();
        },
    });

    function endDrag() {
        if (source) {
            delete source.dataset.dragging;
        }

        source = null;
        delete root.dataset.dragging;
        clearDrop();
    }

    const onPointerDown = () => {
        draggedHandle = null;
    };

    list.addEventListener('keydown', onKeyDown, { capture: true });
    list.addEventListener('keydown', onSideKeyDown, { capture: true });
    list.addEventListener('click', onClick, { capture: true });
    list.addEventListener('pointerdown', onPointerDown, { capture: true });

    list.closest('.tb-sortable')?.setAttribute('data-ready', '');
    return {
        // Blazor may move the item's element, which takes focus off its handle, so the server gives it back here.
        focusHandle(key) {
            triggerOf(items().find(item => item.dataset.key === key))?.focus();
        },
        dispose() {
            list.removeEventListener('keydown', onKeyDown, { capture: true });
            list.removeEventListener('keydown', onSideKeyDown, { capture: true });
            list.removeEventListener('click', onClick, { capture: true });
            list.removeEventListener('pointerdown', onPointerDown, { capture: true });
            pointerDrag.dispose();
        },
    };
}
