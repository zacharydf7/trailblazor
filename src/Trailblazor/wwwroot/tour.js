import { createAnchoredPosition } from '../Trailblazor.Primitives/anchored-position.js';
import { createFocusScope } from '../Trailblazor.Primitives/focus-scope.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { blockBehind } from './overlay.js';

// The space between the highlight and the target it rings.
const ring = 6;

// A tour's steps: each one is placed by its target, which is highlighted and scrolled into view, and gets focus so its
// title is read. Focus stays in the step and the page behind is inert until the tour ends, and then focus goes back
// where it was.
export function createTour(root, events, outsidePopups) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    const shade = root.querySelector('[data-part="shade"]');
    const scope = createFocusScope(root, { trapped: true, restoreFocus: true });
    const block = blockBehind(root, false, outsidePopups);
    let position = null;
    let target = null;
    let watch = null;

    // The shade's hole follows the target as the page scrolls or resizes.
    const highlight = () => {
        if (!target) {
            shade.removeAttribute('style');
            return;
        }

        const box = target.getBoundingClientRect();
        Object.assign(shade.style, {
            top: `${box.top - ring}px`, left: `${box.left - ring}px`,
            width: `${box.width + 2 * ring}px`, height: `${box.height + 2 * ring}px`,
        });
    };

    const onKeyDown = event => {
        if (event.key === 'Escape') {
            event.preventDefault();
            callDotNet(events, 'OnEscape');
        }
    };
    root.addEventListener('keydown', onKeyDown);
    window.addEventListener('scroll', highlight, true);
    window.addEventListener('resize', highlight);

    return {
        // Shows the step that just rendered: its target, if it's on the page, or the middle of the window.
        show() {
            const step = root.querySelector('[data-part="step"]');
            position?.dispose();
            watch?.disconnect();
            target = find(step.dataset.target);
            root.toggleAttribute('data-centered', !target);
            if (target) {
                const still = matchMedia('(prefers-reduced-motion: reduce)').matches;
                target.scrollIntoView({ block: 'center', inline: 'nearest', behavior: still ? 'auto' : 'smooth' });
                position = createAnchoredPosition(step, target,
                    { side: step.dataset.side, align: step.dataset.align, offset: ring + 8 });
                watch = new ResizeObserver(highlight);
                watch.observe(target);
            } else {
                position = null;
                step.removeAttribute('style');
            }

            highlight();
            step.focus({ preventScroll: true });
        },
        dispose() {
            position?.dispose();
            watch?.disconnect();
            root.removeEventListener('keydown', onKeyDown);
            window.removeEventListener('scroll', highlight, true);
            window.removeEventListener('resize', highlight);
            block.dispose();
            scope.dispose();
        },
    };
}

// A selector that isn't valid finds nothing, like one for an element that isn't on the page.
function find(selector) {
    try {
        return document.querySelector(selector);
    } catch {
        return null;
    }
}
