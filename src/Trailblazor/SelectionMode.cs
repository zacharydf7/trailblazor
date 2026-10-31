namespace Trailblazor;

/// <summary>How many rows of a <see cref="TbDataGrid{TItem}"/> or a <see cref="TbTreeView{TItem}"/> can be selected.
/// </summary>
public enum SelectionMode
{
    /// <summary>Rows can't be selected; a tree's rows can still be focused, expanded and activated.</summary>
    None,

    /// <summary>One row at a time: a grid gives each row a radio button, and a tree selects the row focus moves to.
    /// </summary>
    Single,

    /// <summary>Any number of rows: a grid gives each row a checkbox, with one in the header for every row on the
    /// page, and a tree adds rows with Ctrl and Shift as in a file list.</summary>
    Multiple,
}
