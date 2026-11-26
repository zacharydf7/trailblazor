namespace Trailblazor;

/// <summary>The rows a <see cref="TbDataGrid{TItem}"/> asks its <see cref="TbDataGrid{TItem}.DataProvider"/> for.
/// </summary>
/// <param name="StartIndex">How many filtered and sorted rows come before the first one asked for.</param>
/// <param name="Count">How many rows to give, or null for every row.</param>
/// <param name="Sorts">The columns to sort by, in order.</param>
public sealed record GridRequest(int StartIndex, int? Count, IReadOnlyList<GridSort> Sorts)
{
    /// <summary>The conditions a row must meet to be counted and given: every column's filters, each joined to those
    /// before it on its column by its <see cref="GridFilter.Join"/>, which a filter menu's second condition can set to
    /// <see cref="FilterJoin.Or"/>.</summary>
    public IReadOnlyList<GridFilter> Filters { get; init; } = [];

    /// <summary>The totals to give back in <see cref="GridResult{TItem}.Totals"/>, one for each column with a
    /// <see cref="TbColumn{TItem}.Aggregate"/>, worked out over every row that meets the filters.</summary>
    public IReadOnlyList<GridAggregate> Aggregates { get; init; } = [];

    /// <summary>The columns the user groups the rows by, by key, outermost first; empty when the rows aren't grouped.
    /// When it isn't empty, the grid asks for a level of groups or one group's rows, as <see cref="Group"/> says.
    /// </summary>
    public IReadOnlyList<string> Groups { get; init; } = [];

    /// <summary>With <see cref="Groups"/>, the group asked for, as the values of the groups it sits in followed by
    /// its own, outermost first, each a <see cref="GridGroup.Value"/> the provider gave; empty asks for the
    /// top-level groups. While it holds fewer values than <see cref="Groups"/>, give the next level of groups in
    /// <see cref="GridResult{TItem}.Groups"/>, sorted by <see cref="Sorts"/> when one of them sorts their column; once
    /// it holds a value for each, give the group's rows. Either way, give them all, without paging.</summary>
    public IReadOnlyList<object?> Group { get; init; } = [];
}
