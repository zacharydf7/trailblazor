using System.ComponentModel;

namespace Trailblazor.Internal.Events;

/// <summary>Text pasted into a <see cref="TbDataGrid{TItem}"/> at a cell. Raised by the grid's script; consumers don't
/// normally handle it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CellsPasteEventArgs : EventArgs
{
    /// <summary>The id of the row of the cell in focus.</summary>
    public string? Row { get; set; }

    /// <summary>The key of the column of the cell in focus.</summary>
    public string? Column { get; set; }

    /// <summary>The text pasted, such as rows copied from a spreadsheet.</summary>
    public string? Text { get; set; }
}
