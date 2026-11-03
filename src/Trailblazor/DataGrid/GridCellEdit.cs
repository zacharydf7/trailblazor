namespace Trailblazor;

/// <summary>An edit the user saved in a <see cref="TbDataGrid{TItem}"/> cell, once its checks passed.</summary>
/// <param name="Item">The row's item, as it was before the edit.</param>
/// <param name="Column">The column's <see cref="TbColumn{TItem}.Id"/>.</param>
/// <param name="Value">The new value, of the <see cref="TbColumn{TItem}.Field"/>'s type, or what the column's
/// <see cref="TbColumn{TItem}.EditTemplate"/> gave.</param>
public sealed record GridCellEdit<TItem>(TItem Item, string Column, object? Value)
{
    /// <summary>Set by an <see cref="TbDataGrid{TItem}.OnCellEdit"/> handler that can't save the edit, such as when a
    /// server turns it down, to keep the edit open showing why.</summary>
    public string? Error { get; set; }
}
