namespace Trailblazor;

/// <summary>The words a <see cref="TbTabs"/> says to screen readers.</summary>
public sealed record TabsLabels
{
    /// <summary>Names the add button. Leave it null for the app's text for <see cref="TbStrings.TabsAdd"/>.</summary>
    public string? Add { get; init; }

    /// <summary>Describes a tab that can be closed, telling screen reader users how. Leave it null for the app's text
    /// for <see cref="TbStrings.TabsCloseHint"/>.</summary>
    public string? CloseHint { get; init; }

    /// <summary>Names the button that scrolls the tab row toward its start. Leave it null for the app's text for
    /// <see cref="TbStrings.TabsScrollStart"/>.</summary>
    public string? ScrollStart { get; init; }

    /// <summary>Names the button that scrolls the tab row toward its end. Leave it null for the app's text for
    /// <see cref="TbStrings.TabsScrollEnd"/>.</summary>
    public string? ScrollEnd { get; init; }
}
