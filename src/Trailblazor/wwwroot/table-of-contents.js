import { scrollIntoView } from './browser.js';

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
        const box = scroller === document.scrollingElement
            ? { top: 0, bottom: window.innerHeight }
            : scroller.getBoundingClientRect();
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

    let reported;
    let frame = 0;
    const update = () => {
        frame = 0;
        const id = inView()?.id ?? null;
        if (id !== reported) {
            reported = id;
            events.invokeMethodAsync('OnActive', id);
        }
    };
    const schedule = () => {
        frame ||= requestAnimationFrame(update);
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
    };

    // Any scroll counts, since the sections may be in a scrolling panel rather than the page.
    document.addEventListener('scroll', schedule, { capture: true, passive: true });
    window.addEventListener('resize', schedule);
    root.addEventListener('click', onClick);
    schedule();

    // Marks that the links track the scroll, which tests wait for.
    root.dataset.ready = '';
    return {
        dispose() {
            cancelAnimationFrame(frame);
            document.removeEventListener('scroll', schedule, { capture: true });
            window.removeEventListener('resize', schedule);
            root.removeEventListener('click', onClick);
        },
    };
}
