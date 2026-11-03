using System.Collections;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

// The filters: a field per filterable column in the filter row, or a menu on its header, each reading its column's
// filters and replacing them.
public partial class TbDataGrid<TItem>
{
    private readonly Dictionary<string, FilterDraft> _drafts = [];

    private bool HasFilterRow => FilterMode == GridFilterMode.Row && _columns.Any(column => column.CanFilter);

    private bool HasFilterMenu(TbColumn<TItem> column) => FilterMode == GridFilterMode.Menu && column.CanFilter;

    private bool IsFiltered(TbColumn<TItem> column) => _filters.Any(filter => filter.Column == column.Key);

    private IReadOnlyList<GridFilter> FiltersOf(TbColumn<TItem> column) =>
        [.. _filters.Where(filter => filter.Column == column.Key)];

    private static string FilterClass(TbColumn<TItem> column) => column.CanFilter
        ? ClassNames.Join("tb-data-grid__filter", ClassNames.Modifier("tb-data-grid__filter", column.FilterKind))
        : "tb-data-grid__filter";

    // The filter values in invariant text, the form a number input holds, so tests can see a filter has applied.
    private string? FilterText(TbColumn<TItem> column) => IsFiltered(column)
        ? string.Join(" ", FiltersOf(column).SelectMany(filter => filter.Values).Select(ChoiceKey))
        : null;

    // The current filters as text for the request key, with each filter's values as invariant text.
    private string FiltersKey => string.Join(",", _filters.Select(filter =>
        $"{filter.Column}|{filter.Operator}|{string.Join(";", filter.Values.Select(ChoiceKey))}"));

    private string FilterLabel(TbColumn<TItem> column) => Format(TbStrings.DataGridFilter, column.Heading);

    private object? FilterValue(TbColumn<TItem> column, FilterOperator op) =>
        _filters.FirstOrDefault(filter => filter.Column == column.Key && filter.Operator == op)?.Value;

    private string? TextFilter(TbColumn<TItem> column) => FilterValue(column, FilterOperator.Contains) as string;

    private decimal? NumberFilter(TbColumn<TItem> column) =>
        NumberTypes.TryToDecimal(FilterValue(column, FilterOperator.EqualTo), out var number) ? number : null;

    private DateRange<DateOnly> DateFilter(TbColumn<TItem> column) => new(
        FilterValue(column, FilterOperator.GreaterThanOrEqualTo) as DateOnly?,
        FilterValue(column, FilterOperator.LessThanOrEqualTo) as DateOnly?);

    // A choice is named by its value's invariant text, as an enum value or bool writes itself.
    private string? ChoiceFilter(TbColumn<TItem> column) =>
        FilterValue(column, FilterOperator.EqualTo) is { } value ? ChoiceKey(value) : null;

    private IReadOnlyList<string> ChoicesFilter(TbColumn<TItem> column) =>
        FilterValue(column, FilterOperator.IsAnyOf) is IEnumerable values and not string
            ? [.. values.Cast<object?>().Select(ChoiceKey)] : [];

    private static string ChoiceKey(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

    // A column with choices, or whose values are an enum's or a bool's, filters by a list in the menu.
    private static bool HasChoiceList(TbColumn<TItem> column) =>
        column.FilterChoices is not null || column.FilterKind is ColumnFilterKind.Choice or ColumnFilterKind.YesNo;

    private IEnumerable<(string Key, string Label, object Value)> ChoicesOf(TbColumn<TItem> column) =>
        column.FilterChoices is { } choices
            ? choices.OfType<object>().Select(value => (ChoiceKey(value), column.TextOf(value) ?? "", value))
            : column.FilterKind == ColumnFilterKind.YesNo
                ? [(ChoiceKey(true), Localizer[TbStrings.DataGridFilterYes], true),
                    (ChoiceKey(false), Localizer[TbStrings.DataGridFilterNo], false)]
                : column.Choices.Select(choice => (ChoiceKey(choice.Value), choice.Label, choice.Value));

    private Task FilterTextAsync(TbColumn<TItem> column, string? text) => SetFiltersAsync(column,
        string.IsNullOrEmpty(text) ? [] : [new(column.Key, FilterOperator.Contains, text)]);

    private Task FilterNumberAsync(TbColumn<TItem> column, decimal? number) => SetFiltersAsync(column,
        number is { } value ? [new(column.Key, FilterOperator.EqualTo, value)] : []);

    private Task FilterDatesAsync(TbColumn<TItem> column, DateRange<DateOnly> range) =>
        SetFiltersAsync(column, RangeFilters(column, range));

    // A range is a filter for each end it has.
    private static List<GridFilter> RangeFilters(TbColumn<TItem> column, DateRange<DateOnly> range)
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

        return filters;
    }

    private Task FilterChoiceAsync(TbColumn<TItem> column, string? choice) => SetFiltersAsync(column,
        ChoicesOf(column).Where(option => option.Key == choice)
            .Select(option => new GridFilter(column.Key, FilterOperator.EqualTo, option.Value)).Take(1).ToList());

    private Task FilterChoicesAsync(TbColumn<TItem> column, IReadOnlyList<string>? chosen) =>
        SetFiltersAsync(column, AnyOf(column, chosen ?? []));

    private List<GridFilter> AnyOf(TbColumn<TItem> column, IReadOnlyList<string> chosen) => chosen.Count == 0 ? []
        : [new(column.Key, FilterOperator.IsAnyOf,
            ChoicesOf(column).Where(option => chosen.Contains(option.Key)).Select(option => option.Value).ToList())];

    // A template's handlers may belong to the page, so the grid renders itself again after applying their filters.
    private GridFilterContext FilterContext(TbColumn<TItem> column) => new(column.Key, FiltersOf(column),
        async filters =>
        {
            if (filters.FirstOrDefault(filter => filter.Column != column.Key) is { } other)
            {
                throw new ArgumentException($"A filter template for the \"{column.Key}\" column applied a filter "
                    + $"for \"{other.Column}\"; use the context's {nameof(GridFilterContext.Column)}.",
                    nameof(filters));
            }

            await SetFiltersAsync(column, [.. filters]);
            StateHasChanged();
        });

    // A closed menu's draft is dropped when its column's filters change, so it opens on them next time.
    private void ForgetClosedDrafts(string? column = null)
    {
        foreach (var (key, draft) in _drafts.Where(pair => (column is null || pair.Key == column) && !pair.Value.Open)
                     .ToList())
        {
            _drafts.Remove(key);
        }
    }

    // A menu starts from the column's filters as they are, and changes them only when applied.
    private FilterDraft DraftFor(TbColumn<TItem> column)
    {
        if (!_drafts.TryGetValue(column.Key, out var draft))
        {
            draft = _drafts[column.Key] = new();
            ResetDraft(column, draft);
        }

        return draft;
    }

    private void OpenFilterMenu(TbColumn<TItem> column, bool open)
    {
        var draft = DraftFor(column);
        draft.Open = open;
        if (open)
        {
            ResetDraft(column, draft);
        }
    }

    private void ResetDraft(TbColumn<TItem> column, FilterDraft draft)
    {
        var first = FiltersOf(column).FirstOrDefault();
        draft.Operator = first?.Operator is { } op && FilterOperators(column).Contains(op) ? op
            : FilterOperators(column)[0];
        draft.Text = first?.Value as string;
        draft.Number = NumberTypes.TryToDecimal(first?.Value, out var number) ? number : null;
        draft.Range = DateFilter(column);
        draft.Chosen = ChoiceFilter(column) is { } choice ? [choice] : ChoicesFilter(column);
    }

    private static IReadOnlyList<FilterOperator> FilterOperators(TbColumn<TItem> column) =>
        column.FilterKind == ColumnFilterKind.Number
            ? [FilterOperator.EqualTo, FilterOperator.NotEqualTo, FilterOperator.LessThan,
                FilterOperator.LessThanOrEqualTo, FilterOperator.GreaterThan, FilterOperator.GreaterThanOrEqualTo,
                FilterOperator.IsEmpty, FilterOperator.IsNotEmpty]
            : [FilterOperator.Contains, FilterOperator.DoesNotContain, FilterOperator.StartsWith,
                FilterOperator.EndsWith, FilterOperator.EqualTo, FilterOperator.NotEqualTo, FilterOperator.IsEmpty,
                FilterOperator.IsNotEmpty];

    private string OperatorLabel(FilterOperator op) => Localizer[op switch
    {
        FilterOperator.Contains => TbStrings.DataGridFilterContains,
        FilterOperator.DoesNotContain => TbStrings.DataGridFilterDoesNotContain,
        FilterOperator.StartsWith => TbStrings.DataGridFilterStartsWith,
        FilterOperator.EndsWith => TbStrings.DataGridFilterEndsWith,
        FilterOperator.EqualTo => TbStrings.DataGridFilterEqualTo,
        FilterOperator.NotEqualTo => TbStrings.DataGridFilterNotEqualTo,
        FilterOperator.LessThan => TbStrings.DataGridFilterLessThan,
        FilterOperator.LessThanOrEqualTo => TbStrings.DataGridFilterAtMost,
        FilterOperator.GreaterThan => TbStrings.DataGridFilterGreaterThan,
        FilterOperator.GreaterThanOrEqualTo => TbStrings.DataGridFilterAtLeast,
        FilterOperator.IsEmpty => TbStrings.DataGridFilterIsEmpty,
        _ => TbStrings.DataGridFilterIsNotEmpty,
    }];

    private static bool NeedsValue(FilterOperator op) =>
        op is not (FilterOperator.IsEmpty or FilterOperator.IsNotEmpty);

    // Enter in the value applies the menu, as Apply does, unless it confirms an IME composition. The value field
    // updates as the user types, so the draft already holds the text.
    private Task ApplyOnEnterAsync(TbColumn<TItem> column, KeyboardEventArgs args) =>
        args is { Key: "Enter", IsComposing: false } ? ApplyFilterMenuAsync(column) : Task.CompletedTask;

    private Task ApplyFilterMenuAsync(TbColumn<TItem> column)
    {
        var draft = DraftFor(column);
        draft.Open = false;
        object? value = column.FilterKind == ColumnFilterKind.Number ? draft.Number
            : string.IsNullOrEmpty(draft.Text) ? null : draft.Text;
        return SetFiltersAsync(column, HasChoiceList(column) ? AnyOf(column, draft.Chosen)
            : column.FilterKind == ColumnFilterKind.Date ? RangeFilters(column, draft.Range)
            : !NeedsValue(draft.Operator) ? [new(column.Key, draft.Operator)]
            : value is null ? [] : [new(column.Key, draft.Operator, value)]);
    }

    private Task ClearFilterMenuAsync(TbColumn<TItem> column)
    {
        DraftFor(column).Open = false;
        return SetFiltersAsync(column, []);
    }

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
        ForgetClosedDrafts(column.Key);
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

    // What a filter menu shows until it is applied.
    private sealed class FilterDraft
    {
        public bool Open { get; set; }

        public FilterOperator Operator { get; set; }

        public string? Text { get; set; }

        public decimal? Number { get; set; }

        public DateRange<DateOnly> Range { get; set; }

        public IReadOnlyList<string> Chosen { get; set; } = [];
    }
}
