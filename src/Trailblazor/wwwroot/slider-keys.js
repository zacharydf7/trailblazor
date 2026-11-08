// A right-to-left range input's value rises leftward, but WebKit doesn't swap its Left and Right keys, so they're handled
// here the same in every browser: Left raises the value and Right lowers it.
const ranges = '.tb-slider input[type="range"], input.tb-color-picker__slider';

document.addEventListener('keydown', event => {
    const input = event.target;
    if (event.defaultPrevented || (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') || event.altKey
        || event.ctrlKey || event.metaKey || !input.matches?.(ranges) || getComputedStyle(input).direction !== 'rtl') {
        return;
    }

    event.preventDefault();
    const before = input.value;
    if (event.key === 'ArrowLeft') {
        input.stepUp();
    } else {
        input.stepDown();
    }

    if (input.value !== before) {
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }
});
