// The theme lives on <html>, which Blazor doesn't render; theme-head.js puts a remembered one back on the next visit.
const root = document.documentElement;

export function read() {
    return [root.dataset.tbTheme ?? null, root.dataset.tbPalette ?? null, root.dataset.tbDensity ?? null];
}

export function apply(name, value, remember) {
    root.dataset[name] = value;
    (globalThis.trailblazorTheme ??= {})[name] = value;

    if (!remember) {
        return;
    }

    try {
        localStorage.setItem(`trailblazor.${name}`, value);
    } catch {
        // Storage can be off, as in a private window; the choice then lasts until the page reloads.
    }
}
