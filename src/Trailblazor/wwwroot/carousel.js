import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
// Tells the server when focus enters or leaves a carousel whose slides turn by themselves, and when keyboard focus lands
// anywhere in it, which stops the turning until the user starts it again (WAI-ARIA carousel pattern). Focus from a
// pointer press only pauses it. It also tells the server when a move between slides ends.
export function createCarousel(section, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!section) {
        return null;
    }

    // relatedTarget is where focus came from or went; null is outside the page, such as another window.
    const outside = element => !element || !section.contains(element);
    const onFocusIn = event => {
        const fromKeyboard = event.target.matches(':focus-visible');
        if (fromKeyboard || outside(event.relatedTarget)) {
            callDotNet(events, 'OnFocusEnter', fromKeyboard);
        }
    };
    const onFocusOut = event => {
        if (outside(event.relatedTarget)) {
            callDotNet(events, 'OnFocusLeave');
        }
    };

    // A move ends when the slides' own animations settle, finished or canceled, or at once when there are none, such
    // as for reduced motion. The server ignores the end of a move a later turn replaced.
    const slides = section.querySelector(':scope > [data-part=slides]');
    const watchMove = () => {
        const move = slides.dataset.move;
        if (move === undefined) {
            return;
        }

        const animations = [...slides.children].flatMap(slide => slide.getAnimations());
        Promise.allSettled(animations.map(animation => animation.finished))
            .then(() => callDotNet(events, 'OnMoveEnd', Number(move)));
    };
    const moves = new MutationObserver(watchMove);
    moves.observe(slides, { attributes: true, attributeFilter: ['data-move'] });

    section.addEventListener('focusin', onFocusIn);
    section.addEventListener('focusout', onFocusOut);
    // Focus can already be inside, such as from a Tab pressed before the page was interactive.
    if (section.contains(document.activeElement)) {
        onFocusIn({ target: document.activeElement, relatedTarget: null });
    }

    return {
        // Someone who asks the system for reduced motion gets slides that start still.
        prefersReducedMotion: () => matchMedia('(prefers-reduced-motion: reduce)').matches,
        dispose() {
            moves.disconnect();
            section.removeEventListener('focusin', onFocusIn);
            section.removeEventListener('focusout', onFocusOut);
        },
    };
}
