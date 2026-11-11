import { createRovingFocus } from '../Trailblazor.Primitives/roving-focus.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';

// A disabled link stays out unless it was kept focusable, and a select's combobox is its control unless it's turned off.
const controls = 'button, a[href], a[data-kept-focusable], input:not([type="hidden"]), select, textarea, '
    + '[role="combobox"]:not([aria-disabled="true"])';

// The toolbar is one tab stop whose arrow keys move between its controls. When it is too narrow, its toolbar buttons
// move into the overflow menu, last ones first; the browser hides them at once and tells `overflow` which ones moved.
export function attachToolbar(toolbar, more, overflow) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!toolbar) {
        return null;
    }

    // A control inside a menu's popup belongs to the menu, which has keys of its own.
    const inPopup = control => {
        const popup = control.closest('[popover]');
        return popup !== null && popup !== toolbar && toolbar.contains(popup);
    };
    const focus = createRovingFocus(toolbar, {
        items: controls,
        // Moved buttons, and the menu button while nothing has moved, are hidden and can't take focus.
        filter: control => !inPopup(control) && control.checkVisibility(),
        vertical: () => toolbar.getAttribute('aria-orientation') === 'vertical',
    });

    let reported = '';
    const overflows = () => toolbar.scrollWidth > toolbar.clientWidth;
    const fit = () => {
        if (!more) {
            focus.sync();
            return;
        }

        const focused = document.activeElement;
        const items = [...toolbar.querySelectorAll('[data-toolbar-item]')].filter(item => !inPopup(item));
        for (const item of items) {
            item.removeAttribute('data-overflowed');
        }

        more.removeAttribute('data-shown');
        if (overflows()) {
            more.setAttribute('data-shown', '');
            for (const item of [...items].reverse()) {
                item.setAttribute('data-overflowed', '');
                if (!overflows()) {
                    break;
                }
            }
        }

        focus.sync();
        // Focus can't stay on a hidden control, so it goes to the menu button, or to the last control still shown.
        if (toolbar.contains(focused) && !focused.checkVisibility()) {
            focus.focusable().at(-1)?.focus();
        }

        const moved = items.filter(item => item.hasAttribute('data-overflowed')).map(item => item.dataset.toolbarItem);
        if (moved.join(' ') !== reported) {
            reported = moved.join(' ');
            callDotNet(overflow, 'OnOverflow', moved);
        }
    };

    let frame = 0;
    const fitSoon = () => {
        cancelAnimationFrame(frame);
        frame = requestAnimationFrame(fit);
    };
    // Items change size as fonts load or text changes, so each one is watched along with the toolbar.
    const resizes = new ResizeObserver(fitSoon);
    const watch = () => {
        resizes.disconnect();
        resizes.observe(toolbar);
        for (const child of toolbar.children) {
            resizes.observe(child);
        }
    };
    const changes = new MutationObserver(() => {
        watch();
        fitSoon();
    });
    changes.observe(toolbar, { childList: true });
    watch();
    fit();
    // Marks that the keys work and the first fit is done, which tests wait for.
    toolbar.dataset.ready = '';

    return {
        // Moved buttons come back, so a script attached in its place starts from the toolbar as rendered.
        dispose() {
            cancelAnimationFrame(frame);
            resizes.disconnect();
            changes.disconnect();
            focus.dispose();
            toolbar.querySelectorAll('[data-overflowed]').forEach(item => item.removeAttribute('data-overflowed'));
            more?.removeAttribute('data-shown');
            delete toolbar.dataset.ready;
        },
    };
}
