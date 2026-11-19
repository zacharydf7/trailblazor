namespace Trailblazor;

/// <summary>The words of a <see cref="TbDockManager"/>'s menus and what it says to screen readers. Each one left
/// null uses the app's text.</summary>
public sealed record DockLabels
{
    /// <summary>Names a group's tab list, with <c>{0}</c> for the group's number. Leave it null for the app's text
    /// for <see cref="TbStrings.DockGroup"/>.</summary>
    public string? Group { get; init; }

    /// <summary>Names a separator, with <c>{0}</c> for the title of the panel showing before it, and a slid-out
    /// panel's inner edge, with that panel's title. Leave it null for the app's text for
    /// <see cref="TbStrings.DockResize"/>.</summary>
    public string? Resize { get; init; }

    /// <summary>Tells screen readers how to close a closable tab. Leave it null for the app's text for
    /// <see cref="TbStrings.DockCloseHint"/>.</summary>
    public string? CloseHint { get; init; }

    /// <summary>Names a group's menu button, with <c>{0}</c> for the title of the group's showing panel, which the
    /// menu acts on. Leave it null for the app's text for <see cref="TbStrings.DockMenu"/>.</summary>
    public string? Menu { get; init; }

    /// <summary>Names the menu that a right-click, a long press, Shift+F10 or the context menu key opens on a tab.
    /// Leave it null for the app's text for <see cref="TbStrings.DockTabMenu"/>.</summary>
    public string? TabMenu { get; init; }

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

    /// <summary>Names a floating window's tab list, with <c>{0}</c> for its number, which counts on from the docked
    /// groups. Leave it null for the app's text for <see cref="TbStrings.DockFloatingGroup"/>.</summary>
    public string? FloatingGroup { get; init; }

    /// <summary>The menu item that takes a panel out of its group, or out of a window it shares, into a floating
    /// window of its own. Leave it null for the app's text for <see cref="TbStrings.DockFloat"/>.</summary>
    public string? Float { get; init; }

    /// <summary>The menu item that docks a floating panel. Leave it null for the app's text for
    /// <see cref="TbStrings.DockDock"/>.</summary>
    public string? Dock { get; init; }

    /// <summary>What screen readers hear when a panel ends up in a floating window, with <c>{0}</c> for the panel's
    /// title. Leave it null for the app's text for <see cref="TbStrings.DockFloated"/>.</summary>
    public string? Floated { get; init; }

    /// <summary>The submenu that puts a panel away on a strip along an edge of the dock. Leave it null for the app's
    /// text for <see cref="TbStrings.DockAutoHide"/>.</summary>
    public string? AutoHide { get; init; }

    /// <summary>Names a strip of auto-hidden panels, with <c>{0}</c> for the words of its edge, such as
    /// <see cref="AtStart"/>. Leave it null for the app's text for <see cref="TbStrings.DockStrip"/>.</summary>
    public string? Strip { get; init; }

    /// <summary>Names the tab list of an auto-hidden panel that is slid out. Leave it null for the app's text for
    /// <see cref="TbStrings.DockRevealedGroup"/>.</summary>
    public string? RevealedGroup { get; init; }

    /// <summary>What screen readers hear when a panel is put away on a strip, with <c>{0}</c> for the panel's
    /// title. Leave it null for the app's text for <see cref="TbStrings.DockAutoHidden"/>.</summary>
    public string? AutoHidden { get; init; }

    /// <summary>Names the button that moves a floating window, with <c>{0}</c> for the title of the window's
    /// showing panel. Leave it null for the app's text for <see cref="TbStrings.DockMoveWindow"/>.</summary>
    public string? MoveWindow { get; init; }

    /// <summary>Tells screen readers that the arrow keys move a window. Leave it null for the app's text for
    /// <see cref="TbStrings.DockMoveWindowHint"/>.</summary>
    public string? MoveWindowHint { get; init; }

    /// <summary>Names the grip that resizes a floating window, with <c>{0}</c> for the title of the window's
    /// showing panel. Leave it null for the app's text for <see cref="TbStrings.DockResizeWindow"/>.</summary>
    public string? ResizeWindow { get; init; }

    /// <summary>Tells screen readers that the arrow keys resize a window. Leave it null for the app's text for
    /// <see cref="TbStrings.DockResizeWindowHint"/>.</summary>
    public string? ResizeWindowHint { get; init; }

    /// <summary>What screen readers hear when a window has moved, with <c>{0}</c> and <c>{1}</c> for how far across
    /// and down the dock it starts, in percent. Leave it null for the app's text for
    /// <see cref="TbStrings.DockMovedWindow"/>.</summary>
    public string? MovedWindow { get; init; }

    /// <summary>What screen readers hear when a window has been resized, with <c>{0}</c> and <c>{1}</c> for its
    /// width and height in percent of the dock. Leave it null for the app's text for
    /// <see cref="TbStrings.DockResizedWindow"/>.</summary>
    public string? ResizedWindow { get; init; }

    /// <summary>The menu item that moves a window toward the left of the screen. Leave it null for the app's text
    /// for <see cref="TbStrings.DockLeft"/>.</summary>
    public string? Left { get; init; }

    /// <summary>The menu item that moves a window toward the right of the screen. Leave it null for the app's text
    /// for <see cref="TbStrings.DockRight"/>.</summary>
    public string? Right { get; init; }

    /// <summary>The menu item that moves a window up. Leave it null for the app's text for
    /// <see cref="TbStrings.DockUp"/>.</summary>
    public string? Up { get; init; }

    /// <summary>The menu item that moves a window down. Leave it null for the app's text for
    /// <see cref="TbStrings.DockDown"/>.</summary>
    public string? Down { get; init; }

    /// <summary>The menu item that makes a window wider. Leave it null for the app's text for
    /// <see cref="TbStrings.DockWider"/>.</summary>
    public string? Wider { get; init; }

    /// <summary>The menu item that makes a window narrower. Leave it null for the app's text for
    /// <see cref="TbStrings.DockNarrower"/>.</summary>
    public string? Narrower { get; init; }

    /// <summary>The menu item that makes a window taller. Leave it null for the app's text for
    /// <see cref="TbStrings.DockTaller"/>.</summary>
    public string? Taller { get; init; }

    /// <summary>The menu item that makes a window shorter. Leave it null for the app's text for
    /// <see cref="TbStrings.DockShorter"/>.</summary>
    public string? Shorter { get; init; }

    /// <summary>The menu item that has the panel's group fill the dock. Leave it null for the app's text for
    /// <see cref="TbStrings.DockMaximize"/>.</summary>
    public string? Maximize { get; init; }

    /// <summary>The menu item, and the name of the button, that show every group again. Leave it null for the app's
    /// text for <see cref="TbStrings.DockRestore"/>.</summary>
    public string? Restore { get; init; }

    /// <summary>What screen readers hear when a group fills the dock, with <c>{0}</c> for the group's number. Leave
    /// it null for the app's text for <see cref="TbStrings.DockMaximized"/>.</summary>
    public string? Maximized { get; init; }

    /// <summary>What screen readers hear when every group shows again. Leave it null for the app's text for
    /// <see cref="TbStrings.DockRestored"/>.</summary>
    public string? Restored { get; init; }

    /// <summary>The menu item that closes the panel. Leave it null for the app's text for
    /// <see cref="TbStrings.DockClose"/>.</summary>
    public string? Close { get; init; }

    /// <summary>What screen readers hear after a move from the menu, with <c>{0}</c> for the panel's title and
    /// <c>{1}</c> for the number of the group it is in now. Leave it null for the app's text for
    /// <see cref="TbStrings.DockMoved"/>.</summary>
    public string? Moved { get; init; }

    /// <summary>Names the menu button on a separator, with <c>{0}</c> for the title of the panel showing before it,
    /// and the one on a slid-out panel's inner edge, with that panel's title. Leave it null for the app's text for
    /// <see cref="TbStrings.DockSizeMenu"/>.</summary>
    public string? SizeMenu { get; init; }

    /// <summary>The menu item that makes the part before a separator larger, with <c>{0}</c> for the title of the
    /// panel showing in it, and the one that makes a slid-out panel larger, with that panel's title. Leave it null
    /// for the app's text for <see cref="TbStrings.DockLarger"/>.</summary>
    public string? Larger { get; init; }

    /// <summary>The menu item that makes the part before a separator smaller, with <c>{0}</c> for the title of the
    /// panel showing in it, and the one that makes a slid-out panel smaller, with that panel's title. Leave it null
    /// for the app's text for <see cref="TbStrings.DockSmaller"/>.</summary>
    public string? Smaller { get; init; }
}
