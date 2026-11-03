namespace Trailblazor;

/// <summary>How the user has arranged a <see cref="TbDataGrid{TItem}"/>'s rows: its sorts, filters, groups, page and
/// page size, bound with <see cref="TbDataGrid{TItem}.State"/>. It serializes with System.Text.Json, so an app can save
/// it, such as to local storage, and restore it later; a filter's values read back from JSON take the type of their
/// column's values, except on a column with <see cref="TbColumn{TItem}.FilterBy"/>, where they keep the types their
/// JSON gives them. Two states are equal when their lists hold equal items in the same order.</summary>
public sealed record GridState
{
    /// <summary>The columns the rows sort by, in order, as in <see cref="TbDataGrid{TItem}.Sorts"/>. A null, such as
    /// from JSON, reads as none.</summary>
    public IReadOnlyList<GridSort> Sorts { get; init => field = value ?? []; } = [];

    /// <summary>The conditions rows must all meet to show, as in <see cref="TbDataGrid{TItem}.Filters"/>. A null reads
    /// as none.</summary>
    public IReadOnlyList<GridFilter> Filters { get; init => field = value ?? []; } = [];

    /// <summary>The columns the rows are grouped by, outermost first, as in <see cref="TbDataGrid{TItem}.Groups"/>. A
    /// null reads as none.</summary>
    public IReadOnlyList<string> Groups { get; init => field = value ?? []; } = [];

    /// <summary>The current page, from 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>How many rows a page holds, or null to show every row with no pages.</summary>
    public int? PageSize { get; init; }

    /// <summary>Whether the other state has equal sorts, filters and groups in the same order, and the same page and
    /// page size.</summary>
    public bool Equals(GridState? other) => other is not null && Sorts.SequenceEqual(other.Sorts)
        && Filters.SequenceEqual(other.Filters) && Groups.SequenceEqual(other.Groups) && Page == other.Page
        && PageSize == other.PageSize;

    /// <summary>A hash code that equal states share.</summary>
    public override int GetHashCode() => HashCode.Combine(Sorts.Count, Filters.Count, Groups.Count, Page, PageSize);
}
