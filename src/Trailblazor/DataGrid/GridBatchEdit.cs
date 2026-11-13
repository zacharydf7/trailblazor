namespace Trailblazor;

/// <summary>The changes the user saved together in a <see cref="TbDataGrid{TItem}"/> whose
/// <see cref="TbDataGrid{TItem}.EditMode"/> is <see cref="GridEditMode.Batch"/>: one checked edit for each changed
/// cell.</summary>
/// <param name="Changes">Each changed cell, in the order it was first changed, with its row's item as it was before the
/// edit. An error set on one of them is ignored; set <see cref="Error"/> to turn the batch down.</param>
public sealed record GridBatchEdit<TItem>(IReadOnlyList<GridCellEdit<TItem>> Changes)
{
    /// <summary>Set by an <see cref="TbDataGrid{TItem}.OnBatchSave"/> handler that can't save the changes, such as when
    /// a server turns them down, to keep them and show why beside the Save changes button.</summary>
    public string? Error { get; set; }
}
