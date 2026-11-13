using System.ComponentModel;

namespace Trailblazor.Internal.Events;

/// <summary>The unpinned columns a data grid's box shows as it scrolls, by their places among the unpinned columns. Raised
/// by <see cref="TbDataGrid{TItem}"/>'s script when it draws only the columns in view; consumers don't normally handle
/// it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ColumnWindowEventArgs : EventArgs
{
    /// <summary>The place of the first unpinned column in view, counted from 0.</summary>
    public int First { get; set; }

    /// <summary>The place of the last unpinned column in view, counted from 0.</summary>
    public int Last { get; set; }

    /// <summary>How many pixels a rem is on the page, which turns a column's Width in rem into pixels.</summary>
    public double RemPixels { get; set; }
}
