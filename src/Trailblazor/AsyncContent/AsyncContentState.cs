namespace Trailblazor;

/// <summary>What a <see cref="TbAsyncContent"/> shows, as its <c>data-state</c> says.</summary>
public enum AsyncContentState
{
    /// <summary>The first load hasn't finished: <c>loading</c>.</summary>
    Loading,

    /// <summary>The last load failed, and the failure shows: <c>failed</c>.</summary>
    Failed,

    /// <summary>The load found nothing, so the empty content shows: <c>empty</c>.</summary>
    Empty,

    /// <summary>Nothing loaded matches the app's search or filter: <c>no-matches</c>.</summary>
    NoMatches,

    /// <summary>The content shows: <c>content</c>.</summary>
    Content,
}
