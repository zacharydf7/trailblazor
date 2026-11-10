// Dragging, the keys and the grip's menu resize panes in the browser, keeping every pane's share, and report the new
// sizes when they stop; Enter asks the server to collapse or restore. `reporter` hears both, and the panes' order in
// the markup, and `key` names the saved layout. Panes are named by id in every report.
export function attachSplitter(root, reporter, key) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    const storageKey = key ? `tb-splitter:${key}` : null;
    const vertical = () => root.classList.contains('tb-splitter--vertical');
    const panes = () => [...root.children].filter(child => child.dataset.part === 'pane');
    const length = element => {
        const box = element.getBoundingClientRect();
        return vertical() ? box.height : box.width;
    };
    const shares = () => {
        const lengths = panes().map(length);
        const total = lengths.reduce((sum, value) => sum + value, 0);
        return lengths.map(value => total > 0 ? value / total * 100 : 0);
    };
    const limit = (pane, name) => Number(pane.dataset[name]);
    // Moves the separator's two panes by delta percent from the given shares, within both panes' limits.
    const move = (separator, from, delta) => {
        const list = panes();
        const before = list.indexOf(separator.parentElement.previousElementSibling);
        const after = before + 1;
        if (before < 0 || after >= list.length || list[before].hasAttribute('data-collapsed')
            || list[after].hasAttribute('data-collapsed')) {
            return null;
        }

        const pair = from[before] + from[after];
        let first = Math.min(Math.max(from[before] + delta, limit(list[before], 'min')), limit(list[before], 'max'));
        first = pair - Math.min(Math.max(pair - first, limit(list[after], 'min')), limit(list[after], 'max'));
        const next = [...from];
        next[before] = Math.max(first, 0);
        next[after] = pair - next[before];
        list.forEach((pane, index) => {
            pane.style.flexGrow = String(next[index]);
        });
        separator.setAttribute('aria-valuenow', String(Math.round(next[before])));
        return next;
    };
    // Keys can come faster than the server renders the sizes they report, and the render for an earlier key would move
    // the panes back, so while a report is on its way the keys start from the sizes last reported. data-reporting
    // tells a test the app doesn't have the sizes yet.
    let reported = null;
    let reporting = 0;
    const report = sizes => {
        if (sizes) {
            reported = sizes;
            reporting++;
            root.dataset.reporting = '';
            reporter.invokeMethodAsync('OnResize', panes().map(pane => pane.id),
                sizes.map(size => Math.round(size * 100) / 100)).finally(() => {
                if (--reporting === 0) {
                    delete root.dataset.reporting;
                }
            });
        }
    };
    // The separator that a press on it, or on its grip's button, drags, unless it belongs to a nested splitter.
    const separatorFor = target => {
        const pressed = target.closest?.('[data-part="separator"], [data-part="grip"] [data-part="trigger"]');
        const bar = pressed?.closest('[data-part="bar"]');
        return bar?.parentElement === root ? bar.querySelector(':scope > [data-part="separator"]') : null;
    };

    let drag = null;
    // The grip a drag was released on, whose click would open its menu. A touch drag sends no click, so the next
    // press clears it.
    let draggedGrip = null;
    const onPointerDown = event => {
        draggedGrip = null;
        const separator = separatorFor(event.target);
        if (!separator || event.button !== 0) {
            return;
        }

        const pressed = separator === event.target ? separator : event.target.closest('[data-part="trigger"]');
        if (pressed === separator) {
            event.preventDefault();
            separator.focus();
        }

        pressed.setPointerCapture(event.pointerId);
        const start = vertical() ? event.clientY : event.clientX;
        drag = { separator, grip: pressed === separator ? null : pressed, from: shares(), start, sizes: null,
            dragged: false };
    };
    const onPointerMove = event => {
        if (!drag) {
            return;
        }

        const moved = (vertical() ? event.clientY : event.clientX) - drag.start;
        const ltr = vertical() || getComputedStyle(root).direction !== 'rtl';
        // A tap on the grip wanders a few pixels, more with a finger, and still opens its menu without resizing.
        drag.dragged ||= Math.abs(moved) >= (event.pointerType === 'mouse' ? 4 : 10);
        if (drag.grip && !drag.dragged) {
            return;
        }

        drag.sizes = move(drag.separator, drag.from, (ltr ? moved : -moved) / length(root) * 100) ?? drag.sizes;
    };
    const onPointerUp = () => {
        if (drag) {
            draggedGrip = drag.dragged ? drag.grip : null;
            report(drag.sizes);
            drag = null;
        }
    };
    // A drag's release would open the grip's menu, so that click goes nowhere.
    const onClick = event => {
        if (draggedGrip?.contains(event.target)) {
            event.preventDefault();
            event.stopPropagation();
        }

        draggedGrip = null;
    };
    const onKeyDown = event => {
        if (separatorFor(event.target) !== event.target || event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        const separator = event.target;
        const rtl = !vertical() && getComputedStyle(root).direction === 'rtl';
        const grow = vertical() ? 'ArrowDown' : rtl ? 'ArrowLeft' : 'ArrowRight';
        const shrink = vertical() ? 'ArrowUp' : rtl ? 'ArrowRight' : 'ArrowLeft';
        const step = event.shiftKey ? 10 : 1;
        const from = reporting > 0 && reported?.length === panes().length ? reported : shares();
        let sizes;
        switch (event.key) {
            case grow:
                sizes = move(separator, from, step);
                break;
            case shrink:
                sizes = move(separator, from, -step);
                break;
            case 'Home':
                sizes = move(separator, from, -100);
                break;
            case 'End':
                sizes = move(separator, from, 100);
                break;
            case 'Enter':
                reporter.invokeMethodAsync('OnToggle', separator.parentElement.previousElementSibling.id);
                event.preventDefault();
                return;
            default:
                return;
        }

        event.preventDefault();
        report(sizes);
    };

    root.addEventListener('pointerdown', onPointerDown);
    root.addEventListener('pointermove', onPointerMove);
    root.addEventListener('pointerup', onPointerUp);
    root.addEventListener('pointercancel', onPointerUp);
    root.addEventListener('keydown', onKeyDown);
    root.addEventListener('click', onClick, { capture: true });

    // Panes register on the server as they first render, so one added later in the middle needs its place reported.
    let order = '';
    const reportOrder = () => {
        const ids = panes().map(pane => pane.id);
        if (ids.join(' ') !== order) {
            order = ids.join(' ');
            reporter.invokeMethodAsync('OnOrder', ids);
        }
    };
    const observer = new MutationObserver(reportOrder);
    observer.observe(root, { childList: true });
    reportOrder();

    // A collapsed pane is hidden, and focus inside it would fall to the page. An observer runs before the browser drops
    // that focus, and moves it to the pane's separator: the one after it, or the one before it for the last pane. A
    // collapse or restore also changes the sizes outside the reports, so the keys measure them again.
    const collapses = new MutationObserver(records => {
        reported = null;
        for (const { target: pane } of records) {
            if (panes().includes(pane) && pane.hasAttribute('data-collapsed') && pane.contains(document.activeElement)) {
                const bar = pane.nextElementSibling ?? pane.previousElementSibling;
                bar?.querySelector(':scope > [data-part="separator"]').focus();
            }
        }
    });
    collapses.observe(root, { subtree: true, attributes: true, attributeFilter: ['data-collapsed'] });

    // A saved layout is read back once so the server can apply it. Anything else under the key, such as a layout
    // from an older version or another splitter, is ignored.
    let saved = null;
    if (storageKey) {
        try {
            const value = JSON.parse(localStorage.getItem(storageKey) ?? 'null');
            const count = panes().length;
            if (Array.isArray(value?.sizes) && Array.isArray(value?.collapsed) && value.sizes.length === count
                && value.collapsed.length === count
                && value.sizes.every(size => typeof size === 'number' && Number.isFinite(size) && size >= 0)
                && value.collapsed.every(flag => typeof flag === 'boolean')) {
                saved = value;
            }
        } catch {
            saved = null;
        }
    }

    // Marks that dragging and the keys work, which tests wait for.
    root.dataset.ready = '';
    return {
        saved: () => saved,
        // The grip's menu moves a separator by delta percent, as its keys do.
        step(separator, delta) {
            report(move(separator, shares(), delta));
        },
        save(sizes, collapsed) {
            // Storage can be full or turned off, and then the layout just isn't kept.
            try {
                localStorage.setItem(storageKey, JSON.stringify({ sizes, collapsed }));
            } catch {
            }
        },
        dispose() {
            observer.disconnect();
            collapses.disconnect();
            root.removeEventListener('pointerdown', onPointerDown);
            root.removeEventListener('pointermove', onPointerMove);
            root.removeEventListener('pointerup', onPointerUp);
            root.removeEventListener('pointercancel', onPointerUp);
            root.removeEventListener('keydown', onKeyDown);
            root.removeEventListener('click', onClick, { capture: true });
        },
    };
}
