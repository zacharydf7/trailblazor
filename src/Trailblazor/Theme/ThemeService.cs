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

    public event Action? Changed;

    public async Task LoadAsync()
    {
        var module = await modules.ImportAsync(Module);
        // A bUnit test without the browser answers null, which reads as a page that shows neither.
        var shown = await module.InvokeAsync<string?[]?>("read") ?? [null, null];
        Mode = Enum.TryParse<ThemeMode>(shown[0], ignoreCase: true, out var mode) ? mode : _settings.Mode;
        Palette = Enum.TryParse<Palette>(shown[1], ignoreCase: true, out var palette) ? palette : _settings.Palette;
        Changed?.Invoke();
    }

    public async Task SetModeAsync(ThemeMode mode)
    {
        await ApplyAsync("tbTheme", mode.ToString());
        Mode = mode;
        Changed?.Invoke();
    }

    public async Task SetPaletteAsync(Palette palette)
    {
        await ApplyAsync("tbPalette", palette.ToString());
        Palette = palette;
        Changed?.Invoke();
    }

    private async Task ApplyAsync(string name, string value)
    {
        var module = await modules.ImportAsync(Module);
        await module.InvokeVoidAsync("apply", name, value.ToLowerInvariant(), _settings.Remember);
    }
}
