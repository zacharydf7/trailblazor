namespace Trailblazor;

/// <summary>The words a <see cref="TbTreeView{TItem, TKey}"/> shows and says to screen readers.</summary>
public sealed record TreeViewLabels
{
    /// <summary>Shown in place of children that are loading. Leave it null for the app's text for <see
    /// cref="TbStrings.TreeViewLoading"/>.</summary>
    public string? Loading { get; init; }

    /// <summary>Shown in place of children that couldn't load. Leave it null for the app's text for <see
    /// cref="TbStrings.TreeViewLoadFailed"/>.</summary>
    public string? LoadFailed { get; init; }

    /// <summary>The button that loads children again after a failure. Leave it null for the app's text for <see
    /// cref="TbStrings.TreeViewRetry"/>.</summary>
    public string? Retry { get; init; }

    /// <summary>Shown when the tree has no rows, unless <c>EmptyContent</c> says otherwise. Leave it null for the app's
    /// text for <see cref="TbStrings.TreeViewEmpty"/>.</summary>
    public string? Empty { get; init; }

    /// <summary>Read out after a move before a row, with {0} what moved and {1} the row, as in "{0} moved before
    /// {1}." Leave it null for the app's text for <see cref="TbStrings.TreeViewMovedBefore"/>.</summary>
    public string? MovedBefore { get; init; }

    /// <summary>Read out after a move after a row, with {0} what moved and {1} the row, as in "{0} moved after {1}."
    /// Leave it null for the app's text for <see cref="TbStrings.TreeViewMovedAfter"/>.</summary>
    public string? MovedAfter { get; init; }

    /// <summary>Read out after a move into a row, with {0} what moved and {1} the row, as in "{0} moved into {1}."
    /// Leave it null for the app's text for <see cref="TbStrings.TreeViewMovedInside"/>.</summary>
    public string? MovedInside { get; init; }
}
