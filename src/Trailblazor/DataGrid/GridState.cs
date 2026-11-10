using System.Collections.ObjectModel;

namespace Trailblazor;

/// <summary>How the user has arranged a <see cref="TbDataGrid{TItem}"/>: its sorts, filters, groups, page, page size
/// and column widths, bound with <see cref="TbDataGrid{TItem}.State"/>. It serializes with System.Text.Json, so an app
/// can save it, such as to local storage, and restore it later; a filter's values read back from JSON take the type of
/// their column's values, except on a column with <see cref="TbColumn{TItem}.FilterBy"/>, where they keep the types
/// their JSON gives them. Two states are equal when their lists hold equal items in the same order and their column
/// widths match.</summary>
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

    /// <summary>Each column's width in pixels, by column key, once the user has resized a column of a grid with
    /// <see cref="TbDataGrid{TItem}.ResizableColumns"/>; empty until then. The grid holds the widths fixed only when
    /// every column has one here; otherwise the browser treats them as preferred widths. A null reads as none.</summary>
    public IReadOnlyDictionary<string, double> ColumnWidths
    {
        get;
        init => field = value ?? ReadOnlyDictionary<string, double>.Empty;
    } = ReadOnlyDictionary<string, double>.Empty;

    /// <summary>The columns' keys in the order the user put them, once the user has moved a column of a grid with
    /// <see cref="TbDataGrid{TItem}.ReorderableColumns"/>; empty until then. A column it doesn't name follows the ones
    /// it does, and a pinned column stays on its side. A null reads as none.</summary>
    public IReadOnlyList<string> ColumnOrder { get; init => field = value ?? []; } = [];

    /// <summary>Whether the other state has equal sorts, filters and groups in the same order, the same page and page
    /// size, the same column widths and the same column order.</summary>
    public bool Equals(GridState? other) => other is not null && Sorts.SequenceEqual(other.Sorts)
        && Filters.SequenceEqual(other.Filters) && Groups.SequenceEqual(other.Groups) && Page == other.Page
        && PageSize == other.PageSize && SameWidths(ColumnWidths, other.ColumnWidths)
        && ColumnOrder.SequenceEqual(other.ColumnOrder);

    /// <summary>A hash code that equal states share.</summary>
    public override int GetHashCode() => HashCode.Combine(Sorts.Count, Filters.Count, Groups.Count, Page, PageSize,
        ColumnWidths.Count, ColumnOrder.Count);

    internal static bool SameWidths(IReadOnlyDictionary<string, double> one,
        IReadOnlyDictionary<string, double> other) => one.Count == other.Count
        && one.All(width => other.TryGetValue(width.Key, out var value) && value == width.Value);
}
