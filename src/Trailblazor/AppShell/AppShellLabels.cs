namespace Trailblazor;

/// <summary>The words a <see cref="TbAppShell"/> shows and says to screen readers.</summary>
public sealed record AppShellLabels
{
    /// <summary>Names the navigation landmark, which screen readers follow with "navigation". Leave it null for the
    /// app's text for <see cref="TbStrings.AppShellNavigation"/>, "Main" in English, heard as "Main navigation".
    /// </summary>
    public string? Navigation { get; init; }

    /// <summary>Names the app bar's menu button and titles the navigation drawer. Leave it null for the app's text for
    /// <see cref="TbStrings.AppShellMenu"/>.</summary>
    public string? Menu { get; init; }

    /// <summary>The text of the link that skips to the page's content, shown when it has focus. Leave it null for the
    /// app's text for <see cref="TbStrings.AppShellSkipToContent"/>.</summary>
    public string? SkipLink { get; init; }
}
