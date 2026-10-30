namespace Trailblazor;

/// <summary>How many rows of a <see cref="TbTreeView{TItem}"/> can be selected.</summary>
public enum TreeSelectionMode
{
    /// <summary>Rows can't be selected; they can still be focused, expanded and activated.</summary>
    None,

    /// <summary>One row at a time.</summary>
    Single,

    /// <summary>Any number of rows, with Ctrl and Shift as in a file list.</summary>
    Multiple,
}
