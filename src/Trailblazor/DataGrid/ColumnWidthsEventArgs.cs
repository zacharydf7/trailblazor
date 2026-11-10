namespace Trailblazor;

/// <summary>The widths a data grid's script reports when the user resizes a column: every column's width in pixels, by
/// column key, so the other columns keep theirs. Raised by <see cref="TbDataGrid{TItem}"/>'s table; consumers don't
/// normally handle it.</summary>
public sealed class ColumnWidthsEventArgs : EventArgs
{
    /// <summary>Each column's width in pixels, by column key.</summary>
    public Dictionary<string, double> Widths { get; set; } = [];
}
