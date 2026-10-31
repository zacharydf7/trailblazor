namespace Trailblazor;

/// <summary>Whether the page is drawn light or dark.</summary>
public enum ThemeMode
{
    /// <summary>Follows the device's own light or dark setting, and changes when it does.</summary>
    System,

    /// <summary>Always light.</summary>
    Light,

    /// <summary>Always dark.</summary>
    Dark,
}
