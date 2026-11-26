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
        $"{filter.Column}|{filter.Join}|{filter.Operator}|{string.Join(";", filter.Values.Select(ChoiceKey))}"));

    private string FilterLabel(TbColumn<TItem> column) =>
        Format(Labels.Filter, TbStrings.DataGridFilter, column.Heading);

    private string FilterValueLabel => Text(Labels.FilterValue, TbStrings.DataGridFilterValue);

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
                ? [(ChoiceKey(true), YesNo(true), true), (ChoiceKey(false), YesNo(false), false)]
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
        var filters = FiltersOf(column);
        draft.First.Reset(filters.FirstOrDefault(), FilterOperators(column));
        draft.Second.Reset(filters.Skip(1).FirstOrDefault(), FilterOperators(column));
        draft.HasSecond = filters.Count > 1;
        draft.FocusSecond = false;
        draft.Join = filters.Skip(1).FirstOrDefault()?.Join ?? FilterJoin.And;
        draft.Range = DateFilter(column);
        draft.Chosen = ChoiceFilter(column) is { } choice ? [choice] : ChoicesFilter(column);
    }

    private static IReadOnlyList<FilterOperator> FilterOperators(TbColumn<TItem> column) =>
        column.FilterKind == ColumnFilterKind.Number ? Comparisons.ForNumbers : Comparisons.ForText;

    private string OperatorLabel(FilterOperator op) => Comparisons.NameOf(op, Labels.Comparisons, Localizer);


    // Enter in the value applies the menu, as Apply does, unless it confirms an IME composition. The value field
    // updates as the user types, so the draft already holds the text.
    private Task ApplyOnEnterAsync(TbColumn<TItem> column, KeyboardEventArgs args) =>
        args is { Key: "Enter", IsComposing: false } ? ApplyFilterMenuAsync(column) : Task.CompletedTask;

    // A second condition counts only when the first one is complete.
    private Task ApplyFilterMenuAsync(TbColumn<TItem> column)
    {
        var draft = DraftFor(column);
        draft.Open = false;
        if (HasChoiceList(column) || column.FilterKind == ColumnFilterKind.Date)
        {
            return SetFiltersAsync(column, HasChoiceList(column) ? AnyOf(column, draft.Chosen)
                : RangeFilters(column, draft.Range));
        }

        List<GridFilter> filters = [];
        if (draft.First.FilterFor(column) is { } first)
        {
            filters.Add(first);
            if (draft.HasSecond && draft.Second.FilterFor(column) is { } second)
            {
                filters.Add(second with { Join = draft.Join });
            }
        }

        return SetFiltersAsync(column, filters);
    }

    private static void AddSecond(FilterDraft draft)
    {
        draft.HasSecond = true;
        draft.FocusSecond = true;
    }

    private static void RemoveSecond(FilterDraft draft)
    {
        draft.HasSecond = false;
        draft.FocusAdd = true;
    }

    // Adding or removing the second condition takes away the button that had focus, so focus moves to the second
    // condition's select, or back to the Add a condition button, once the menu has drawn it.
    private async Task FocusFilterMenusAsync()
    {
        foreach (var draft in _drafts.Values.Where(draft => draft.FocusSecond || draft.FocusAdd))
        {
            if (draft.FocusSecond && draft.Second.Select is { } select)
            {
                draft.FocusSecond = false;
                await select.FocusAsync();
            }
            else if (draft.FocusAdd && draft.AddButton is { } add)
            {
                draft.FocusAdd = false;
                await add.FocusAsync();
            }
        }
    }

    private Task ClearFilterMenuAsync(TbColumn<TItem> column)
    {
        DraftFor(column).Open = false;
        return SetFiltersAsync(column, []);
    }

    private Task SetFiltersAsync(TbColumn<TItem> column, List<GridFilter> filters) =>
        AnsweredAsync(() => ReplaceFiltersAsync(column, filters));

    // A column's filters are replaced together, so a range's two ends change as one. The page goes back to the first,
    // and the rows now shown are counted aloud once they have loaded, unless newer filters replaced these.
    private async Task ReplaceFiltersAsync(TbColumn<TItem> column, List<GridFilter> filters)
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

        await StateChangedAsync();

        await show;
        // A provider that finds nothing says so itself.
        if (ReferenceEquals(next, _filters) && !_failed && (RowCount > 0 || DataProvider is null))
        {
            Announcer.Polite(Format(Labels.RowsShown, TbStrings.DataGridRowsShown, RowCount));
        }
    }

    // What a filter menu shows until it is applied: up to two conditions, or a range, or the chosen values.
    private sealed class FilterDraft
    {
        public bool Open { get; set; }

        public ConditionDraft First { get; } = new();

        public bool HasSecond { get; set; }

        public bool FocusSecond { get; set; }

        public bool FocusAdd { get; set; }

        public TbButton? AddButton { get; set; }

        public FilterJoin Join { get; set; }

        public ConditionDraft Second { get; } = new();

        public DateRange<DateOnly> Range { get; set; }

        public IReadOnlyList<string> Chosen { get; set; } = [];
    }

    // A condition and its value in a filter menu.
    private sealed class ConditionDraft
    {
        public FilterOperator Operator { get; set; }

        public string? Text { get; set; }

        public decimal? Number { get; set; }

        public TbSelect<FilterOperator>? Select { get; set; }

        public void Reset(GridFilter? filter, IReadOnlyList<FilterOperator> operators)
        {
            Operator = filter?.Operator is { } op && operators.Contains(op) ? op : operators[0];
            Text = filter?.Value as string;
            Number = NumberTypes.TryToDecimal(filter?.Value, out var number) ? number : null;
        }

        // The filter this condition makes, or null while it still needs a value.
        public GridFilter? FilterFor(TbColumn<TItem> column)
        {
            if (!Comparisons.NeedsValue(Operator))
            {
                return new(column.Key, Operator);
            }

            object? value = column.FilterKind == ColumnFilterKind.Number ? Number
                : string.IsNullOrEmpty(Text) ? null : Text;
            return value is null ? null : new(column.Key, Operator, value);
        }
    }
}
