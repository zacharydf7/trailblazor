namespace Trailblazor;

/// <summary>The rows a <see cref="TbDataGrid{TItem}.DataProvider"/> found for a <see cref="GridRequest"/>.</summary>
/// <param name="Items">The rows asked for, in order.</param>
/// <param name="TotalCount">How many rows there are in all, so the grid can page through them.</param>
public sealed record GridResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);
