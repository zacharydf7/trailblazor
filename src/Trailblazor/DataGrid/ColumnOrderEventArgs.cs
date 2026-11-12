using System.ComponentModel;

namespace Trailblazor.Internal.Events;

/// <summary>The column order a data grid's script reports when the user drags a header: every column's key, in its new
/// place. Raised by <see cref="TbDataGrid{TItem}"/>'s table; consumers don't normally handle it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ColumnOrderEventArgs : EventArgs
{
    /// <summary>The columns' keys, in order.</summary>
    public List<string> Order { get; set; } = [];
}
