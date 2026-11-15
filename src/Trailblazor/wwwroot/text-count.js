// Keeps a text field's counter up with typing, with no round trip, and tells the server when the characters left reach
// one of the field's thresholds, so it announces them. The JS initializer imports this at startup.

// Each field's count at its last update, from when it took focus, so keystrokes landing together still cross a
// threshold.
const lastCount = new WeakMap();
const thresholds = [20, 0];

document.addEventListener('focusin', event => {
    if (event.target.dataset?.count) {
        lastCount.set(event.target, event.target.value.length);
    }
}, true);

// Captured before debounced-input.js holds the event back, and read once the field's own listeners, such as a mask's,
// have changed the text.
document.addEventListener('input', event => {
    const field = event.target;
    if (field.dataset?.count) {
        setTimeout(() => update(field));
    }
}, true);

function update(field) {
    const counter = document.getElementById(field.dataset.count);
    if (!counter) {
        return;
    }

    const count = field.value.length;
    const number = new Intl.NumberFormat(counter.dataset.culture || undefined);
    const limit = counter.dataset.limit === undefined ? null : Number(counter.dataset.limit);
    // Blazor keeps the text nodes it rendered and updates those, so they're changed in place rather than replaced.
    counter.querySelector('[data-part=count-shown]').firstChild.nodeValue =
        limit === null ? number.format(count) : `${number.format(count)} / ${number.format(limit)}`;
    const said = count === 1 && counter.dataset.saidOne !== undefined ? counter.dataset.saidOne : counter.dataset.said;
    counter.querySelector('[data-part=count-said]').firstChild.nodeValue = said.replace('{0}', number.format(count));

    if (limit === null || !lastCount.has(field)) {
        return;
    }

    const was = limit - lastCount.get(field);
    const left = limit - count;
    lastCount.set(field, count);
    if (thresholds.some(threshold => was > threshold && left <= threshold)) {
        field.dispatchEvent(new CustomEvent('tb-characters-left', { bubbles: true, detail: left }));
    }
}
