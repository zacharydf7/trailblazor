using System.Globalization;

namespace Trailblazor;

// The filter row: one field per filterable column, which reads its column's filters and replaces them.
public partial class TbDataGrid<TItem>
{
    private bool HasFilterRow => _columns.Any(column => column.CanFilter);

    private bool IsFiltered(TbColumn<TItem> column) => _filters.Any(filter => filter.Column == column.Key);

    private static string FilterClass(TbColumn<TItem> column) => column.CanFilter
        ? ClassNames.Join("tb-data-grid__filter", ClassNames.Modifier("tb-data-grid__filter", column.FilterKind))
        : "tb-data-grid__filter";

    // The filter values in invariant text, the form a number input holds, so tests can see a filter has applied.
    private string? FilterText(TbColumn<TItem> column) => IsFiltered(column)
        ? string.Join(" ", _filters.Where(filter => filter.Column == column.Key)
            .Select(filter => Convert.ToString(filter.Value, CultureInfo.InvariantCulture)))
        : null;

    private string FilterLabel(TbColumn<TItem> column) => Format(TbStrings.DataGridFilter, column.Heading);

    private object? FilterValue(TbColumn<TItem> column, FilterOperator op) =>
        _filters.FirstOrDefault(filter => filter.Column == column.Key && filter.Operator == op)?.Value;

    private string? TextFilter(TbColumn<TItem> column) => FilterValue(column, FilterOperator.Contains) as string;

    private decimal? NumberFilter(TbColumn<TItem> column) =>
        NumberTypes.TryToDecimal(FilterValue(column, FilterOperator.EqualTo), out var number) ? number : null;

    private DateRange<DateOnly> DateFilter(TbColumn<TItem> column) => new(
        FilterValue(column, FilterOperator.GreaterThanOrEqualTo) as DateOnly?,
        FilterValue(column, FilterOperator.LessThanOrEqualTo) as DateOnly?);

    // A choice is named by its value's text, as an enum value or bool writes itself.
    private string? ChoiceFilter(TbColumn<TItem> column) => FilterValue(column, FilterOperator.EqualTo)?.ToString();

    private Task FilterTextAsync(TbColumn<TItem> column, string? text) => SetFiltersAsync(column,
        string.IsNullOrEmpty(text) ? [] : [new(column.Key, FilterOperator.Contains, text)]);

    private Task FilterNumberAsync(TbColumn<TItem> column, decimal? number) => SetFiltersAsync(column,
        number is { } value ? [new(column.Key, FilterOperator.EqualTo, value)] : []);

    // A range is a filter for each end it has.
    private Task FilterDatesAsync(TbColumn<TItem> column, DateRange<DateOnly> range)
    {
        List<GridFilter> filters = [];
        if (range.Start is { } start)
        {
            filters.Add(new(column.Key, FilterOperator.GreaterThanOrEqualTo, start));
        }

        if (range.End is { } end)
        {
            filters.Add(new(column.Key, FilterOperator.LessThanOrEqualTo, end));
        }

        return SetFiltersAsync(column, filters);
    }

    private Task FilterChoiceAsync(TbColumn<TItem> column, string? choice) => SetFiltersAsync(column,
        choice is null ? [] : [new(column.Key, FilterOperator.EqualTo, column.FilterKind == ColumnFilterKind.YesNo
            ? bool.Parse(choice) : column.Choices.First(option => option.Value.ToString() == choice).Value)]);

    // A column's filters are replaced together, so a range's two ends change as one. The page goes back to the first,
    // and the rows now shown are counted aloud once they have loaded, unless newer filters replaced these.
    private async Task SetFiltersAsync(TbColumn<TItem> column, List<GridFilter> filters)
    {
        List<GridFilter> next = [.. _filters.Where(filter => filter.Column != column.Key), .. filters];
        if (next.SequenceEqual(_filters))
        {
            return;
        }

        Telemetry.Used(TelemetryName, "filter");
        _filters = next;
        var pageMoved = _page != 1;
        _page = 1;
        var show = ShowAsync(force: false);
        await FiltersChanged.InvokeAsync(_filters);
        if (pageMoved)
        {
            await PageChanged.InvokeAsync(1);
        }

        await show;
        // A provider that finds nothing says so itself.
        if (ReferenceEquals(next, _filters) && !_failed && (_total > 0 || DataProvider is null))
        {
            Announcer.Polite(Format(TbStrings.DataGridRowsShown, _total));
        }
    }
}
