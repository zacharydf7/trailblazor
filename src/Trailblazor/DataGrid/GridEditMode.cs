namespace Trailblazor;

/// <summary>What one edit of a <see cref="TbDataGrid{TItem}"/> covers.</summary>
public enum GridEditMode
{
    /// <summary>One cell, saved on its own.</summary>
    Cell,

    /// <summary>Every <see cref="TbColumn{TItem}.Editable"/> cell of a row, checked and saved together.</summary>
    Row,

    /// <summary>One cell at a time, as <see cref="Cell"/> does, but each checked edit is kept as a change, marked on its
    /// cell, until the user saves or discards all the changes at once.</summary>
    Batch,
}
