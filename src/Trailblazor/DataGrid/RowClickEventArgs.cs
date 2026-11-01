namespace Trailblazor;

/// <summary>A click on a <see cref="TbDataGrid{TItem}"/> row, with whether it landed on a control in the row, such as a
/// cell's button, whose click is its own. Raised by the grid's rows; consumers don't normally handle it.</summary>
public sealed class RowClickEventArgs : EventArgs
{
    /// <summary>Whether the click landed on a link, button, field or other control in the row.</summary>
    public bool OnControl { get; set; }
}
