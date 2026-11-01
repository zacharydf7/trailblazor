const unnamed = option => !option.getAttribute('aria-label') && !option.textContent.trim()
    && !option.querySelector('[alt]:not([alt=""]), [aria-label], [title]');

// An option that shows no text, such as an image with no alt, has no name a screen reader can say, so the developer
// hears about it once per list, for options that arrive later too, such as ones that load.
export function watchForUnnamedOptions(list) {
    const check = () => {
        if ([...list.querySelectorAll('[role="option"]')].some(unnamed)) {
            observer.disconnect();
            console.warn('Trailblazor: an option shows no text and has no Text, so screen readers can\'t name it. '
                + 'Give the TbOption a Text.', list);
        }
    };
    const observer = new MutationObserver(check);
    observer.observe(list, { childList: true, subtree: true });
    check();
    return observer;
}
