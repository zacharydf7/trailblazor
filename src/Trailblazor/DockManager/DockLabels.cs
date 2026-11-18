namespace Trailblazor;

/// <summary>The words a <see cref="TbDockManager"/> says to screen readers. Each one left null uses the app's text.
/// </summary>
public sealed record DockLabels
{
    /// <summary>Names a group's tab list, with <c>{0}</c> for the group's number. Leave it null for the app's text
    /// for <see cref="TbStrings.DockGroup"/>.</summary>
    public string? Group { get; init; }

    /// <summary>Names a separator, with <c>{0}</c> for the title of the panel showing before it. Leave it null for
    /// the app's text for <see cref="TbStrings.DockResize"/>.</summary>
    public string? Resize { get; init; }

    /// <summary>Tells screen readers how to close a closable tab. Leave it null for the app's text for
    /// <see cref="TbStrings.DockCloseHint"/>.</summary>
    public string? CloseHint { get; init; }
}
