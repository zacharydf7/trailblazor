namespace Trailblazor;

/// <summary>A row the user asked to delete in a <see cref="TbDataGrid{TItem}"/>, with its Delete button or the
/// Delete key.</summary>
/// <param name="Item">The row's item.</param>
public sealed record GridRowDelete<TItem>(TItem Item)
{
    /// <summary>Set by an <see cref="TbDataGrid{TItem}.OnRowDelete"/> handler that can't delete the row, such as when a
    /// server turns it down, to keep the row and say why.</summary>
    public string? Error { get; set; }
}
