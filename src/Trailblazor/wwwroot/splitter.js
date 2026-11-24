import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { firstTabbable } from '../Trailblazor.Primitives/focus-scope.js';
import { createGripDrag } from '../Trailblazor.Primitives/grip-drag.js';
import { frames, watchSizes } from '../Trailblazor.Primitives/pages.js';
// Dragging, the keys and the grip's menu resize panes in the browser, keeping every pane's share, and report the new
// sizes when they stop; Enter asks the server to collapse or restore. `reporter` hears both, and the panes' order in
// the markup. Panes are named by id in every report.
export function attachSplitter(root, reporter) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    const storageKey = key => `tb-splitter:${key}`;
    // The pane beside `from`, a pane or a separator's bar, in the same splitter, that shows and can take focus: the
    // nearest one before it when `before` is set, or else the nearest one after it, and the other side when that side
    // has none.
    const shownBeside = (from, before) => {
        const shown = [...from.parentElement.children].filter(child => child !== from
            && child.dataset.part === 'pane' && !child.inert && child.checkVisibility({ visibilityProperty: true }));
        const after = shown.filter(pane => from.compareDocumentPosition(pane) & Node.DOCUMENT_POSITION_FOLLOWING);
        const earlier = shown.filter(pane => !after.includes(pane));
        return before ? earlier.at(-1) ?? after[0] : after[0] ?? earlier.at(-1);
    };
    // Focus that would fall to the page goes into a pane instead: to its first control, or to the pane itself. A pane
    // that isn't focusable of its own is focusable only until focus leaves it.
    const focusInto = pane => {
        const control = firstTabbable(pane);
        if (control) {
            control.focus();
            return;
        }

        if (!pane.hasAttribute('tabindex')) {
            pane.tabIndex = -1;
            pane.addEventListener('blur', () => pane.removeAttribute('tabindex'), { once: true });
        }

        pane.focus();
    };
    // While the splitter is narrower than data-stack-below, its panes go one under the other with nothing to drag.
    // Its width comes from what holds it, so setting data-stacked doesn't change the width being measured. A size
    // change is answered a frame later, since stacking changes the height of what is observed. When stacking hides
    // the separator that has focus, whether this splitter's own or one of a vertical splitter inside a pane, focus
    // moves to the pane before that separator, or the one after it when the one before is collapsed.
    const stack = () => {
        const below = Number(root.dataset.stackBelow);
        root.toggleAttribute('data-stacked', below > 0 && root.getBoundingClientRect().width < below);
        const active = root.ownerDocument.activeElement;
        if (root.contains(active) && active.dataset.part === 'separator' && !active.checkVisibility()) {
            const pane = shownBeside(active.parentElement, true);
            if (pane) {
                focusInto(pane);
            }
        }
    };
    const stackFrame = frames(root, stack);
    const widths = watchSizes(() => stackFrame.request());
    widths.observe(root);
    const stackBelow = new MutationObserver(stack);
    stackBelow.observe(root, { attributes: true, attributeFilter: ['data-stack-below'] });
    stack();
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
    // the panes back, so the keys start from the sizes last reported until the root's data-report shows that report's
    // number. Waiting for the call isn't enough, since it can finish before its render arrives. data-reporting tells a
    // test the app doesn't have the sizes yet.
    let reported = null;
    let reports = 0;
    let reporting = 0;
    const report = sizes => {
        if (sizes) {
            reported = sizes;
            reports++;
            reporting++;
            root.dataset.reporting = '';
            callDotNet(reporter, 'OnResize', panes().map(pane => pane.id),
                sizes.map(size => Math.round(size * 100) / 100), reports).finally(() => {
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
    const grips = createGripDrag(root);
    const onPointerDown = event => {
        grips.pressed();
        const separator = separatorFor(event.target);
        if (!separator || event.button !== 0) {
            return;
        }

        const pressed = separator === event.target ? separator : event.target.closest('[data-part="trigger"]');
        if (pressed === separator) {
            event.preventDefault();
            // Focus from script counts as keyboard focus, so the mark keeps a press from showing the focus highlight.
            separator.dataset.pointerFocus = '';
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
        drag.dragged ||= grips.isDrag(moved, event);
        if (drag.grip && !drag.dragged) {
            return;
        }

        drag.sizes = move(drag.separator, drag.from, (ltr ? moved : -moved) / length(root) * 100) ?? drag.sizes;
    };
    const onPointerUp = () => {
        if (drag) {
            grips.released(drag.dragged ? drag.grip : null);
            report(drag.sizes);
            drag = null;
        }
    };
    const onKeyDown = event => {
        delete separatorFor(event.target)?.dataset.pointerFocus;
        if (separatorFor(event.target) !== event.target || event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        const separator = event.target;
        const rtl = !vertical() && getComputedStyle(root).direction === 'rtl';
        const grow = vertical() ? 'ArrowDown' : rtl ? 'ArrowLeft' : 'ArrowRight';
        const shrink = vertical() ? 'ArrowUp' : rtl ? 'ArrowRight' : 'ArrowLeft';
        const step = event.shiftKey ? 10 : 1;
        const rendered = Number(root.dataset.report ?? 0) >= reports;
        const from = !rendered && reported?.length === panes().length ? reported : shares();
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
                callDotNet(reporter, 'OnToggle', separator.parentElement.previousElementSibling.id);
                event.preventDefault();
                return;
            default:
                return;
        }

        event.preventDefault();
        report(sizes);
    };

    const onFocusOut = event => delete separatorFor(event.target)?.dataset.pointerFocus;

    root.addEventListener('pointerdown', onPointerDown);
    root.addEventListener('pointermove', onPointerMove);
    root.addEventListener('pointerup', onPointerUp);
    root.addEventListener('pointercancel', onPointerUp);
    root.addEventListener('keydown', onKeyDown);
    root.addEventListener('focusout', onFocusOut);

    // Panes register on the server as they first render, so one added later in the middle needs its place reported.
    let order = '';
    const reportOrder = () => {
        const ids = panes().map(pane => pane.id);
        if (ids.join(' ') !== order) {
            order = ids.join(' ');
            callDotNet(reporter, 'OnOrder', ids);
        }
    };
    const observer = new MutationObserver(reportOrder);
    observer.observe(root, { childList: true });
    reportOrder();

    // A collapsed pane is hidden, and focus inside it would fall to the page. An observer runs before the browser drops
    // that focus, and moves it to the pane's separator: the one after it, or the one before it for the last pane.
    // While stacked, the separators are hidden too, so focus goes into the next pane that shows, or the last one
    // before it when none follows. A collapse or restore also changes the sizes outside the reports, so the keys
    // measure them again.
    const collapses = new MutationObserver(records => {
        reported = null;
        for (const { target: pane } of records) {
            if (panes().includes(pane) && pane.hasAttribute('data-collapsed')
                && pane.contains(pane.ownerDocument.activeElement)) {
                const separator = (pane.nextElementSibling ?? pane.previousElementSibling)
                    ?.querySelector(':scope > [data-part="separator"]');
                if (separator?.checkVisibility()) {
                    separator.focus();
                } else {
                    const next = shownBeside(pane, false);
                    if (next) {
                        focusInto(next);
                    }
                }
            }
        }
    });
    collapses.observe(root, { subtree: true, attributes: true, attributeFilter: ['data-collapsed'] });

    // Marks that dragging and the keys work, which tests wait for.
    root.dataset.ready = '';
    return {
        // The layout saved under the key, for the server to apply. Anything else under the key, such as a layout
        // from an older version or another splitter, is ignored.
        saved(key) {
            try {
                const value = JSON.parse(localStorage.getItem(storageKey(key)) ?? 'null');
                const count = panes().length;
                return Array.isArray(value?.sizes) && Array.isArray(value?.collapsed) && value.sizes.length === count
                    && value.collapsed.length === count
                    && value.sizes.every(size => typeof size === 'number' && Number.isFinite(size) && size >= 0)
                    && value.collapsed.every(flag => typeof flag === 'boolean') ? value : null;
            } catch {
                return null;
            }
        },
        // The grip's menu moves a separator by delta percent, as its keys do.
        step(separator, delta) {
            report(move(separator, shares(), delta));
        },
        save(key, sizes, collapsed) {
            // Storage can be full or turned off, and then the layout just isn't kept.
            try {
                localStorage.setItem(storageKey(key), JSON.stringify({ sizes, collapsed }));
            } catch {
            }
        },
        dispose() {
            stackFrame.cancel();
            widths.disconnect();
            stackBelow.disconnect();
            root.removeAttribute('data-stacked');
            observer.disconnect();
            collapses.disconnect();
            root.removeEventListener('pointerdown', onPointerDown);
            root.removeEventListener('pointermove', onPointerMove);
            root.removeEventListener('pointerup', onPointerUp);
            root.removeEventListener('pointercancel', onPointerUp);
            root.removeEventListener('keydown', onKeyDown);
            root.removeEventListener('focusout', onFocusOut);
            grips.dispose();
        },
    };
}
