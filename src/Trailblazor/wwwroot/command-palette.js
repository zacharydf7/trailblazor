import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { pointerMoved } from '../Trailblazor.Primitives/resting-pointer.js';

// Focus stays in the search box while the arrow keys move an active option, which Enter clicks, as in a combobox. The
// first option turns active whenever the results change, so Enter right after typing runs the best match.
export function attachPalette(scope) {
    // Blazor passes null for an element that was removed before this call arrived.
    const input = scope?.querySelector('[role="combobox"]');
    const listbox = scope?.querySelector('[role="listbox"]');
    if (!input || !listbox) {
        return null;
    }

    const choosable = '[role="option"]:not([aria-disabled="true"])';
    const options = () => [...listbox.querySelectorAll(choosable)];
    let active = null;

    // Blazor reuses option elements as the results change and doesn't know about these attributes, so the option they
    // were set on is always cleared first.
    const setActive = option => {
        active?.removeAttribute('data-active');
        active?.setAttribute('aria-selected', 'false');
        active = option ?? null;
        if (active) {
            active.setAttribute('data-active', '');
            active.setAttribute('aria-selected', 'true');
            input.setAttribute('aria-activedescendant', active.id);
            active.scrollIntoView({ block: 'nearest' });
        } else {
            input.removeAttribute('aria-activedescendant');
        }
    };
    const move = step => {
        const list = options();
        if (list.length > 0) {
            const index = list.indexOf(active);
            setActive(index < 0 ? list[0] : list[(index + step + list.length) % list.length]);
        }
    };

    const onKeyDown = event => {
        if (isComposing(event)) {
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
                if (!active) {
                    return;
                }

                active.click();
                break;
            default:
                return;
        }

        event.preventDefault();
    };
    // Pressing an option would take focus from the search box.
    const onPointerDown = event => event.preventDefault();
    const onPointerMove = event => {
        const option = event.target.closest(choosable);
        if (option && option !== active && pointerMoved(event)) {
            setActive(option);
        }
    };
    const observer = new MutationObserver(() => setActive(options()[0]));

    observer.observe(listbox, { childList: true, subtree: true, characterData: true });
    input.addEventListener('keydown', onKeyDown);
    listbox.addEventListener('pointerdown', onPointerDown);
    listbox.addEventListener('pointermove', onPointerMove);
    setActive(options()[0]);
    // Marks that the keys work, which tests wait for.
    input.dataset.ready = '';

    return {
        dispose() {
            observer.disconnect();
            input.removeEventListener('keydown', onKeyDown);
            listbox.removeEventListener('pointerdown', onPointerDown);
            listbox.removeEventListener('pointermove', onPointerMove);
        },
    };
}
