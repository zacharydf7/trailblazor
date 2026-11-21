import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { pointerMoved } from '../Trailblazor.Primitives/resting-pointer.js';

// The lists open under the field, one for each level. Focus never leaves the input: the keys move an active option
// across the lists in the browser, and opening or choosing clicks it, which reaches the server.
export function createCascader(input, popup, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!input || !popup) {
        return null;
    }

    const control = input.closest('[data-part="control"]') ?? input.parentElement;
    let position = null;
    let active = null;
    // The level whose list the keys asked to open, which the server draws after the click.
    let opening = null;

    const isOpen = () => popup.matches(':popover-open');
    const choosable = '[role="option"]:not([aria-disabled="true"])';
    const listOf = option => option?.closest('[role="listbox"]');
    const levelOf = option => Number(listOf(option)?.dataset.level ?? 0);
    const optionsIn = list => list ? [...list.querySelectorAll(choosable)] : [];
    const column = level => popup.querySelector(`[data-part="column"][data-level="${level}"]`);

    const setActive = option => {
        active?.removeAttribute('data-active');
        active = option ?? null;
        if (active) {
            active.setAttribute('data-active', '');
            input.setAttribute('aria-activedescendant', active.id);
            active.scrollIntoView({ block: 'nearest', inline: 'nearest' });
        } else {
            input.removeAttribute('aria-activedescendant');
        }
    };
    // With nothing active, the keys start at the deepest open choice, or the chosen one, or the first.
    const start = () => {
        const lists = [...popup.querySelectorAll('[role="listbox"]')];
        for (const list of lists.reverse()) {
            const marked = list.querySelector(`${choosable}[aria-selected="true"], ${choosable}[data-open]`);
            if (marked) {
                return marked;
            }
        }

        return optionsIn(popup.querySelector('[role="listbox"]'))[0];
    };
    const open = () => {
        if (input.disabled || input.readOnly || isOpen()) {
            return;
        }

        popup.showPopover();
        document.addEventListener('pointerdown', onDocumentPointerDown, true);
        document.addEventListener('keydown', onDocumentKeyDown, true);
        position = createAnchoredPosition(popup, control, { side: 'Bottom', align: 'Start', offset: 4 });
        input.setAttribute('aria-expanded', 'true');
        callDotNet(events, 'OnToggle', true);
    };
    const close = () => {
        if (!isOpen()) {
            return;
        }

        popup.hidePopover();
        document.removeEventListener('pointerdown', onDocumentPointerDown, true);
        document.removeEventListener('keydown', onDocumentKeyDown, true);
        position?.dispose();
        position = null;
        opening = null;
        setActive(null);
        input.setAttribute('aria-expanded', 'false');
        callDotNet(events, 'OnToggle', false);
    };
    const move = step => {
        const list = optionsIn(listOf(active) ?? popup.querySelector('[role="listbox"]'));
        if (list.length === 0) {
            return;
        }

        const index = list.indexOf(active);
        setActive(index < 0 ? (step > 0 ? list[0] : list.at(-1)) : list[(index + step + list.length) % list.length]);
    };
    // A choice with more under it opens its list; the keys then move into it once the server has drawn it.
    const openNext = () => {
        if (!active?.hasAttribute('data-more')) {
            return;
        }

        opening = levelOf(active) + 1;
        if (active.hasAttribute('data-open') && column(opening)) {
            moveInto(opening);
            return;
        }

        callDotNet(events, 'OnOpen', levelOf(active), Number(active.dataset.index));
    };
    const moveInto = level => {
        const first = optionsIn(column(level))[0];
        if (first) {
            opening = null;
            setActive(first);
        }
    };

    const onKeyDown = event => {
        if (input.readOnly || isComposing(event)) {
            return;
        }

        const searching = popup.hasAttribute('data-searching');
        // In a right-to-left page the next level opens to the left, so the arrows swap.
        const rtl = getComputedStyle(input).direction === 'rtl';
        const key = rtl && event.key === 'ArrowLeft' ? 'ArrowRight'
            : rtl && event.key === 'ArrowRight' ? 'ArrowLeft' : event.key;
        switch (key) {
            case 'ArrowDown':
            case 'ArrowUp':
                if (!isOpen()) {
                    open();
                    setActive(start());
                    if (event.altKey || active) {
                        break;
                    }
                }

                move(event.key === 'ArrowDown' ? 1 : -1);
                break;
            case 'Home':
            case 'End':
                if (!isOpen() || !active) {
                    return;
                }

                setActive(event.key === 'Home' ? optionsIn(listOf(active))[0] : optionsIn(listOf(active)).at(-1));
                break;
            case 'ArrowRight':
                if (!isOpen() || searching || !active) {
                    return;
                }

                openNext();
                break;
            case 'ArrowLeft':
                if (!isOpen() || searching || !active || levelOf(active) === 0) {
                    return;
                }

                setActive(column(levelOf(active) - 1)?.querySelector('[data-open]'));
                break;
            case 'Enter':
                // With nothing active, Enter closes the lists, and the browser's change event settles what was typed.
                if (!isOpen() || !active) {
                    close();
                    return;
                }

                // A choice that can't be the value only opens; one that can, as with AllowParents, is chosen.
                if (!searching && active.hasAttribute('data-more') && !active.hasAttribute('data-choosable')) {
                    openNext();
                    break;
                }

                active.click();
                break;
            default:
                return;
        }

        event.preventDefault();
    };
    const onInput = () => {
        setActive(null);
        open();
    };
    const onInputClick = () => {
        if (!isOpen()) {
            open();
        }
    };
    const onDocumentPointerDown = event => {
        if (!control.contains(event.target) && !popup.contains(event.target)) {
            close();
        }
    };
    // Added only while the lists are open, so Escape on a closed field reaches what surrounds it, such as a dialog.
    const onDocumentKeyDown = event => {
        if (event.key === 'Escape' && !isComposing(event)) {
            event.preventDefault();
            close();
        }
    };
    const onFocusOut = event => {
        if (!popup.contains(event.relatedTarget)) {
            close();
        }
    };
    // Pressing a choice would take focus from the input, which closes the lists before the click arrives.
    const onPopupPointerDown = event => event.preventDefault();
    // Choosing the end of a path, or a search result, closes the lists; opening a choice keeps them open.
    const onPopupClick = event => {
        const option = event.target.closest(choosable);
        if (!option) {
            return;
        }

        setActive(option);
        const endsPath = option.hasAttribute('data-choosable') && !option.hasAttribute('data-more');
        if (popup.hasAttribute('data-searching') || endsPath) {
            close();
        }
    };
    const onPopupPointerMove = event => {
        const option = event.target.closest(choosable);
        if (option && option !== active && pointerMoved(event)) {
            setActive(option);
        }
    };
    // Watches for the server drawing a list the keys opened, or removing the active option as a search starts or
    // ends.
    const observer = new MutationObserver(() => {
        if (opening !== null && column(opening)) {
            moveInto(opening);
        } else if (active && !popup.contains(active)) {
            setActive(null);
        }
    });

    observer.observe(popup, { childList: true, subtree: true });
    input.addEventListener('input', onInput);
    input.addEventListener('keydown', onKeyDown);
    input.addEventListener('click', onInputClick);
    input.addEventListener('focusout', onFocusOut);
    popup.addEventListener('pointerdown', onPopupPointerDown);
    popup.addEventListener('click', onPopupClick);
    popup.addEventListener('pointermove', onPopupPointerMove);
    // Marks that the keys work, which tests wait for.
    input.dataset.ready = '';

    return {
        dispose() {
            close();
            observer.disconnect();
            input.removeEventListener('input', onInput);
            input.removeEventListener('keydown', onKeyDown);
            input.removeEventListener('click', onInputClick);
            input.removeEventListener('focusout', onFocusOut);
            popup.removeEventListener('pointerdown', onPopupPointerDown);
            popup.removeEventListener('click', onPopupClick);
            popup.removeEventListener('pointermove', onPopupPointerMove);
        },
    };
}
