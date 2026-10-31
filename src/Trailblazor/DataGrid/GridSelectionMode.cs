namespace Trailblazor;

/// <summary>How many rows of a <see cref="TbDataGrid{TItem}"/> can be selected.</summary>
public enum GridSelectionMode
{
    /// <summary>Rows can't be selected.</summary>
    None,

    /// <summary>One row at a time, chosen with a radio button or by clicking the row.</summary>
    Single,

    /// <summary>Any number of rows, each with a checkbox, and one in the header for every row on the page.</summary>
    Multiple,
}
