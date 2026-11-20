namespace Trailblazor;

/// <summary>The words a <see cref="TbRibbon"/> says to screen readers.</summary>
public sealed record RibbonLabels
{
    /// <summary>Names the button that collapses the ribbon to its tabs and shows its commands again. Leave it null for
    /// the app's text for <see cref="TbStrings.RibbonCollapse"/>.</summary>
    public string? Collapse { get; init; }
}
