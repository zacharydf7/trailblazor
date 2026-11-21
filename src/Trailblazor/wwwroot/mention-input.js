import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { pointerMoved } from '../Trailblazor.Primitives/resting-pointer.js';

// The longest mention text the field looks back over for a trigger.
const longest = 50;

// A mention input's suggestions: a trigger before the caret asks the server to search, the list opens under the caret
// once suggestions land, and the keys move an active one and choose it. Focus never leaves the field, which carries
// aria-activedescendant, as a combobox's input does.
export function createMentionInput(input, listbox, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!input || !listbox) {
        return null;
    }

    const control = input.closest('[data-part="control"]') ?? input.parentElement;
    const caret = control.querySelector('[data-part="caret"]');
    let query = null;
    // Each query is numbered, and the server marks its suggestions with the number of the query they answer, so only
    // the latest query's suggestions can be chosen; until they land, none from the list the new text replaced can be.
    // A script attached again, when the field swaps its input for a text area, counts on from the server's number.
    let asked = Number(listbox.dataset.resultsFor) || 0;
    let askedQuery = false;
    let waiting = false;
    // Enter or Tab pressed before the latest suggestions land chooses the first, unless the query or the focus changes.
    let chooseOnArrival = false;
    let dismissed = null;
    let active = null;
    let position = null;

    const isOpen = () => listbox.matches(':popover-open');
    const options = () => [...listbox.querySelectorAll('[role="option"]')];

    // The trigger and text before the caret, when a trigger starts a word there and no space follows it.
    const find = () => {
        const end = input.selectionStart;
        if (end !== input.selectionEnd || input.readOnly || input.disabled) {
            return null;
        }

        const triggers = input.dataset.triggers ?? '@';
        for (let start = end - 1; start >= 0 && end - start <= longest; start--) {
            const character = input.value[start];
            if (/\s/.test(character)) {
                return null;
            }

            if (triggers.includes(character) && (start === 0 || /[\s([{"']/.test(input.value[start - 1]))) {
                return { trigger: character, start, text: input.value.slice(start + 1, end) };
            }
        }

        return null;
    };
    const update = () => {
        const next = find();
        if (next?.start === query?.start && next?.text === query?.text && next?.trigger === query?.trigger) {
            return;
        }

        query = next && next.start !== dismissed ? next : null;
        chooseOnArrival = false;
        if (query) {
            placeCaret(query.start);
            askedQuery = true;
            waiting = true;
            callDotNet(events, 'OnQuery', query.trigger, query.text, ++asked);
        } else {
            close();
            if (askedQuery) {
                askedQuery = false;
                callDotNet(events, 'OnQuery', null, '', ++asked);
            }
        }
    };

    // The mark sits where the trigger is, so the list opens under it, as it would under a word being typed.
    const placeCaret = index => {
        const point = caretPoint(input, index);
        caret.style.left = `${input.offsetLeft + point.x}px`;
        caret.style.top = `${input.offsetTop + point.y}px`;
        caret.style.height = `${point.height}px`;
    };

    // A one-line field is a combobox, which says whether its list is open; a text area has no such state.
    const expand = open => {
        if (input.getAttribute('role') === 'combobox') {
            input.setAttribute('aria-expanded', String(open));
        }
    };
    const setActive = option => {
        active?.removeAttribute('data-active');
        active = option ?? null;
        if (active) {
            active.setAttribute('data-active', '');
            input.setAttribute('aria-activedescendant', active.id);
            active.scrollIntoView({ block: 'nearest' });
        } else {
            input.removeAttribute('aria-activedescendant');
        }
    };
    const open = () => {
        if (!isOpen()) {
            listbox.showPopover();
            position = createAnchoredPosition(listbox, caret, { side: 'Bottom', align: 'Start', offset: 4 });
            expand(true);
        }

        setActive(options()[0]);
    };
    const close = () => {
        if (isOpen()) {
            listbox.hidePopover();
            position?.dispose();
            position = null;
            expand(false);
        }

        setActive(null);
    };
    const move = step => {
        const list = options();
        const index = list.indexOf(active);
        setActive(list[(index + step + list.length) % list.length]);
    };

    // The server is told first, while it still has the suggestions, and the text then goes in as typing does.
    const choose = option => {
        const index = options().indexOf(option);
        if (!query || index < 0 || waiting) {
            return;
        }

        callDotNet(events, 'OnChosen', index, asked);
        const { start } = query;
        const end = input.selectionStart;
        const text = `${option.dataset.insert} `;
        query = null;
        askedQuery = false;
        close();
        input.focus();
        input.setSelectionRange(start, end);
        if (!input.ownerDocument.execCommand('insertText', false, text)) {
            input.setRangeText(text, start, end, 'end');
            input.dispatchEvent(new Event('input', { bubbles: true }));
        }
    };

    const onInput = () => {
        dismissed = null;
        update();
    };
    const onSelectionChange = () => {
        if (input.ownerDocument.activeElement === input) {
            update();
        }
    };
    const onKeyDown = event => {
        if (!isOpen() || isComposing(event)) {
            return;
        }

        switch (event.key) {
            case 'ArrowDown':
                move(1);
                break;
            case 'ArrowUp':
                move(-1);
                break;
            case 'Enter':
            case 'Tab':
                if (waiting) {
                    chooseOnArrival = true;
                } else {
                    choose(active);
                }

                break;
            case 'Escape':
                // Escape closes the list until the text changes, without leaving a dialog the field is in.
                dismissed = query?.start ?? null;
                query = null;
                close();
                break;
            default:
                return;
        }

        event.preventDefault();
        event.stopPropagation();
    };
    const onFocusOut = event => {
        if (!listbox.contains(event.relatedTarget)) {
            chooseOnArrival = false;
            close();
        }
    };
    // Pressing a suggestion would take focus from the field, which closes the list before the click arrives.
    const onListboxPointerDown = event => event.preventDefault();
    const onListboxClick = event => {
        const option = event.target.closest('[role="option"]');
        if (option) {
            choose(option);
        }
    };
    const onListboxPointerMove = event => {
        const option = event.target.closest('[role="option"]');
        if (option && option !== active && pointerMoved(event)) {
            setActive(option);
        }
    };
    // The latest suggestions have landed when the server marks them with the latest query's number: the list opens
    // with some, and closes without.
    const results = new MutationObserver(() => {
        if (Number(listbox.dataset.resultsFor) !== asked) {
            return;
        }

        waiting = false;
        if (query && options().length > 0 && input.ownerDocument.activeElement === input) {
            open();
            if (chooseOnArrival) {
                choose(active);
            }
        } else {
            close();
        }
    });

    results.observe(listbox, { attributes: true, attributeFilter: ['data-results-for'] });
    input.addEventListener('input', onInput);
    input.addEventListener('keydown', onKeyDown);
    input.addEventListener('focusout', onFocusOut);
    document.addEventListener('selectionchange', onSelectionChange);
    listbox.addEventListener('pointerdown', onListboxPointerDown);
    listbox.addEventListener('click', onListboxClick);
    listbox.addEventListener('pointermove', onListboxPointerMove);
    // Marks that the suggestions work, which tests wait for.
    input.dataset.ready = '';

    return {
        dispose() {
            close();
            results.disconnect();
            input.removeEventListener('input', onInput);
            input.removeEventListener('keydown', onKeyDown);
            input.removeEventListener('focusout', onFocusOut);
            document.removeEventListener('selectionchange', onSelectionChange);
            listbox.removeEventListener('pointerdown', onListboxPointerDown);
            listbox.removeEventListener('click', onListboxClick);
            listbox.removeEventListener('pointermove', onListboxPointerMove);
        },
    };
}

// Where a character of the field's text sits, relative to the field's border box, measured in a hidden copy that wraps
// the text the same way.
function caretPoint(field, index) {
    const style = getComputedStyle(field);
    const copy = document.createElement('div');
    for (const name of ['boxSizing', 'width', 'fontFamily', 'fontSize', 'fontWeight', 'fontStyle', 'letterSpacing',
        'lineHeight', 'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft', 'borderTopWidth',
        'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth', 'textTransform', 'textIndent', 'tabSize',
        'wordSpacing', 'direction']) {
        copy.style[name] = style[name];
    }

    Object.assign(copy.style, {
        position: 'absolute', visibility: 'hidden', top: '0', left: '-9999px', borderStyle: 'solid',
        whiteSpace: field.localName === 'textarea' ? 'pre-wrap' : 'pre', overflowWrap: 'break-word',
    });
    copy.textContent = field.value.slice(0, index);
    const mark = document.createElement('span');
    mark.textContent = field.value.slice(index) || '.';
    copy.append(mark);
    field.ownerDocument.body.append(copy);
    const point = {
        x: mark.offsetLeft - field.scrollLeft,
        y: mark.offsetTop - field.scrollTop,
        height: parseFloat(style.lineHeight) || mark.offsetHeight,
    };
    copy.remove();
    return point;
}
