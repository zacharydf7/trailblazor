namespace Trailblazor;

/// <summary>The words a <see cref="TbSplitter"/>'s separators and their menus show and say to screen readers. Except
/// for <see cref="Pane"/>, each has <c>{0}</c> for the pane it acts on, named by its <c>Label</c> or by
/// <see cref="Pane"/>.</summary>
public sealed record SplitterLabels
{
    /// <summary>Names a separator. Leave it null for the app's text for <see cref="TbStrings.SplitterResize"/>.
    /// </summary>
    public string? Resize { get; init; }

    /// <summary>Names a separator's menu button. Leave it null for the app's text for
    /// <see cref="TbStrings.SplitterMenu"/>.</summary>
    public string? Menu { get; init; }

    /// <summary>The menu item that makes the pane larger. Leave it null for the app's text for
    /// <see cref="TbStrings.SplitterLarger"/>.</summary>
    public string? Larger { get; init; }

    /// <summary>The menu item that makes the pane smaller. Leave it null for the app's text for
    /// <see cref="TbStrings.SplitterSmaller"/>.</summary>
    public string? Smaller { get; init; }

    /// <summary>The menu item that collapses a pane. Leave it null for the app's text for
    /// <see cref="TbStrings.SplitterCollapse"/>.</summary>
    public string? Collapse { get; init; }

    /// <summary>The menu item that restores a collapsed pane. Leave it null for the app's text for
    /// <see cref="TbStrings.SplitterRestore"/>.</summary>
    public string? Restore { get; init; }

    /// <summary>Names a pane with no <c>Label</c>, with <c>{0}</c> for its number. Leave it null for the app's text
    /// for <see cref="TbStrings.SplitterPane"/>.</summary>
    public string? Pane { get; init; }
}
