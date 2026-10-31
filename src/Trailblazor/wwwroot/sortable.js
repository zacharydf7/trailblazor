import { createPointerDrag } from '../Trailblazor.Primitives/pointer-drag.js';

const moveKeys = ['ArrowUp', 'ArrowDown', 'Home', 'End'];

// Moves the items of a list, or the rows of a grid's body, by dragging a handle or with the arrow keys on it. The
// server makes each move, so this only names the item, by its key, and where it went: before or after another item.
export function attachSortable(list, events, itemSelector = '[data-part="item"]') {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!list) {
        return null;
    }

    const root = list.closest('.tb-sortable, .tb-data-grid') ?? list;
    const items = () => [...list.children].filter(item => item.matches(itemSelector));
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
    const reason = () => root.querySelector(':scope > [data-part="drop-reason"]');

    const clearDrop = () => {
        items().forEach(item => {
            delete item.dataset.drop;
            delete item.dataset.dropRefused;
        });
        const box = reason();
        if (box) {
            box.hidden = true;
        }

        over = null;
    };

    const showRefusal = (item, text) => {
        item.dataset.dropRefused = '';
        const box = reason();
        if (box && text) {
            box.textContent = text;
            box.style.insetBlockStart = `${item.getBoundingClientRect().bottom - root.getBoundingClientRect().top}px`;
            box.hidden = false;
        }
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
        events.invokeMethodAsync('OnKey', item.dataset.key, event.key);
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
            let item = document.elementFromPoint(event.clientX, event.clientY);
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
            events.invokeMethodAsync('OnDragOver', source.dataset.key, item.dataset.key, before).then(check => {
                if (over === current && !check.allowed) {
                    showRefusal(item, check.reason);
                }
            });
        },
        onDrop: () => {
            const target = over;
            const moved = source;
            draggedHandle = handleOf(triggerOf(moved));
            endDrag();
            if (target && moved) {
                events.invokeMethodAsync('OnDrop', moved.dataset.key, target.item.dataset.key, target.before);
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
    list.addEventListener('click', onClick, { capture: true });
    list.addEventListener('pointerdown', onPointerDown, { capture: true });

    return {
        // Blazor may move the item's element, which takes focus off its handle, so the server gives it back here.
        focusHandle(key) {
            triggerOf(items().find(item => item.dataset.key === key))?.focus();
        },
        dispose() {
            list.removeEventListener('keydown', onKeyDown, { capture: true });
            list.removeEventListener('click', onClick, { capture: true });
            list.removeEventListener('pointerdown', onPointerDown, { capture: true });
            pointerDrag.dispose();
        },
    };
}
