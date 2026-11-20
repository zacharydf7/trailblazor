namespace Trailblazor;

/// <summary>The words a <see cref="TbRibbon"/> says to screen readers.</summary>
public sealed record RibbonLabels
{
    /// <summary>Names the button that collapses the ribbon to its tabs and shows its commands again. Leave it null for
    /// the app's text for <see cref="TbStrings.RibbonCollapse"/>.</summary>
    public string? Collapse { get; init; }

    /// <summary>Announces one tab with a <see cref="TbRibbonTab.Context"/> that has appeared; {0} is the context and
    /// {1} the tab's title. Leave it null for the app's text for <see cref="TbStrings.RibbonContextTab"/>.</summary>
    public string? ContextTab { get; init; }

    /// <summary>Announces several tabs of one context that appeared together; {0} is the context and {1} the tabs'
    /// titles, separated by commas. Leave it null for the app's text for
    /// <see cref="TbStrings.RibbonContextTabs"/>.</summary>
    public string? ContextTabs { get; init; }
}
