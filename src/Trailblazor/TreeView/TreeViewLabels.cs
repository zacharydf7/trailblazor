namespace Trailblazor;

/// <summary>The words a <see cref="TbTreeView{TItem}"/> shows and says to screen readers.</summary>
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
}
