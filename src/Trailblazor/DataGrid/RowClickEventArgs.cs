using System.ComponentModel;

namespace Trailblazor.Internal.Events;

/// <summary>A click on a <see cref="TbDataGrid{TItem}"/> row, with whether the row should ignore it. Raised by the grid's
/// rows; consumers don't normally handle it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RowClickEventArgs : EventArgs
{
    /// <summary>Whether the row should ignore the click: it landed on a link, button, field or other control in the row,
    /// or was the second click of a double-click, or ended a drag that selected the row's text.</summary>
    public bool Ignored { get; set; }

    /// <summary>Whether Shift was held, which selects a range of rows.</summary>
    public bool ShiftKey { get; set; }
}

/// <summary>A range of <see cref="TbDataGrid{TItem}"/> rows growing or shrinking to reach this row. Raised by the grid's
/// rows; consumers don't normally handle it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RowExtendEventArgs : EventArgs
{
    /// <summary>The id of the row that Shift and an arrow key moved from, which anchors the range if no chosen row is
    /// among the rows, or null for a Shift+click on a row's checkbox or a move from a group's heading.</summary>
    public string? From { get; set; }
}
