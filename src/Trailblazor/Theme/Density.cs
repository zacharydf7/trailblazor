namespace Trailblazor;

/// <summary>How tall controls, items and rows are, set for the page through <see cref="ThemeSettings.Density"/> or
/// <see cref="IThemeService.SetDensityAsync"/>, or for part of it with a <c>data-tb-density</c> attribute.</summary>
public enum Density
{
    /// <summary>The default: room enough for touch and for reading at a glance.</summary>
    Comfortable,

    /// <summary>Shorter controls, items and rows, for screens full of data. Every target stays at least 24 by 24 px.
    /// </summary>
    Compact,
}
