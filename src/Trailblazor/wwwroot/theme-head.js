// Loaded by TbThemeHead as a plain script in <head>, so it runs before the page paints and a remembered theme never
// flashes in after it. Its own data attributes carry the app's defaults.
(() => {
    const { mode, palette, density, remember } = document.currentScript.dataset;
    const remembered = name => {
        try {
            return remember === 'true' ? localStorage.getItem(`trailblazor.${name}`) : null;
        } catch {
            return null;
        }
    };

    // Kept where theme.js updates it and the initializer puts it back after an enhanced navigation.
    globalThis.trailblazorTheme = {
        tbTheme: remembered('tbTheme') ?? mode,
        tbPalette: remembered('tbPalette') ?? palette,
        tbDensity: remembered('tbDensity') ?? density,
    };
    Object.assign(document.documentElement.dataset, globalThis.trailblazorTheme);
})();
