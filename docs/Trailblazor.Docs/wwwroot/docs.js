// The docs' theme and palette live on <html>, which Blazor doesn't render, and are remembered for the next visit.
export function getLook() {
    const { tbTheme, tbPalette } = document.documentElement.dataset;
    return { theme: tbTheme ?? 'system', palette: tbPalette ?? 'graphite' };
}

export function setLook(name, value) {
    document.documentElement.dataset[name] = value;
    try {
        localStorage.setItem(`docs-${name}`, value);
    } catch {
        // Storage can be off, as in a private window; the choice then lasts until the page reloads.
    }
}
