namespace Trailblazor;

/// <summary>The changes the user saved together in a <see cref="TbDataGrid{TItem}"/> whose
/// <see cref="TbDataGrid{TItem}.EditMode"/> is <see cref="GridEditMode.Batch"/>: one checked edit for each changed
/// cell, the rows marked deleted, and the rows added.</summary>
/// <param name="Changes">Each changed cell, in the order it was first changed, with its row's item as it was before the
/// edit; a deleted row's cells aren't among them, and an added row's are, with its new item, which already holds the
/// values its columns can set. An error set on one of them is ignored; set <see cref="Error"/> to turn the batch
/// down.</param>
public sealed record GridBatchEdit<TItem>(IReadOnlyList<GridCellEdit<TItem>> Changes)
{
    /// <summary>The items of the rows the user deleted, in the order they were deleted, in a grid with
    /// <see cref="TbDataGrid{TItem}.Deletable"/>; remove them from the items, or delete them where they're stored, as
    /// you save the changes.</summary>
    public IReadOnlyList<TItem> Deleted { get; init; } = [];

    /// <summary>The items of the rows the user added, in the order they were added, in a grid with a
    /// <see cref="TbDataGrid{TItem}.NewItem"/>: each made by it, with each value typed into its row set on it where the
    /// grid can set that column's field, and every cell checked. Add them to the items, or save them, as you save the
    /// changes.</summary>
    public IReadOnlyList<TItem> Added { get; init; } = [];

    /// <summary>Set by an <see cref="TbDataGrid{TItem}.OnBatchSave"/> handler that can't save the changes, such as when
    /// a server turns them down, to keep them and show why beside the Save changes button.</summary>
    public string? Error { get; set; }
}
