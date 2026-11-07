namespace Trailblazor;

/// <summary>A click on a <see cref="TbDataGrid{TItem}"/> row, with whether the row should ignore it. Raised by the grid's
/// rows; consumers don't normally handle it.</summary>
public sealed class RowClickEventArgs : EventArgs
{
    /// <summary>Whether the row should ignore the click: it landed on a link, button, field or other control in the row,
    /// or was the second click of a double-click, or ended a drag that selected the row's text.</summary>
    public bool Ignored { get; set; }
}
