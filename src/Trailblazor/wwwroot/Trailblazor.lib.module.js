// Blazor runs this initializer itself, so consumers add no script tags.
import '../Trailblazor.Primitives/pointer-origin.js';
import './read-only.js';
import './select-early-click.js';
import { watchPageWidths } from './number-field.js';
import { focusMarksOnPage } from './autofocus.js';
import './force-load.js';
import './password-reveal.js';
import './form.js';
import { focusMarkedForms, forgetMarks } from './static-form.js';
import './file-upload.js';
import './accordion.js';
import './pagination.js';
import { settleStaticPanelStops } from './tabs.js';
import './lightbox.js';
import './tree-select.js';
import './field-box.js';
import './range-slider.js';
import './slider-keys.js';
import './chip.js';
import './tooltip.js';
import './throttled-input.js';
import './debounced-input.js';
import './text-count.js';
import './message-hold.js';
import { measurePageScrollbar } from './page-scroll.js';
import { isApple } from '../Trailblazor.Primitives/platform.js';
// Every module that another one imports loads here too, so a component's module finds its imports already loaded.
import '../Trailblazor.Primitives/anchored-position.js';
import '../Trailblazor.Primitives/composing.js';
import '../Trailblazor.Primitives/dotnet-call.js';
import '../Trailblazor.Primitives/focus-scope.js';
import '../Trailblazor.Primitives/grip-drag.js';
import '../Trailblazor.Primitives/pointer-drag.js';
import '../Trailblazor.Primitives/roving-focus.js';
import './browser.js';
import './date-grid.js';
import './dialog.js';
import './field.js';
import './inert.js';
import { forwardToo } from './pop-out.js';
import './ribbon-key-tips.js';
import './option-filter.js';
import './overlay.js';
import './shortcuts.js';
import './toolbar.js';
import './typeahead.js';
import './unnamed-options.js';

// data-tb-script tells a test on a static page that its controls work; data-tb-apple makes the styles show Mod as ⌘.
// data-tb-navigated says an enhanced navigation has left the first page. TbRoot's mark speaks only for that page when
// interactivity is set page by page, so the mark comes off, and a test passes over it once this is set, even when the
// mark first renders afterward. Blazor also raises enhancedload on the first page, as its streamed content lands, so
// only a new address counts. This module can load after a navigation, while the session starts, so the first page is
// the one the browser loaded the document for.
const address = url => url.pathname + url.search;
const firstPage = address(new URL(performance.getEntriesByType('navigation')[0]?.name ?? location.href));
let navigated = false;
const markRoot = () => {
    document.documentElement.dataset.tbScript = '';
    navigated ||= address(location) !== firstPage;
    if (navigated) {
        document.documentElement.dataset.tbNavigated = '';
        document.querySelectorAll('[data-tb-interactive][data-tb-session]')
            .forEach(mark => mark.removeAttribute('data-tb-interactive'));
    }

    if (isApple()) {
        document.documentElement.dataset.tbApple = '';
    }
};
markRoot();

// Blazor Web Apps call afterWebStarted; standalone WebAssembly and older hosts call afterStarted.
export function afterWebStarted(blazor) {
    registerEvents(blazor);
    // An enhanced navigation copies the server's <html>, which has no theme, so the one the page showed goes back.
    blazor.addEventListener('enhancedload', () => {
        if (globalThis.trailblazorTheme) {
            Object.assign(document.documentElement.dataset, globalThis.trailblazorTheme);
        }

        // The navigation removed the root's inline style and marks too, and the root may not have resized.
        measurePageScrollbar();
        markRoot();
        watchPageWidths();
        settleStaticPanelStops();
        forgetMarks();
        // AutoFocus first, so a failed post's first error takes focus last.
        focusMarksOnPage();
        focusMarkedForms();
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
    // Blazor listens for each of these on this page's document, so a panel that shows in a browser window of its own
    // has to send them on from there.
    const register = (name, options) => {
        forwardToo(options.browserEventName);
        blazor.registerCustomEventType(name, options);
    };

    // A number input reports text it can't parse as an empty value; badInput tells that apart from a cleared field.
    // Going to or from such text in an empty field fires no change event, so leaving the field reports too.
    const numberEventArgs = event => ({ value: event.target.value, badInput: event.target.validity.badInput });
    register('tbnumberchange', { browserEventName: 'change', createEventArgs: numberEventArgs });
    register('tbnumberleave', { browserEventName: 'focusout', createEventArgs: numberEventArgs });
    register('tbnumberinput', { browserEventName: 'input', createEventArgs: numberEventArgs });
    // number-field.js sends these for Page Up and Page Down, and for a held step button. The browser event has a name
    // of its own, since one matching the Blazor event's name would reach the handler twice.
    register('tbnumberstep',
        { browserEventName: 'tb-number-step', createEventArgs: event => event.detail });

    // A field's OnFocus and OnBlur count only focus that crosses its root, not moves between its own parts. A data
    // grid's edit around such a field has a root of its own, and Blazor hands every handler the same arguments, so
    // each root gets its own answer.
    const fieldFocusEventArgs = event => {
        const crosses = root => !root || !root.contains(event.relatedTarget);
        return {
            crossesField: crosses(event.target.closest?.('[data-tb-focus-root]')),
            crossesEdit: crosses(event.target.closest?.('[data-tb-focus-root="edit"]')),
        };
    };
    register('tbfocusenter',
        { browserEventName: 'focusin', createEventArgs: fieldFocusEventArgs });
    // A text field's OnEnter hears an Enter pressed and released in the same field, after the change it commits; the
    // server hears no other key. An Enter that ends an input method composition doesn't count, and Safari marks it only
    // with keyCode 229.
    let enterDown = null;
    document.addEventListener('keydown', event => {
        enterDown = event.key === 'Enter' && !event.isComposing && event.keyCode !== 229 ? event.target : null;
    }, true);
    document.addEventListener('keyup', event => {
        if (event.key === 'Enter' && event.target === enterDown) {
            enterDown = null;
            event.target.dispatchEvent(new CustomEvent('tb-enter', { bubbles: true }));
        }
    }, true);
    register('tbenter', { browserEventName: 'tb-enter' });
    register('tbfocusleave',
        { browserEventName: 'focusout', createEventArgs: fieldFocusEventArgs });
    // A label can't take focus, so pressing one sends focus to the page, then back when its click focuses the box.
    // In a field that already holds focus, the press leaves focus where it is, so moving between options isn't leaving.
    // A link or other control inside a label still takes focus when pressed.
    const ownPress = 'a[href], button, input, select, textarea, summary, [tabindex], [contenteditable]';
    document.addEventListener('mousedown', event => {
        const root = event.target.closest?.('[data-tb-focus-root]');
        const label = event.target.closest?.('label');
        if (event.button === 0 && root?.contains(document.activeElement) && label
            && !label.contains(event.target.closest(ownPress))) {
            event.preventDefault();
        }
    }, true);

    // A press on a field's show-password button leaves focus, and the caret, in the field.
    document.addEventListener('mousedown', event => {
        if (event.button === 0 && event.target.closest?.('.tb-field [data-part="reveal"]')?.closest('.tb-field')
            ?.contains(document.activeElement)) {
            event.preventDefault();
        }
    }, true);

    // A click on a data grid row says whether the row should ignore it, as when it landed on a control in the row,
    // whose click is the control's own.
    const rowControls = 'a[href], button, input, select, textarea, label, summary, [contenteditable], '
        + '[role="button"], [role="checkbox"], [role="combobox"], [role="switch"], [role="link"]';
    // A drag ends in a click where it was released. Enter clicks a row with detail 0, so it never counts as a drag.
    const dragThreshold = 4;
    let pressedAt = null;
    document.addEventListener('pointerdown', event => pressedAt = { x: event.clientX, y: event.clientY }, true);
    const endsTextDrag = event => event.detail === 1 && !document.getSelection().isCollapsed
        && pressedAt !== null && Math.hypot(event.clientX - pressedAt.x, event.clientY - pressedAt.y) >= dragThreshold;
    // A press that selected cells, by dragging across them or with Shift, ends in a click the row ignores.
    const cellGesture = event => !!event.target.closest?.('table[data-cell-gesture]');
    // The second click of a double-click leaves the row alone, so it doesn't toggle the row's selection back or raise
    // its click again. So does a drag that selects the row's text or its cells, and any click in an edit, such as
    // choosing an option.
    register('tbrowclick', {
        browserEventName: 'click',
        createEventArgs: event => ({
            ignored: !!event.target.closest?.(rowControls) || !!event.target.closest?.('[data-part=editor]')
                || event.detail > 1 || endsTextDrag(event) || event.detail > 0 && cellGesture(event),
            shiftKey: event.shiftKey,
        }),
    });
    // The grid's script raises these only for their keys and clicks, so other keys cost no trip to the server.
    // Each browser event's name differs from the registered one, since Blazor would otherwise handle it twice.
    register('tbrowextend', {
        browserEventName: 'tb-row-extend',
        createEventArgs: event => ({ from: event.detail }),
    });
    register('tbcellrange', {
        browserEventName: 'tb-cell-range',
        createEventArgs: event => event.detail,
    });
    register('tbcellscopy', { browserEventName: 'tb-cells-copy', createEventArgs: () => ({}) });
    register('tbsignaturestroke', {
        browserEventName: 'tb-signature-stroke',
        createEventArgs: event => ({ points: event.detail.points }),
    });
    register('tbcolumnwindow', {
        browserEventName: 'tb-column-window',
        createEventArgs: event => event.detail,
    });
    register('tbtreedrop', {
        browserEventName: 'tb-tree-drop',
        createEventArgs: event => event.detail,
    });
    register('tbcontextmenuopen', {
        browserEventName: 'tb-context-menu-open',
        createEventArgs: event => ({ key: event.detail.key, request: event.detail.request }),
    });
    register('tbcolumnwidths', {
        browserEventName: 'tb-column-widths',
        createEventArgs: event => ({ widths: event.detail.widths }),
    });
    register('tbcolumnorder', {
        browserEventName: 'tb-column-order',
        createEventArgs: event => ({ order: event.detail.order }),
    });
    register('tbselectall', { browserEventName: 'tb-select-all', createEventArgs: () => ({}) });
    register('tbhotkey', { browserEventName: 'tb-hotkey', createEventArgs: () => ({}) });
    register('tbcharactersleft',
        { browserEventName: 'tb-characters-left', createEventArgs: event => ({ left: event.detail }) });
    register('tbtreefilterdown', {
        browserEventName: 'tb-tree-filter-down',
        createEventArgs: () => ({}),
    });
    register('tbrichtextchange', {
        browserEventName: 'tb-rich-text-change',
        createEventArgs: event => ({ html: event.detail.html, continues: event.detail.continues }),
    });
    register('tbrichtextpaste', {
        browserEventName: 'tb-rich-text-paste',
        createEventArgs: event => ({ html: event.detail.html, continues: event.detail.continues }),
    });
    register('tbrichtextlink', {
        browserEventName: 'tb-rich-text-link',
        createEventArgs: event => ({ link: event.detail.link, insert: event.detail.insert }),
    });
    register('tbtagadd', {
        browserEventName: 'tb-tag-add',
        createEventArgs: event => ({ text: event.detail.text }),
    });
    register('tbtagremove', {
        browserEventName: 'tb-tag-remove',
        createEventArgs: event => ({ key: event.detail.key, focus: event.detail.focus }),
    });
    register('tbeditstart', { browserEventName: 'tb-edit-start', createEventArgs: () => ({}) });

    register('tbeditkey', {
        browserEventName: 'tb-edit-key',
        createEventArgs: event => ({
            key: event.detail.key,
            isComposing: event.detail.isComposing,
            handled: event.detail.handled,
        }),
    });
}
