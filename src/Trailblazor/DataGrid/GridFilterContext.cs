namespace Trailblazor;

/// <summary>What a column's <see cref="TbColumn{TItem}.FilterTemplate"/> needs: the column's filters, and a way to
/// replace them.</summary>
public sealed class GridFilterContext
{
    private readonly Func<IReadOnlyList<GridFilter>, Task> _apply;

    internal GridFilterContext(string column, IReadOnlyList<GridFilter> filters,
        Func<IReadOnlyList<GridFilter>, Task> apply)
    {
        Column = column;
        Filters = filters;
        _apply = apply;
    }

    /// <summary>The column's <see cref="TbColumn{TItem}.Id"/>, for the <see cref="GridFilter"/>s the template makes.
    /// </summary>
    public string Column { get; }

    /// <summary>The column's filters now.</summary>
    public IReadOnlyList<GridFilter> Filters { get; }

    /// <summary>Replaces the column's filters with these, or clears them when there are none, and shows the rows that
    /// meet them, as the built-in filters do.</summary>
    public Task ApplyAsync(params IReadOnlyList<GridFilter> filters) => _apply(filters);
}
