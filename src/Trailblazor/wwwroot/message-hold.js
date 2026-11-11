// A field's message can clear as a press elsewhere blurs the field, moving what was pressed out from under the pointer
// so no click follows. A field showing a message as a press starts keeps its height until the press ends.
const held = new Map();
// Long enough for a tap's click, which comes after pointerup.
const releaseDelay = 500;
let releaseTimer;

document.addEventListener('pointerdown', () => {
    clearTimeout(releaseTimer);
    // A file upload can show two messages, so its field is listed once.
    const fields = new Set([...document.querySelectorAll('.tb-field__error')].map(message => message.parentElement));
    const heights = [...fields].filter(field => !held.has(field))
        .map(field => [field, getComputedStyle(field).blockSize]);
    // Every height is read before any is set, so the fields lay out once.
    for (const [field, blockSize] of heights) {
        held.set(field, field.style.minBlockSize);
        field.style.minBlockSize = blockSize;
    }
}, true);

const release = () => {
    clearTimeout(releaseTimer);
    for (const [field, minBlockSize] of held) {
        field.style.minBlockSize = minBlockSize;
    }

    held.clear();
};

// Let go on click rather than pointerup, since a tap's click comes after pointerup and its target is chosen by then.
// A press dragged off its target gets no click, so a timer lets go of that one.
document.addEventListener('click', release, true);
document.addEventListener('pointerup', () => {
    clearTimeout(releaseTimer);
    releaseTimer = setTimeout(release, releaseDelay);
}, true);
document.addEventListener('pointercancel', release, true);
