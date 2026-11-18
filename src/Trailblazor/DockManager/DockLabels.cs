namespace Trailblazor;

/// <summary>The words of a <see cref="TbDockManager"/>'s menus and what it says to screen readers. Each one left
/// null uses the app's text.</summary>
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

    /// <summary>Names a group's menu button, with <c>{0}</c> for the title of the group's showing panel, which the
    /// menu acts on. Leave it null for the app's text for <see cref="TbStrings.DockMenu"/>.</summary>
    public string? Menu { get; init; }

    /// <summary>The menu item that moves the tab one place toward the start of its row. Leave it null for the app's
    /// text for <see cref="TbStrings.DockMoveTabBack"/>.</summary>
    public string? MoveTabBack { get; init; }

    /// <summary>The menu item that moves the tab one place toward the end of its row. Leave it null for the app's
    /// text for <see cref="TbStrings.DockMoveTabForward"/>.</summary>
    public string? MoveTabForward { get; init; }

    /// <summary>The submenu that lists the other groups. Leave it null for the app's text for
    /// <see cref="TbStrings.DockMoveToGroup"/>.</summary>
    public string? MoveToGroup { get; init; }

    /// <summary>Names a group in that submenu, with <c>{0}</c> for its number and <c>{1}</c> for the title of its
    /// showing panel. Leave it null for the app's text for <see cref="TbStrings.DockGroupItem"/>.</summary>
    public string? GroupItem { get; init; }

    /// <summary>The submenu that gives the panel a new group on one side of its own. Leave it null for the app's
    /// text for <see cref="TbStrings.DockNewGroup"/>.</summary>
    public string? NewGroup { get; init; }

    /// <summary>The submenu that gives the panel a new group along an edge of the dock. Leave it null for the app's
    /// text for <see cref="TbStrings.DockNewGroupAtEdge"/>.</summary>
    public string? NewGroupAtEdge { get; init; }

    /// <summary>The item for the side where text starts, in both new-group submenus. Leave it null for the app's
    /// text for <see cref="TbStrings.DockAtStart"/>.</summary>
    public string? AtStart { get; init; }

    /// <summary>The item for the side where text ends, in both new-group submenus. Leave it null for the app's
    /// text for <see cref="TbStrings.DockAtEnd"/>.</summary>
    public string? AtEnd { get; init; }

    /// <summary>The item for a new group above the panel's own, in the new-group submenu. Leave it null for the
    /// app's text for <see cref="TbStrings.DockAbove"/>.</summary>
    public string? Above { get; init; }

    /// <summary>The item for a new group below the panel's own, in the new-group submenu. Leave it null for the
    /// app's text for <see cref="TbStrings.DockBelow"/>.</summary>
    public string? Below { get; init; }

    /// <summary>The item for the dock's top edge, in the edge submenu. Leave it null for the app's text for
    /// <see cref="TbStrings.DockAtTop"/>.</summary>
    public string? AtTop { get; init; }

    /// <summary>The item for the dock's bottom edge, in the edge submenu. Leave it null for the app's text for
    /// <see cref="TbStrings.DockAtBottom"/>.</summary>
    public string? AtBottom { get; init; }

    /// <summary>The menu item that closes the panel. Leave it null for the app's text for
    /// <see cref="TbStrings.DockClose"/>.</summary>
    public string? Close { get; init; }

    /// <summary>What screen readers hear after a move from the menu, with <c>{0}</c> for the panel's title and
    /// <c>{1}</c> for the number of the group it is in now. Leave it null for the app's text for
    /// <see cref="TbStrings.DockMoved"/>.</summary>
    public string? Moved { get; init; }

    /// <summary>Names the menu button on a separator, with <c>{0}</c> for the title of the panel showing before it.
    /// Leave it null for the app's text for <see cref="TbStrings.DockSizeMenu"/>.</summary>
    public string? SizeMenu { get; init; }

    /// <summary>The menu item that makes the part before a separator larger, with <c>{0}</c> for the title of the
    /// panel showing in it. Leave it null for the app's text for <see cref="TbStrings.DockLarger"/>.</summary>
    public string? Larger { get; init; }

    /// <summary>The menu item that makes the part before a separator smaller, with <c>{0}</c> for the title of the
    /// panel showing in it. Leave it null for the app's text for <see cref="TbStrings.DockSmaller"/>.</summary>
    public string? Smaller { get; init; }
}
