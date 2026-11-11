using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

internal sealed class ThemeService(JsModuleLoader modules, IOptions<TrailblazorOptions> options) : IThemeService
{
    private const string Module = "./_content/Trailblazor/theme.js";
    private readonly ThemeSettings _settings = options.Value.Theme;

    public ThemeMode Mode { get; private set; } = options.Value.Theme.Mode;

    public Palette Palette { get; private set; } = options.Value.Theme.Palette;

    public Density Density { get; private set; } = options.Value.Theme.Density;

    public event Action? Changed;

    public async Task LoadAsync()
    {
        var module = await modules.ImportAsync(Module);
        // A bUnit test without the browser answers null, which reads as a page that shows none of them.
        var shown = await module.InvokeAsync<string?[]?>("read") ?? [null, null, null];
        Mode = Enum.TryParse<ThemeMode>(shown[0], ignoreCase: true, out var mode) ? mode : _settings.Mode;
        // The app's own palette keeps its seeds, so it still equals the one in the options.
        Palette = string.IsNullOrWhiteSpace(shown[1]) || shown[1] == _settings.Palette.Name ? _settings.Palette
            : new Palette(shown[1]!);
        Density = Enum.TryParse<Density>(shown[2], ignoreCase: true, out var density) ? density : _settings.Density;
        Changed?.Invoke();
    }

    public async Task SetModeAsync(ThemeMode mode)
    {
        await ApplyAsync("tbTheme", mode.ToString().ToLowerInvariant());
        Mode = mode;
        Changed?.Invoke();
    }

    public async Task SetPaletteAsync(Palette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        await ApplyAsync("tbPalette", palette.Name);
        Palette = palette;
        Changed?.Invoke();
    }

    public async Task SetDensityAsync(Density density)
    {
        await ApplyAsync("tbDensity", density.ToString().ToLowerInvariant());
        Density = density;
        Changed?.Invoke();
    }

    private async Task ApplyAsync(string name, string value)
    {
        var module = await modules.ImportAsync(Module);
        await module.InvokeVoidAsync("apply", name, value, _settings.Remember);
    }
}
