using System.ComponentModel;

namespace Trailblazor.Internal.Events;

/// <summary>A range of <see cref="TbDataGrid{TItem}"/> cells the user selected, from the cell it started at to the one it
/// reaches. Raised by the grid's script; consumers don't normally handle it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CellRangeEventArgs : EventArgs
{
    /// <summary>The id of the row the range started at, or null when the range is cleared.</summary>
    public string? FromRow { get; set; }

    /// <summary>The key of the column the range started at.</summary>
    public string? FromColumn { get; set; }

    /// <summary>The id of the row the range reaches.</summary>
    public string? ToRow { get; set; }

    /// <summary>The key of the column the range reaches.</summary>
    public string? ToColumn { get; set; }
}
