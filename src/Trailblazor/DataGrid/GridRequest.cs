namespace Trailblazor;

/// <summary>The rows a <see cref="TbDataGrid{TItem}"/> asks its <see cref="TbDataGrid{TItem}.DataProvider"/> for.
/// </summary>
/// <param name="StartIndex">How many filtered and sorted rows come before the first one asked for.</param>
/// <param name="Count">How many rows to give, or null for every row.</param>
/// <param name="Sorts">The columns to sort by, in order.</param>
public sealed record GridRequest(int StartIndex, int? Count, IReadOnlyList<GridSort> Sorts)
{
    /// <summary>The conditions a row must all meet to be counted and given.</summary>
    public IReadOnlyList<GridFilter> Filters { get; init; } = [];

    /// <summary>The totals to give back in <see cref="GridResult{TItem}.Totals"/>, one for each column with a
    /// <see cref="TbColumn{TItem}.Aggregate"/>, worked out over every row that meets the filters.</summary>
    public IReadOnlyList<GridAggregate> Aggregates { get; init; } = [];
}
