// Copying text that TbText shortens, to MaxLength or in the middle, gives its whole text when the selection holds no
// other text, such as a drag across part of it or a triple click on it. A selection that runs on past it copies what
// shows, as anywhere else.
const fullTextOf = text => text.querySelector(':scope > .tb-visually-hidden[data-part="full-text"]');

document.addEventListener('copy', event => {
    const page = event.target.ownerDocument;
    const selection = page.getSelection();
    if (!event.clipboardData || !selection || selection.isCollapsed || selection.rangeCount !== 1) {
        return;
    }

    const range = selection.getRangeAt(0);
    const texts = [...page.querySelectorAll('.tb-text')]
        .filter(text => fullTextOf(text) && range.intersectsNode(text));
    if (texts.length !== 1) {
        return;
    }

    const [text] = texts;
    const outside = [];
    if (range.isPointInRange(text, 0)) {
        const before = range.cloneRange();
        before.setEndBefore(text);
        outside.push(before.toString());
    }

    if (range.isPointInRange(text, text.childNodes.length)) {
        const after = range.cloneRange();
        after.setStartAfter(text);
        outside.push(after.toString());
    }

    if (outside.join('').trim() !== '') {
        return;
    }

    event.clipboardData.setData('text/plain', fullTextOf(text).textContent);
    event.preventDefault();
});
