import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { viewOf } from '../Trailblazor.Primitives/pages.js';

// Shows the button once the page, or the element that scrolls, is past the threshold, and takes it back to the top.
// It tells .NET the first time the button shows, which is when people can first see it.
export function attachBackToTop(root, events, scrollContainer, threshold) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    let settings = { scrollContainer, threshold };
    let reported = false;
    // Looked up each time, so a container that renders after the button is still found.
    const container = () => settings.scrollContainer
        ? root.ownerDocument.querySelector(settings.scrollContainer) : null;
    const update = () => {
        const element = container();
        const shown = (element ? element.scrollTop : viewOf(root).scrollY) > settings.threshold;
        root.toggleAttribute('data-shown', shown);
        if (shown && !reported) {
            reported = true;
            callDotNet(events, 'OnShown');
        }
    };

    // Scroll events don't bubble, but a capturing listener on the document hears them all, the page's and every
    // element's.
    document.addEventListener('scroll', update, { capture: true, passive: true });
    update();
    root.dataset.ready = '';

    return {
        set(newScrollContainer, newThreshold) {
            settings = { scrollContainer: newScrollContainer, threshold: newThreshold };
            update();
        },
        // Focus goes to the top too, so the next Tab starts there; an element that can't take focus gets tabindex=-1
        // until it loses focus.
        top(focusTarget) {
            const element = container();
            const still = matchMedia('(prefers-reduced-motion: reduce)').matches;
            (element ?? viewOf(root)).scrollTo({ top: 0, behavior: still ? 'auto' : 'smooth' });
            const page = root.ownerDocument;
            const target = page.querySelector(focusTarget) ?? element ?? page.body;
            if (!target.hasAttribute('tabindex') && target.tabIndex < 0) {
                target.setAttribute('tabindex', '-1');
                target.addEventListener('blur', () => target.removeAttribute('tabindex'), { once: true });
            }

            target.focus({ preventScroll: true });
        },
        dispose() {
            document.removeEventListener('scroll', update, { capture: true });
        },
    };
}
