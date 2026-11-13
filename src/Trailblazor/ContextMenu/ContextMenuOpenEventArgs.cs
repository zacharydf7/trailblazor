using System.ComponentModel;

namespace Trailblazor.Internal.Events;

/// <summary>A <see cref="TbContextMenu"/> asking for its items to be drawn for a key before it shows them. Raised by the
/// menu's script and by the bUnit context menu driver; consumers don't normally handle it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ContextMenuOpenEventArgs : EventArgs
{
    /// <summary>The <c>data-context-key</c> of the element the menu opens for, or empty for the rest of the target.
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>The script's number for this request. The menu writes it back once it has drawn the items, so the
    /// answer to an earlier request doesn't show the menu.</summary>
    public int Request { get; set; }
}
