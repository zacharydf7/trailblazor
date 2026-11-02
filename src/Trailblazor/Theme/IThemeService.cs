namespace Trailblazor;

/// <summary>Reads and changes the page's theme mode (light, dark or the device's setting), its palette and its
/// density. <c>AddTrailblazor()</c> registers it.</summary>
public interface IThemeService
{
    /// <summary>The mode the page shows, as of the last <see cref="LoadAsync"/> or change.</summary>
    ThemeMode Mode { get; }

    /// <summary>The palette the page shows, as of the last <see cref="LoadAsync"/> or change.</summary>
    Palette Palette { get; }

    /// <summary>The density the page shows, as of the last <see cref="LoadAsync"/> or change.</summary>
    Density Density { get; }

    /// <summary>Raised after the mode, palette or density changes, or is loaded.</summary>
    event Action? Changed;

    /// <summary>Reads the mode, palette and density the page shows, which <see cref="TbThemeHead"/> may have restored
    /// from an earlier visit. Call it once the page is interactive.</summary>
    Task LoadAsync();

    /// <summary>Shows the page in a mode, and remembers it when <see cref="ThemeSettings.Remember"/> is on.</summary>
    Task SetModeAsync(ThemeMode mode);

    /// <summary>Shows the page in a palette, and remembers it when <see cref="ThemeSettings.Remember"/> is on.
    /// </summary>
    Task SetPaletteAsync(Palette palette);

    /// <summary>Shows the page in a density, and remembers it when <see cref="ThemeSettings.Remember"/> is on.
    /// </summary>
    Task SetDensityAsync(Density density);
}
