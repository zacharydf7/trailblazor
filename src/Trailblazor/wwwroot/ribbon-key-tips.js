import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { controls } from './toolbar.js';

// Key tips. Alt, pressed and let go on its own, shows a badge on each control in the tabs' row that has data-key-tip:
// the tabs and what sits beside them. A tab's key selects it and shows the badges of its commands, a command's key
// chooses it, and Escape steps back a level. While its badge shows, a control has data-key-tip-shown and names its key
// in aria-keyshortcuts, and the ribbon's data-key-tips names the level. A control that a key tip reaches says all its
// keys in its description, whether the badges show or not.

const ribbons = new Set();
// Alt went down alone, and nothing has happened since.
let armed = false;
// The ribbon whose key tips show, with the level they're at.
let active = null;

const keyOf = control => control.dataset.keyTip.toUpperCase();
const opens = control => control.matches('[aria-haspopup], [aria-expanded], [popovertarget]');
const enabled = control => !control.disabled && control.getAttribute('aria-disabled') !== 'true';
const usable = control => enabled(control) && control.checkVisibility();
const tabList = root => root.querySelector(':scope > .tb-tabs > [role="tablist"]');
const inPanel = (root, control) => {
    const panel = control.closest('[role="tabpanel"]');
    return panel !== null && root.contains(panel);
};
// A ribbon inside this one, as in a dialog written in a group, has key tips of its own.
const own = (root, control) => control.closest('[data-part="ribbon"]') === root;
const beside = root => [...root.querySelectorAll('[data-key-tip]')]
    .filter(control => own(root, control) && !inPanel(root, control) && usable(control));
// A tab's commands count only once its panel shows, which the server answers a moment after the tab's key.
const commandsOf = (root, tab) => {
    const panel = tab.ownerDocument.getElementById(tab.getAttribute('aria-controls') ?? '');
    return panel && !panel.hidden
        ? [...panel.querySelectorAll('[data-key-tip]')].filter(control => own(root, control) && usable(control)) : null;
};
const showing = state => state.tab ? commandsOf(state.root, state.tab) : beside(state.root);
// A popup of the ribbon's own that is open, such as a collapsed group's or a menu's. A dialog written in the ribbon
// shows as a popup while it isn't modal, and neither it nor a popup inside it or inside another ribbon counts.
const ownPopupOpen = root => [...root.querySelectorAll(':popover-open')].some(popup => popup.localName !== 'dialog'
    && popup.parentElement.closest('[data-part="ribbon"], dialog') === root);

// A ribbon behind a modal dialog is out of reach: a dialog on the page layer makes it inert, and the browser does for
// one in the top layer. Of the others, the ribbon with focus comes first, the innermost if one is inside another, and
// then the first one on the page.
const target = event => {
    // Alt is for the window it was pressed in, which can be one that shows a part of the page.
    const page = event.target.ownerDocument ?? document;
    const top = [...page.querySelectorAll('dialog:modal')].at(-1);
    const candidates = [...ribbons].filter(root => root.ownerDocument === page && !root.closest('[inert]')
        && (!top || top.contains(root)) && root.checkVisibility() && beside(root).length > 0);
    candidates.sort((one, other) => one.compareDocumentPosition(other) & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1);
    // A ribbon comes before the ones inside it, so the last with focus is the innermost.
    return candidates.findLast(root => root.contains(page.activeElement)) ?? candidates[0];
};

const clear = state => {
    for (const [control, shortcuts] of state.kept) {
        control.removeAttribute('data-key-tip-shown');
        if (shortcuts === null) {
            control.removeAttribute('aria-keyshortcuts');
        } else {
            control.setAttribute('aria-keyshortcuts', shortcuts);
        }
    }

    state.kept.clear();
};
const show = state => {
    clear(state);
    for (const control of showing(state) ?? []) {
        state.kept.set(control, control.getAttribute('aria-keyshortcuts'));
        control.setAttribute('data-key-tip-shown', '');
        control.setAttribute('aria-keyshortcuts', keyOf(control));
    }

    state.root.dataset.keyTips = state.tab ? 'commands' : 'tabs';
};

const stop = restoreFocus => {
    const state = active;
    if (!state) {
        return;
    }

    active = null;
    state.changes.disconnect();
    clear(state);
    delete state.root.dataset.keyTips;
    if (restoreFocus && state.returnTo?.isConnected && state.returnTo !== state.returnTo.ownerDocument.body) {
        state.returnTo.focus();
    }
};

const choose = (state, control) => {
    if (control.matches('[role="tab"]') && tabList(state.root)?.contains(control)) {
        state.tab = control;
        control.focus();
        control.click();
        show(state);
    } else if (control.matches('[data-part="group-button"]')) {
        // A collapsed group opens, and its controls' badges show once it has.
        control.click();
    } else if (control.matches('button') && !opens(control)) {
        // Focus goes back first, so a command that acts on what had focus finds it there.
        stop(true);
        control.click();
    } else {
        // A button that opens something, such as a menu's, is pressed and keeps focus. Anything else is a field, or
        // an element around one, and takes focus.
        stop(false);
        (control.matches(controls) ? control : control.querySelector(controls))?.focus();
        if (control.matches('button')) {
            control.click();
        }
    }
};
const press = (state, key) => {
    const controls = showing(state);
    if (controls === null) {
        // Typed before the tab's commands showed, so it waits for them.
        state.pending = key;
        return;
    }

    const control = controls.find(candidate => keyOf(candidate) === key);
    if (control) {
        choose(state, control);
    }
};
const refresh = () => {
    const state = active;
    if (!state) {
        return;
    }

    show(state);
    if (state.pending !== null && showing(state) !== null) {
        const key = state.pending;
        state.pending = null;
        press(state, key);
    }
};
const start = root => {
    const state = {
        root, tab: null, pending: null, returnTo: root.ownerDocument.activeElement, kept: new Map(),
        changes: new MutationObserver(refresh),
    };
    active = state;
    // The badges come first, so the selected tab names its key as it takes focus. Focus in the tabs leaves the arrow
    // keys ready to take over.
    show(state);
    tabList(root)?.querySelector('[role="tab"][tabindex="0"]')?.focus();
    state.changes.observe(root, {
        subtree: true, childList: true, attributes: true,
        attributeFilter: ['aria-selected', 'aria-expanded', 'hidden', 'disabled', 'data-key-tip', 'data-collapsed',
            'data-peek'],
    });
};
// From a tab's commands back to the tabs, and from the tabs out, to where focus was.
const back = state => {
    if (!state.tab) {
        stop(true);
        return;
    }

    const tab = state.tab;
    state.tab = null;
    state.pending = null;
    tab.focus();
    state.root.removeAttribute('data-peek');
    show(state);
};

const onKeyDown = event => {
    if (event.key === 'Alt') {
        if (!event.repeat) {
            armed = !event.ctrlKey && !event.shiftKey && !event.metaKey
                && (active !== null || target(event) !== undefined);
        }

        // Left alone, Alt on its own would go to the browser's menu on Windows and Linux.
        if (armed) {
            event.preventDefault();
        }

        return;
    }

    armed = false;
    if (!active || event.key === 'Shift' || isComposing(event)) {
        return;
    }

    if (event.altKey) {
        // Alt with a key belongs to the browser. Ctrl and Cmd end the key tips as they go down, like any other key.
        stop(false);
    } else if (event.key === 'Escape') {
        // A collapsed group's open popup takes Escape first.
        if (!ownPopupOpen(active.root)) {
            event.preventDefault();
            back(active);
        }
    } else if (event.key.length === 1 && event.key !== ' ') {
        event.preventDefault();
        press(active, event.key.toUpperCase());
    } else {
        // The arrow keys, Tab, Enter and the rest work as they do without key tips, from the control with focus.
        stop(false);
    }
};
const onKeyUp = event => {
    if (event.key !== 'Alt' || !armed) {
        return;
    }

    armed = false;
    event.preventDefault();
    if (active) {
        stop(true);
        return;
    }

    const root = target(event);
    if (root) {
        start(root);
    }
};
// A press anywhere, or the window losing focus, ends the key tips where they are.
const drop = () => {
    armed = false;
    stop(false);
};
const onFocusIn = event => {
    if (active && !active.root.contains(event.target)) {
        stop(false);
    }
};

// The badges are for the eye, so each control that a key tip reaches also says its keys in its description: Alt, its
// tab's key, the key of the collapsed group it is in, and its own. The text is in a hidden element at the end of the
// ribbon, which the control's aria-describedby names after whatever it names already. The ribbon's data-key-tip-label
// words it, with {0} for the keys. The returned function stops following the ribbon.
let notesMade = 0;
const describedBy = control => (control.getAttribute('aria-describedby') ?? '').split(' ').filter(Boolean);
const setDescribedBy = (control, ids) => {
    if (ids.length > 0) {
        control.setAttribute('aria-describedby', ids.join(' '));
    } else {
        control.removeAttribute('aria-describedby');
    }
};
const describeKeys = root => {
    const holder = root.appendChild(root.ownerDocument.createElement('div'));
    holder.hidden = true;
    // The element with each control's keys.
    const notes = new Map();
    const wanted = () => {
        const texts = new Map();
        const format = root.dataset.keyTipLabel;
        if (!format) {
            return texts;
        }

        const add = (control, ...before) => {
            // A key tip on an element around a field is read from the field. The element named by aria-describedby
            // would take the place of an aria-description the app wrote, so that control keeps its own.
            const reader = control.matches(controls) ? control : control.querySelector(controls);
            if (reader && own(root, control) && enabled(control) && enabled(reader)
                && !reader.hasAttribute('aria-description')) {
                texts.set(reader, format.replaceAll('{0}', ['Alt', ...before, keyOf(control)].join(', ')));
            }
        };
        const all = [...root.querySelectorAll('[data-key-tip]')];
        all.filter(control => !inPanel(root, control)).forEach(control => add(control));
        for (const tab of all.filter(control => control.matches('[role="tab"]') && enabled(control))) {
            const panel = root.ownerDocument.getElementById(tab.getAttribute('aria-controls') ?? '');
            for (const control of panel?.querySelectorAll('[data-key-tip]') ?? []) {
                const button = control.closest('[data-part="group"][data-collapsed]')
                    ?.querySelector(':scope > [data-part="group-button"]');
                if (!button || button === control) {
                    add(control, keyOf(tab));
                } else if (button.dataset.keyTip) {
                    add(control, keyOf(tab), keyOf(button));
                }
            }
        }

        return texts;
    };
    // Blazor writes aria-describedby again when a control that has one re-renders, and a tooltip names itself there
    // when it arrives. So each pass names the element last again where it isn't, and changes nothing where it is.
    const update = () => {
        const texts = wanted();
        for (const [control, note] of notes) {
            if (!texts.has(control)) {
                setDescribedBy(control, describedBy(control).filter(id => id !== note.id));
                note.remove();
                notes.delete(control);
            }
        }

        for (const [control, text] of texts) {
            let note = notes.get(control);
            if (!note) {
                note = holder.appendChild(root.ownerDocument.createElement('span'));
                note.id = `tb-key-tip-${++notesMade}`;
                notes.set(control, note);
            }

            if (note.textContent !== text) {
                note.textContent = text;
            }

            if (describedBy(control).at(-1) !== note.id) {
                setDescribedBy(control, [...describedBy(control).filter(id => id !== note.id), note.id]);
            }
        }
    };
    const changes = new MutationObserver(update);
    changes.observe(root, {
        subtree: true, childList: true, attributes: true,
        attributeFilter: ['data-key-tip', 'data-key-tip-label', 'data-collapsed', 'disabled', 'aria-disabled',
            'aria-describedby', 'aria-description'],
    });
    update();
    return () => changes.disconnect();
};

// Turns key tips on for a ribbon; the returned function turns them off again.
export function attachKeyTips(root) {
    if (ribbons.size === 0) {
        document.addEventListener('keydown', onKeyDown, true);
        document.addEventListener('keyup', onKeyUp, true);
        document.addEventListener('pointerdown', drop, true);
        document.addEventListener('focusin', onFocusIn, true);
        window.addEventListener('blur', drop);
    }

    ribbons.add(root);
    const forgetKeys = describeKeys(root);
    return () => {
        if (active?.root === root) {
            stop(false);
        }

        forgetKeys();
        ribbons.delete(root);
        if (ribbons.size === 0) {
            document.removeEventListener('keydown', onKeyDown, true);
            document.removeEventListener('keyup', onKeyUp, true);
            document.removeEventListener('pointerdown', drop, true);
            document.removeEventListener('focusin', onFocusIn, true);
            window.removeEventListener('blur', drop);
        }
    };
}
