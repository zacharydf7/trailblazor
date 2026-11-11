using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;
using static Trailblazor.CalendarDates;

namespace Trailblazor;

/// <summary>A month of days shown in place, for choosing one date. Its title shows the year's months, and then the
/// decade's years, to reach a distant date quickly. Bind it with <c>@bind-Value</c> to a <see cref="DateOnly"/> or
/// <see cref="DateTime"/>; it never converts time zones, and a picked date is midnight.</summary>
public partial class TbCalendar<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDate>
    : IAsyncDisposable where TDate : struct
{
    private ElementReference _grid;
    // Set once the script has attached and given the browser's date; the grid's data-ready tells tests so.
    private IJSObjectReference? _handle;
    private DotNetObjectReference<CalendarEvents>? _events;
    private View _view;
    // The cell that holds the grid's tab stop; its month, year or decade is the one shown.
    private DateOnly _focus;
    private DateOnly? _browserToday;
    private TDate? _lastValue;
    private TDate? _lastMonth;
    // The month last shown or reported, so MonthChanged is raised once for each.
    private DateOnly? _reportedMonth;
    private bool _moved;
    // Counts ShowChosen calls, so the grid's data-shown changes and the script moves focus in the grid to the shown
    // day.
    private int _shown;
    private bool _disposed;
    private readonly ItemHandlers<DateOnly, Func<Task>> _pick;

    private enum View { Days, Months, Years }

    /// <summary>Creates the calendar.</summary>
    public TbCalendar() => _pick = new(date => () => PickAsync(date));

    /// <summary>The month shown, as any date in it. It defaults to the value's month, or else today's, and follows the
    /// user as they move to other months; bind it with <c>@bind-Month</c> to load what <see cref="DayContent"/> shows.
    /// </summary>
    [Parameter]
    public TDate? Month { get; set; }

    /// <summary>Raised with the first day of the month whenever the calendar shows another month.</summary>
    [Parameter]
    public EventCallback<TDate?> MonthChanged { get; set; }

    /// <summary>The earliest date that can be chosen.</summary>
    [Parameter]
    public TDate? Min { get; set; }

    /// <summary>The latest date that can be chosen.</summary>
    [Parameter]
    public TDate? Max { get; set; }

    /// <summary>Returns true for a date that can't be chosen, such as a weekend. The calendar still lets focus reach
    /// it.</summary>
    [Parameter]
    public Func<TDate, bool>? IsDateDisabled { get; set; }

    /// <summary>Raised when a day that can be chosen is picked, even the one already chosen, so a popup holding
    /// the calendar can close. <see cref="TbFieldBase{TValue}.ValueChanged"/> is raised only for a new date.
    /// </summary>
    [Parameter]
    public EventCallback<TDate> OnDayPicked { get; set; }

    /// <summary>Content shown under a day's number, such as a dot for a day with events. Screen readers hear its text
    /// after the date.</summary>
    [Parameter]
    public RenderFragment<TDate>? DayContent { get; set; }

    /// <summary>The culture for the month and day names and the first day of the week. Defaults to the current
    /// culture. Dates are always Gregorian, like the grid, even for a culture with its own calendar, such as th-TH.
    /// </summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>The words the calendar says to screen readers.</summary>
    [Parameter]
    public CalendarLabels Labels { get; set; } = new();

    /// <summary>Extra CSS classes for the calendar's label, helper text and messages.</summary>
    [Parameter]
    public ChoiceClasses? Classes { get; set; }

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    private CultureInfo DateCulture => CalendarDates.Gregorian(Culture ?? CultureInfo.CurrentCulture);

    // The server's clock can be in another time zone, so the browser's date wins once its script has attached.
    private DateOnly Today => _browserToday ?? DateOnly.FromDateTime(Time.GetLocalNow().DateTime);

    private DateOnly MinDate => Min is { } min ? ToDate(min) : DateOnly.MinValue;

    private DateOnly MaxDate => Max is { } max ? ToDate(max) : DateOnly.MaxValue;

    private DateOnly? Chosen => Value is { } value ? ToDate(value) : null;

    private int DecadeStart => _focus.Year - _focus.Year % 10;

    private string TitleId => $"{FieldId}-title";

    private string Title => _view switch
    {
        View.Days => _focus.ToString("Y", DateCulture),
        View.Months => _focus.Year.ToString(DateCulture),
        _ => $"{DecadeStart.ToString(DateCulture)}–{(DecadeStart + 9).ToString(DateCulture)}",
    };

    /// <inheritdoc />
    public override async ValueTask FocusAsync(bool preventScroll = false)
    {
        // The grid's tab stop is rendered from the start, so focus needn't wait for the script to attach.
        var module = await Modules.ImportAsync("./_content/Trailblazor/calendar.js");
        await module.InvokeVoidAsync("focusGrid", _grid, preventScroll);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        ThrowUnlessDate<TDate>(nameof(TbCalendar<TDate>));
        _focus = Clamp(Today);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // A value from the parent brings its month into view; one the calendar reported is already there.
        if (!Nullable.Equals(Value, _lastValue))
        {
            _lastValue = Value;
            if (Chosen is { } chosen)
            {
                _focus = Clamp(chosen);
            }
        }

        // A month from the parent moves the calendar only when it's a new one, so re-rendering with the old one, or with
        // the month the calendar reported, leaves it where the user took it.
        if (!Nullable.Equals(Month, _lastMonth))
        {
            _lastMonth = Month;
            if (Month is { } month && FirstOfMonth(ToDate(month)) != FirstOfMonth(_focus))
            {
                _focus = Clamp(SameDayIn(FirstOfMonth(ToDate(month)), _focus.Day));
            }
        }
    }

    /// <inheritdoc />
    protected override Task OnParametersSetAsync() => ReportMonthAsync();

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        try
        {
            if (firstRender && RendererInfo.IsInteractive)
            {
                await AttachAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the calendar went with it.
        }
    }

    private async Task AttachAsync()
    {
        var module = await Modules.ImportAsync("./_content/Trailblazor/calendar.js");
        if (_disposed)
        {
            return;
        }

        _events = DotNetObjectReference.Create(new CalendarEvents(OnShowDateAsync, OnPickAsync));
        var handle = await module.InvokeAsync<IJSObjectReference?>("createCalendar", _grid, _events,
            (int)DateCulture.DateTimeFormat.FirstDayOfWeek);
        if (handle is null)
        {
            return;
        }

        _browserToday = await handle.InvokeAsync<DateOnly?>("today");
        if (_disposed)
        {
            await handle.DisposeHandleAsync();
            return;
        }

        _handle = handle;
        // With nothing given to show, the calendar opened on the server's today, which may not be the browser's.
        if (!_moved && Value is null && Month is null)
        {
            _focus = Clamp(Today);
            await ReportMonthAsync();
        }

        StateHasChanged();
    }

    // The browser moves focus among the cells it shows; one in another month, year or decade brings that into view.
    private async Task OnShowDateAsync(DateOnly date)
    {
        _moved = true;
        _focus = _view switch
        {
            View.Days => date,
            View.Months => SameDayIn(date, _focus.Day),
            _ => SameDayIn(new DateOnly(date.Year, _focus.Month, 1), _focus.Day),
        };
        await ReportMonthAsync();
        StateHasChanged();
    }

    // A call from the browser renders only when asked to, unlike a click.
    private async Task OnPickAsync(DateOnly date)
    {
        await PickAsync(date);
        StateHasChanged();
    }

    private Task PickAsync(DateOnly date) => _view switch
    {
        View.Days => PickDayAsync(date),
        View.Months => ZoomInAsync(View.Days, date, IsUnavailableMonth(date)),
        _ => ZoomInAsync(View.Months, date, IsUnavailableYear(date.Year)),
    };

    private async Task PickDayAsync(DateOnly day)
    {
        _moved = true;
        _focus = day;
        if (!ReadOnly && !IsUnavailable(day))
        {
            var value = Keep(day, Value);
            _lastValue = value;
            await SetValueAsync(value);
            await OnDayPicked.InvokeAsync(value!.Value);
        }

        await ReportMonthAsync();
    }

    // A month or year opens on the same day of the month, or its last day if it's shorter, so moving back down keeps
    // the day.
    private async Task ZoomInAsync(View view, DateOnly date, bool unavailable)
    {
        if (unavailable)
        {
            return;
        }

        _moved = true;
        _view = view;
        _focus = Clamp(SameDayIn(_view == View.Days ? date : new DateOnly(date.Year, _focus.Month, 1), _focus.Day));
        await ReportMonthAsync();
    }

    // Shows the chosen date's month again, or today's, such as each time a date picker's popup closes.
    internal void ShowChosen()
    {
        _view = View.Days;
        _focus = Clamp(Chosen ?? Today);
        _shown++;
        StateHasChanged();
    }

    private void ZoomOut()
    {
        _moved = true;
        _view = _view == View.Days ? View.Months : View.Years;
    }

    private async Task PageAsync(int step)
    {
        if (Paged(step) is not { } paged)
        {
            return;
        }

        _moved = true;
        _focus = paged;
        await ReportMonthAsync();
    }

    // The same day a month, year or decade away, or null if that's outside the years 1 to 9999.
    private DateOnly? Paged(int step)
    {
        var months = _focus.Year * 12 + _focus.Month - 1 + step * (_view switch
        {
            View.Days => 1,
            View.Months => 12,
            _ => 120,
        });
        var year = months / 12;
        return year is < 1 or > 9999 ? null : SameDayIn(new DateOnly(year, months % 12 + 1, 1), _focus.Day);
    }

    // Only the days view shows a month, so paging through years or decades reports nothing until a month shows.
    private async Task ReportMonthAsync()
    {
        var month = FirstOfMonth(_focus);
        if (_view != View.Days || month == _reportedMonth)
        {
            return;
        }

        _reportedMonth = month;
        await MonthChanged.InvokeAsync(FromDate<TDate>(month));
    }

    private bool IsUnavailable(DateOnly date) => CalendarDates.IsUnavailable(date, Min, Max, IsDateDisabled);

    private bool IsUnavailableMonth(DateOnly month) => month < FirstOfMonth(MinDate) || month > MaxDate;

    private bool IsUnavailableYear(int year) => year < MinDate.Year || year > MaxDate.Year;

    private DateOnly Clamp(DateOnly date) => date < MinDate ? MinDate : date > MaxDate ? MaxDate : date;

    private static DateOnly SameDayIn(DateOnly month, int day) =>
        new(month.Year, month.Month, Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month)));

    // A cell is chosen, or today's, when the chosen date or today falls in it; unit gives the cell a date falls in.
    private bool IsChosen(DateOnly cell, Func<DateOnly, DateOnly> unit) => Chosen is { } chosen && unit(chosen) == cell;

    private bool IsToday(DateOnly cell, Func<DateOnly, DateOnly> unit) => unit(Today) == cell;

    private string CellClass(DateOnly cell, Func<DateOnly, DateOnly> unit) => ClassNames.Join("tb-calendar__cell",
        IsChosen(cell, unit) ? "tb-calendar__cell--selected" : null,
        IsToday(cell, unit) ? "tb-calendar__cell--today" : null);

    private static DateOnly DayOf(DateOnly date) => date;

    private static DateOnly YearOf(DateOnly date) => new(date.Year, 1, 1);

    private string PreviousLabel => _view switch
    {
        View.Days => Labels.PreviousMonth ?? Localizer[TbStrings.CalendarPreviousMonth],
        View.Months => Labels.PreviousYear ?? Localizer[TbStrings.CalendarPreviousYear],
        _ => Labels.PreviousDecade ?? Localizer[TbStrings.CalendarPreviousDecade],
    };

    private string NextLabel => _view switch
    {
        View.Days => Labels.NextMonth ?? Localizer[TbStrings.CalendarNextMonth],
        View.Months => Labels.NextYear ?? Localizer[TbStrings.CalendarNextYear],
        _ => Labels.NextDecade ?? Localizer[TbStrings.CalendarNextDecade],
    };

    private string ZoomOutLabel => string.Format(DateCulture, _view == View.Days
        ? Labels.ChooseMonth ?? Localizer[TbStrings.CalendarChooseMonth]
        : Labels.ChooseYear ?? Localizer[TbStrings.CalendarChooseYear], Title);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Blazor calls only DisposeAsync when both exist, so the field's own cleanup has to run from here.
        Dispose();
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _events?.Dispose();
    }
}
