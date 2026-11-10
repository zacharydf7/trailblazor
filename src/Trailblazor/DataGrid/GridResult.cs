namespace Trailblazor;

/// <summary>The rows a <see cref="TbDataGrid{TItem}.DataProvider"/> found for a <see cref="GridRequest"/>.</summary>
/// <param name="Items">The rows asked for, in order.</param>
/// <param name="TotalCount">How many rows there are in all, so the grid can page through them.</param>
public sealed record GridResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount)
{
    /// <summary>The totals the request's <see cref="GridRequest.Aggregates"/> asked for, by column key, over every row
    /// that meets the filters. A column the provider leaves out shows no total. Give them in every result, including
    /// each window of rows a virtualized grid loads.</summary>
    public IReadOnlyDictionary<string, object?> Totals { get; init; } = new Dictionary<string, object?>();
}
