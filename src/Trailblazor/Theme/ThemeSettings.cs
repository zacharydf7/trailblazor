namespace Trailblazor;

/// <summary>The theme a page starts with, set through <see cref="TrailblazorOptions.Theme"/>.</summary>
public sealed class ThemeSettings
{
    /// <summary>Light, dark, or the device's own setting. Defaults to <see cref="ThemeMode.System"/>.</summary>
    public ThemeMode Mode { get; set; } = ThemeMode.System;

    /// <summary>The page's colors, a built-in palette or your own. Defaults to
    /// <see cref="Trailblazor.Palette.Graphite"/>.</summary>
    public Palette Palette { get; set; } = Palette.Graphite;

    /// <summary>How tall controls, items and rows are. Defaults to <see cref="Trailblazor.Density.Comfortable"/>.
    /// </summary>
    public Density Density { get; set; } = Density.Comfortable;

    /// <summary>Remembers the user's choice in the browser, so the next visit starts with it. On by default.</summary>
    public bool Remember { get; set; } = true;
}
