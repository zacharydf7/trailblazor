namespace Trailblazor;

/// <summary>A row the user saved in a <see cref="TbDataGrid{TItem}"/> whose <see cref="TbDataGrid{TItem}.EditMode"/>
/// is <see cref="GridEditMode.Row"/>, once every cell's checks passed.</summary>
/// <param name="Item">The row's item, as it was before the edit.</param>
/// <param name="Values">The value of every editable cell in the row, changed or not, by its column's
/// <see cref="TbColumn{TItem}.Id"/>: of the column's <see cref="TbColumn{TItem}.Field"/>'s type, or what its
/// <see cref="TbColumn{TItem}.EditTemplate"/> gave.</param>
public sealed record GridRowEdit<TItem>(TItem Item, IReadOnlyDictionary<string, object?> Values)
{
    /// <summary>Set by an <see cref="TbDataGrid{TItem}.OnRowEdit"/> handler that can't save the row, such as when a
    /// server turns it down, to keep the edit open and show why beside the row's Save and Cancel buttons.</summary>
    public string? Error { get; set; }
}
