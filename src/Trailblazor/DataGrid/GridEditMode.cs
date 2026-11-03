namespace Trailblazor;

/// <summary>What one edit of a <see cref="TbDataGrid{TItem}"/> covers.</summary>
public enum GridEditMode
{
    /// <summary>One cell, saved on its own.</summary>
    Cell,

    /// <summary>Every <see cref="TbColumn{TItem}.Editable"/> cell of a row, checked and saved together.</summary>
    Row,
}
