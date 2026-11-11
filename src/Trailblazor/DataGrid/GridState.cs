using System.Collections.ObjectModel;

namespace Trailblazor;

/// <summary>How the user has arranged a <see cref="TbDataGrid{TItem}"/>: its sorts, filters, groups, page, page size
/// and column widths, order and visibility, bound with <see cref="TbDataGrid{TItem}.State"/>. It serializes with
/// System.Text.Json, so an app can save it, such as to local storage, and restore it later; a filter's values read
/// back from JSON take the type of their column's values, except on a column with
/// <see cref="TbColumn{TItem}.FilterBy"/>, where they keep the types their JSON gives them. Two states are equal when
/// their lists hold equal items in the same order and their column widths match.</summary>
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

    /// <summary>The keys of the columns the user hid, in a grid with <see cref="TbDataGrid{TItem}.HideableColumns"/>,
    /// in the order they were hidden. Set it to start with a column hidden. A null reads as none.</summary>
    public IReadOnlyList<string> HiddenColumns { get; init => field = value ?? []; } = [];

    /// <summary>Whether the other state has equal sorts, filters and groups in the same order, the same page and page
    /// size, the same column widths, the same column order and the same hidden columns.</summary>
    public bool Equals(GridState? other) => other is not null && Sorts.SequenceEqual(other.Sorts)
        && Filters.SequenceEqual(other.Filters) && Groups.SequenceEqual(other.Groups) && Page == other.Page
        && PageSize == other.PageSize && SameWidths(ColumnWidths, other.ColumnWidths)
        && ColumnOrder.SequenceEqual(other.ColumnOrder) && HiddenColumns.SequenceEqual(other.HiddenColumns);

    /// <summary>A hash code that equal states share.</summary>
    public override int GetHashCode() => HashCode.Combine(Sorts.Count, Filters.Count, Groups.Count, Page, PageSize,
        ColumnWidths.Count, ColumnOrder.Count, HiddenColumns.Count);

    /// <summary>The sorts, page and page size as query parameters, such as <c>sort=name,-length</c>, <c>page=2</c> and
    /// <c>size=20</c>, for <c>NavigationManager.GetUriWithQueryParameters</c>. A value at its default is null, which
    /// leaves its parameter out. A <paramref name="prefix"/>, such as "trails", names them <c>trails.sort</c> and so on, so two
    /// grids can share a page. Filters, groups and columns aren't included.</summary>
    public IReadOnlyDictionary<string, object?> ToQueryParameters(string? prefix = null) => new Dictionary<string, object?>
    {
        [Named(prefix, "sort")] = Sorts.Count == 0 ? null : string.Join(',', Sorts.Select(sort =>
            (sort.Direction == SortDirection.Descending ? "-" : "") + sort.Column)),
        [Named(prefix, "page")] = Page == 1 ? null : Page,
        [Named(prefix, "size")] = PageSize,
    };

    /// <summary>The sorts, page and page size in a query string written by <see cref="ToQueryParameters"/> with the
    /// same prefix, such as the query of the page's address. Anything the query doesn't name, or that can't be read,
    /// keeps its default. The sorts' column keys come from the address as typed, so a <c>DataProvider</c> that builds a
    /// query from them should check them against its own columns first.</summary>
    public static GridState FromQuery(string? query, string? prefix = null)
    {
        var values = (query ?? "").TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToLookup(pair => Unescape(pair[0]), pair => pair.Length > 1 ? Unescape(pair[1]) : "");
        var sorts = values[Named(prefix, "sort")].LastOrDefault()?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
        return new()
        {
            Sorts = [.. sorts.Select(sort => sort.StartsWith('-')
                ? new GridSort(sort[1..], SortDirection.Descending) : new GridSort(sort, SortDirection.Ascending))],
            Page = PositiveNumber(values[Named(prefix, "page")].LastOrDefault()) ?? 1,
            PageSize = PositiveNumber(values[Named(prefix, "size")].LastOrDefault()),
        };
    }

    private static string Named(string? prefix, string name) => prefix is null ? name : $"{prefix}.{name}";

    private static string Unescape(string text) => Uri.UnescapeDataString(text.Replace('+', ' '));

    private static int? PositiveNumber(string? text) =>
        int.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var number) && number > 0
            ? number : null;

    internal static bool SameWidths(IReadOnlyDictionary<string, double> one,
        IReadOnlyDictionary<string, double> other) => one.Count == other.Count
        && one.All(width => other.TryGetValue(width.Key, out var value) && value == width.Value);
}
