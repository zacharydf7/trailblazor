import { scrollIntoView } from './browser.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';

const settleDelay = 150;

// Tells `events` which section is in view, by its id, as the page scrolls, and scrolls to a section smoothly when its
// link is followed, unless the user asks for less motion.
export function attachTableOfContents(root, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    const links = () => [...root.querySelectorAll('a[data-target]')];

    // The nearest ancestor that scrolls the sections, or the page.
    const scrollerOf = element => {
        for (let parent = element.parentElement; parent && parent !== document.body; parent = parent.parentElement) {
            const overflow = getComputedStyle(parent).overflowY;
            if ((overflow === 'auto' || overflow === 'scroll') && parent.scrollHeight > parent.clientHeight) {
                return parent;
            }
        }

        return document.scrollingElement;
    };
    const viewOf = scroller => scroller === document.scrollingElement
        ? { top: 0, bottom: window.innerHeight }
        : scroller.getBoundingClientRect();

    // A section is in view once its start passes the top quarter of what scrolls it. At the top of the scroll, and
    // before any section has passed that line, the first section is in view if it shows. At the bottom, where the last
    // sections can't reach the line, the last section that shows is in view, or, when none shows, the last one past
    // the line.
    const inView = () => {
        const sections = links().map(link => document.getElementById(link.dataset.target)).filter(Boolean);
        if (sections.length === 0) {
            return null;
        }

        const scroller = scrollerOf(sections[0]);
        const box = viewOf(scroller);
        const tops = sections.map(section => section.getBoundingClientRect().top);
        const shows = index => tops[index] >= box.top && tops[index] < box.bottom;
        if (scroller.scrollTop <= 0) {
            return shows(0) ? sections[0] : null;
        }

        const atEnd = scroller.scrollTop + scroller.clientHeight >= scroller.scrollHeight - 2;
        const line = box.top + (box.bottom - box.top) / 4;
        let index = atEnd ? tops.findLastIndex((_, at) => shows(at)) : -1;
        if (index < 0) {
            index = tops.findLastIndex(top => top <= line);
        }

        return index >= 0 ? sections[index] : shows(0) ? sections[0] : null;
    };

    // A followed link stays marked during and after its scroll, even where another section would win by position,
    // until its section's scroller scrolls again. Its own scroll settles once that scroller has been still for a moment,
    // and a user scroll that began before then has taken over if the section no longer shows.
    let followed = null;
    let settling = 0;
    const overlapsView = section => {
        const box = viewOf(scrollerOf(section));
        const { top, bottom } = section.getBoundingClientRect();
        return bottom > box.top && top < box.bottom;
    };
    const settle = () => {
        clearTimeout(settling);
        settling = setTimeout(() => {
            settling = 0;
            if (followed && !overlapsView(followed)) {
                followed = null;
                schedule();
            }
        }, settleDelay);
    };
    let reported;
    let frame = 0;
    const update = () => {
        frame = 0;
        const id = (followed?.isConnected ? followed : inView())?.id ?? null;
        if (id !== reported) {
            reported = id;
            callDotNet(events, 'OnActive', id);
        }
    };
    const schedule = () => {
        frame ||= requestAnimationFrame(update);
    };
    const onScroll = event => {
        const scroller = event.target === document ? document.scrollingElement : event.target;
        if (followed && scroller === scrollerOf(followed)) {
            if (settling) {
                settle();
            } else {
                followed = null;
            }
        }

        schedule();
    };

    // Focus moves to the section, so Tab goes on from there as it would after the browser's own jump.
    const onClick = event => {
        const link = event.target.closest('a[data-target]');
        const section = link ? document.getElementById(link.dataset.target) : null;
        if (!section || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
            return;
        }

        event.preventDefault();
        // Replaced, not pushed: an entry Blazor's router didn't make would lack the state it reads on Back.
        history.replaceState(history.state, '', link.href);
        scrollIntoView(section, 'start', true);
        if (section.tabIndex < 0 && !section.hasAttribute('tabindex')) {
            section.tabIndex = -1;
        }

        section.focus({ preventScroll: true });
        followed = section;
        settle();
        schedule();
    };

    // Any scroll counts, since the sections may be in a scrolling panel rather than the page.
    document.addEventListener('scroll', onScroll, { capture: true, passive: true });
    window.addEventListener('resize', schedule);
    root.addEventListener('click', onClick);
    schedule();

    // Marks that the links track the scroll, which tests wait for.
    root.dataset.ready = '';
    return {
        dispose() {
            cancelAnimationFrame(frame);
            clearTimeout(settling);
            document.removeEventListener('scroll', onScroll, { capture: true });
            window.removeEventListener('resize', schedule);
            root.removeEventListener('click', onClick);
        },
    };
}
