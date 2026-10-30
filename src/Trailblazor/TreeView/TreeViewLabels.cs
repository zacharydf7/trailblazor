namespace Trailblazor;

/// <summary>The words a <see cref="TbTreeView{TItem}"/> shows and says to screen readers.</summary>
public sealed record TreeViewLabels
{
    /// <summary>Shown in place of children that are loading. Defaults to "Loading…".</summary>
    public string Loading { get; init; } = "Loading…";

    /// <summary>Shown in place of children that couldn't load. Defaults to "Couldn't load these items.".</summary>
    public string LoadFailed { get; init; } = "Couldn't load these items.";

    /// <summary>The button that loads children again after a failure. Defaults to "Retry".</summary>
    public string Retry { get; init; } = "Retry";

    /// <summary>Shown when the tree has no rows, unless <c>EmptyContent</c> says otherwise. Defaults to "Nothing to
    /// show.".</summary>
    public string Empty { get; init; } = "Nothing to show.";
}
