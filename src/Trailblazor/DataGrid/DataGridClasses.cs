namespace Trailblazor;

/// <summary>Extra CSS classes for the main parts inside a <see cref="TbDataGrid{TItem}"/>. Style the others by their
/// <c>data-part</c>, under a class on the grid.</summary>
public sealed record DataGridClasses
{
    /// <summary>Added to the toolbar above the grid (<c>data-part="toolbar"</c>).</summary>
    public string? Toolbar { get; init; }

    /// <summary>Added to every column's header cell.</summary>
    public string? Header { get; init; }

    /// <summary>Added to every row of items (<c>data-part="row"</c>), along with the classes
    /// <see cref="TbDataGrid{TItem}.RowClass"/> gives it.</summary>
    public string? Row { get; init; }

    /// <summary>Added to every column's cell in each row of items.</summary>
    public string? Cell { get; init; }

    /// <summary>Added to every group's heading row (<c>data-part="group"</c>).</summary>
    public string? Group { get; init; }

    /// <summary>Added to the row of totals at the bottom (<c>data-part="totals"</c>).</summary>
    public string? Totals { get; init; }

    /// <summary>Added to the pager under the grid (<c>data-part="pager"</c>).</summary>
    public string? Pager { get; init; }
}
