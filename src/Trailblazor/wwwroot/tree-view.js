import { createTypeahead } from './typeahead.js';

// Focus moves among the rows here, at once; the keys that open, close, select or check a row go to the server.
export function createTreeView(tree, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!tree) {
        return null;
    }

    const typing = createTypeahead();
    const rows = () => [...tree.querySelectorAll('[role="treeitem"][data-key]')];
    const level = row => Number(row.getAttribute('aria-level'));

    // Only the tab stop's own end-slot controls are in the tab order, so the tree stays one stop and Tab from a row
    // reaches its actions.
    const syncEnds = () => {
        const stop = tree.querySelector('[role="treeitem"][tabindex="0"]');
        tree.querySelectorAll('[data-part="end"] :is(a[href], button, input, select, textarea, [tabindex])')
            .forEach(control => {
                // Writing only real changes keeps the observer below from hearing its own writes forever.
                const wanted = control.closest('[role="treeitem"]') === stop ? 0 : -1;
                if (control.tabIndex !== wanted) {
                    control.tabIndex = wanted;
                }
            });
    };
    const focusRow = target => {
        tree.querySelectorAll('[role="treeitem"][tabindex="0"]').forEach(other => { other.tabIndex = -1; });
        target.tabIndex = 0;
        syncEnds();
        if (document.activeElement !== target) {
            target.focus();
        }
    };
    const observer = new MutationObserver(syncEnds);
    // Every way focus reaches a row, a click included, makes it the tree's one tab stop.
    const onFocusIn = event => {
        if (event.target.matches('[role="treeitem"][data-key]')) {
            focusRow(event.target);
            events.invokeMethodAsync('OnFocus', event.target.dataset.key);
        }
    };
    const onKeyDown = event => {
        // Keys pressed in a row's own controls, such as a field in its end slot, are theirs.
        const current = event.target;
        if (!current.matches('[role="treeitem"][data-key]') || event.altKey) {
            return;
        }

        const list = rows();
        const index = list.indexOf(current);
        const expanded = current.getAttribute('aria-expanded');
        const send = key => events.invokeMethodAsync('OnKey', current.dataset.key, key, event.shiftKey,
            event.ctrlKey || event.metaKey);
        let next = null;
        switch (event.key) {
            case 'ArrowDown':
                next = list[index + 1];
                break;
            case 'ArrowUp':
                next = list[index - 1];
                break;
            case 'Home':
                next = list[0];
                break;
            case 'End':
                next = list.at(-1);
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
                    next = list.slice(0, index).findLast(other => level(other) < level(current));
                }

                break;
            case 'Enter':
            case '*':
                send(event.key);
                break;
            case ' ':
                if (typing.typing()) {
                    next = typing.find(list, current, ' ');
                } else {
                    send(' ');
                }

                break;
            default:
                if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'a') {
                    send('SelectAll');
                } else if (event.key.length === 1 && !event.ctrlKey && !event.metaKey) {
                    next = typing.find(list, current, event.key);
                } else {
                    return;
                }
        }

        event.preventDefault();
        if (next) {
            focusRow(next);
            // Shift with an arrow extends a multiple selection to the row focus lands on.
            if (event.shiftKey && (event.key === 'ArrowDown' || event.key === 'ArrowUp')) {
                events.invokeMethodAsync('OnKey', next.dataset.key, 'Extend', true, false);
            }
        }
    };

    tree.addEventListener('focusin', onFocusIn);
    tree.addEventListener('keydown', onKeyDown);
    observer.observe(tree, { childList: true, subtree: true, attributes: true, attributeFilter: ['tabindex'] });
    syncEnds();
    return {
        dispose() {
            observer.disconnect();
            tree.removeEventListener('focusin', onFocusIn);
            tree.removeEventListener('keydown', onKeyDown);
        },
    };
}
