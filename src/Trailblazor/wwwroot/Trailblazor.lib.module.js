// Blazor runs this initializer itself, so consumers add no script tags.
import '../Trailblazor.Primitives/pointer-origin.js';
import './read-only.js';
import './number-field.js';
import './autofocus.js';
import './file-upload.js';
import './accordion.js';
import './pagination.js';
import './field-box.js';
import './range-slider.js';
import { measurePageScrollbar } from './page-scroll.js';

// Blazor Web Apps call afterWebStarted; standalone WebAssembly and older hosts call afterStarted.
export function afterWebStarted(blazor) {
    registerEvents(blazor);
    // An enhanced navigation copies the server's <html>, which has no theme, so the one the page showed goes back.
    blazor.addEventListener('enhancedload', () => {
        if (globalThis.trailblazorTheme) {
            Object.assign(document.documentElement.dataset, globalThis.trailblazorTheme);
        }

        // The navigation removed the root's inline style too, and the root may not have resized.
        measurePageScrollbar();
    });
}

export function afterStarted(blazor) {
    registerEvents(blazor);
}

let registered = false;

function registerEvents(blazor) {
    if (registered) {
        return;
    }

    registered = true;

    // A number input reports text it can't parse as an empty value; badInput tells that apart from a cleared field.
    // Going to or from such text in an empty field fires no change event, so leaving the field reports too.
    const numberEventArgs = event => ({ value: event.target.value, badInput: event.target.validity.badInput });
    blazor.registerCustomEventType('tbnumberchange', { browserEventName: 'change', createEventArgs: numberEventArgs });
    blazor.registerCustomEventType('tbnumberleave', { browserEventName: 'focusout', createEventArgs: numberEventArgs });
    blazor.registerCustomEventType('tbnumberinput', { browserEventName: 'input', createEventArgs: numberEventArgs });

    // A field's OnFocus and OnBlur count only focus that crosses its root, not moves between its own parts.
    const fieldFocusEventArgs = event => {
        const root = event.target.closest?.('[data-tb-focus-root]');
        return { crossesField: !root || !root.contains(event.relatedTarget) };
    };
    blazor.registerCustomEventType('tbfocusenter', { browserEventName: 'focusin', createEventArgs: fieldFocusEventArgs });
    blazor.registerCustomEventType('tbfocusleave', { browserEventName: 'focusout', createEventArgs: fieldFocusEventArgs });

    // A click on a data grid row says whether the row should ignore it, as when it landed on a control in the row, whose
    // click is the control's own.
    const rowControls = 'a[href], button, input, select, textarea, label, summary, [contenteditable], '
        + '[role="button"], [role="checkbox"], [role="combobox"], [role="switch"], [role="link"]';
    // A drag ends in a click where it was released. Enter clicks a row with a detail of 0, so it never counts as a drag.
    const dragThreshold = 4;
    let pressedAt = null;
    document.addEventListener('pointerdown', event => pressedAt = { x: event.clientX, y: event.clientY }, true);
    const endsTextDrag = event => event.detail === 1 && !document.getSelection().isCollapsed
        && pressedAt !== null && Math.hypot(event.clientX - pressedAt.x, event.clientY - pressedAt.y) >= dragThreshold;
    // The second click of a double-click leaves the row alone, so it doesn't toggle the row's selection back or raise
    // its click again. So does a drag that selects the row's text, and any click in an edit, such as choosing an option.
    blazor.registerCustomEventType('tbrowclick', {
        browserEventName: 'click',
        createEventArgs: event => ({
            ignored: !!event.target.closest?.(rowControls) || !!event.target.closest?.('[data-part=editor]')
                || event.detail > 1 || endsTextDrag(event),
            shiftKey: event.shiftKey,
        }),
    });
    // The grid's script raises these only for the keys and clicks they're for, so other keys cost no trip to the server.
    // Each browser event's name differs from the registered one, since Blazor would otherwise handle it twice.
    blazor.registerCustomEventType('tbrowextend', {
        browserEventName: 'tb-row-extend',
        createEventArgs: event => ({ from: event.detail }),
    });
    blazor.registerCustomEventType('tbselectall', { browserEventName: 'tb-select-all', createEventArgs: () => ({}) });
    blazor.registerCustomEventType('tbeditstart', { browserEventName: 'tb-edit-start', createEventArgs: () => ({}) });

    // A key in a grid cell's edit says whether its field used it; the grid's own Enter and Escape guards don't count.
    blazor.registerCustomEventType('tbeditkey', {
        browserEventName: 'keydown',
        createEventArgs: event => ({
            key: event.key,
            isComposing: event.isComposing,
            handled: event.defaultPrevented && !event.tbGridGuarded,
        }),
    });
}
