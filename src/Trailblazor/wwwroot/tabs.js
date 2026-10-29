// The arrow keys, Home and End move focus between enabled tabs; with automatic activation they also select it.
export function attachTabs(list) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!list) {
        return null;
    }

    const onKeyDown = event => {
        const tabs = [...list.querySelectorAll(':scope > [role="tab"]:not(:disabled)')];
        const index = tabs.indexOf(event.target);
        if (index < 0) {
            return;
        }

        const forward = getComputedStyle(list).direction === 'rtl' ? 'ArrowLeft' : 'ArrowRight';
        const back = forward === 'ArrowRight' ? 'ArrowLeft' : 'ArrowRight';
        const next = {
            [forward]: tabs[(index + 1) % tabs.length],
            [back]: tabs[(index - 1 + tabs.length) % tabs.length],
            Home: tabs[0],
            End: tabs.at(-1),
        }[event.key];
        if (!next) {
            return;
        }

        event.preventDefault();
        next.focus();
        if (list.dataset.activation === 'automatic') {
            next.click();
        }
    };

    list.addEventListener('keydown', onKeyDown);
    // Horizontal is already a tab list's default; setting it marks that the arrow keys are attached.
    list.setAttribute('aria-orientation', 'horizontal');
    return {
        dispose() {
            list.removeEventListener('keydown', onKeyDown);
        },
    };
}
