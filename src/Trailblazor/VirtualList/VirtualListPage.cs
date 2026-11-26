namespace Trailblazor;

/// <summary>The items a <see cref="TbVirtualList{TItem}.PageProvider"/> found, without a count of them all.</summary>
/// <param name="Items">The items asked for, in order, or fewer at the end.</param>
/// <param name="HasMore">Whether items follow these. Set it to false on the last page; the list then counts the items
/// as those before plus these.</param>
public sealed record VirtualListPage<TItem>(IReadOnlyList<TItem> Items, bool HasMore);
