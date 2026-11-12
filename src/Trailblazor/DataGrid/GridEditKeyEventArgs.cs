using System.ComponentModel;

namespace Trailblazor.Internal.Events;

/// <summary>A key pressed in a <see cref="TbDataGrid{TItem}"/> cell's edit, with whether its field already used it,
/// such as Enter choosing an option. Raised by the grid's edits; consumers don't normally handle it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class GridEditKeyEventArgs : EventArgs
{
    /// <summary>The key, as <see cref="Microsoft.AspNetCore.Components.Web.KeyboardEventArgs.Key"/> names it.
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>Whether the key is part of an IME composition.</summary>
    public bool IsComposing { get; set; }

    /// <summary>Whether the field already used the key, such as a select opening or closing its list.</summary>
    public bool Handled { get; set; }
}
