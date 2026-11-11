using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;
using static Trailblazor.CalendarDates;

namespace Trailblazor;

/// <summary>Chooses a range of dates, typed into two boxes or picked in a two-month calendar. Bind it with
/// <c>@bind-Value</c> to a <see cref="DateRange{TDate}"/> of <see cref="DateOnly"/> or <see cref="DateTime"/>. It
/// shows and returns exactly the dates it is given and never converts time zones; a picked date is midnight.
/// </summary>
public partial class TbDateRangePicker<TDate> : IAsyncDisposable where TDate : struct
{
    private ElementReference _startInput;
    private ElementReference _panel;
    // Set once the script has attached and given the browser's date; the trigger's data-ready tells tests so.
    private IJSObjectReference? _handle;
    private DotNetObjectReference<DateRangePickerEvents>? _events;
    private string? _startText;
    private string? _endText;
    private bool _startBad;
    private bool _endBad;
    private bool _open;
    private DateOnly _firstMonth;
    private DateOnly _focus;
    private DateOnly? _anchor;
    private DateOnly? _browserToday;
    private bool _disposed;
    private readonly ItemHandlers<DateOnly, Func<Task>> _pick;

    /// <summary>Creates the date range picker.</summary>
    public TbDateRangePicker() => _pick = new(day => () => PickAsync(day));

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

    /// <summary>How the boxes show and read dates, such as <c>MM/dd/yyyy</c>. Defaults to the culture's short date.
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>The culture for the format, the month and day names, and the first day of the week. Defaults to the
    /// current culture. Dates are always Gregorian, like the grid, even for a culture with its own calendar, such as
    /// th-TH.</summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>Ready-made ranges shown beside the calendar, such as "Last 7 days".</summary>
    [Parameter]
    public IReadOnlyList<DateRangePreset<TDate>>? Presets { get; set; }

    /// <summary>Shows a button that clears both dates.</summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>Text shown in the start date's box while it's empty, such as "Arrive". It is not a label. Leave it null
    /// for the format, such as MM/DD/YYYY. Without the format there, give it in the field's helper text, since the box
    /// only reads dates typed in that format.</summary>
    [Parameter]
    public string? StartPlaceholder { get; set; }

    /// <summary>Text shown in the end date's box while it's empty, such as "Depart". It is not a label. Leave it null
    /// for the format. Without the format there, give it in the field's helper text, since the box only reads dates
    /// typed in that format.</summary>
    [Parameter]
    public string? EndPlaceholder { get; set; }

    /// <summary>The words the picker shows and says to screen readers.</summary>
    [Parameter]
    public DateRangePickerLabels Labels { get; set; } = new();

    /// <summary>Extra CSS classes for the field's parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    // The label names the group of boxes, not one box.
    private protected override bool LabelTargetsInput => false;

    private CultureInfo DateCulture => CalendarDates.Gregorian(Culture ?? CultureInfo.CurrentCulture);

    private string DateFormat => Format ?? DateCulture.DateTimeFormat.ShortDatePattern;

    private string FormatHint => CalendarDates.FormatHint(DateFormat);

    // The server's clock can be in another time zone, so the browser's date wins once its script has attached.
    private DateOnly Today => _browserToday ?? DateOnly.FromDateTime(Time.GetLocalNow().DateTime);

    // Its two boxes are named from the field's own ids, so an id it is given stays on its root.
    private protected override bool IdNamesControl => false;

    private string StartId => $"{InputId}-start";

    private string EndId => $"{InputId}-end";

    private string PanelId => $"{FieldId}-panel";

    private DateOnly? StartDate => Value.Start is { } start ? ToDate(start) : null;

    private DateOnly? EndDate => Value.End is { } end ? ToDate(end) : null;

    private DateOnly MinDate => Min is { } min ? ToDate(min) : DateOnly.MinValue;

    private DateOnly MaxDate => Max is { } max ? ToDate(max) : DateOnly.MaxValue;

    private string? StartText => _startText ?? FormatDate(StartDate);

    private string? EndText => _endText ?? FormatDate(EndDate);

    private bool HasDates => Value.Start is not null || Value.End is not null || _startText is not null
        || _endText is not null;

    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) => _startInput.FocusAsync(preventScroll);

    private protected override Task OnResetAsync()
    {
        ClearTexts();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ThrowUnlessDate<TDate>(nameof(TbDateRangePicker<TDate>));
        base.OnParametersSet();
        // The typed dates go with their error, which a new range from the parent or a new binding clears.
        if (!HasParseError)
        {
            ClearTexts();
        }

        if (!_open)
        {
            ShowValue();
        }
    }

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
            // The circuit is gone, and the picker went with it.
        }
    }

    private async Task AttachAsync()
    {
        var module = await Modules.ImportAsync("./_content/Trailblazor/date-range-picker.js");
        if (_disposed)
        {
            return;
        }

        _events = DotNetObjectReference.Create(new DateRangePickerEvents(OnToggleAsync, OnShowDateAsync, OnPickAsync));
        var handle = await module.InvokeAsync<IJSObjectReference?>("createDateRangePicker", _panel, _events,
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
        if (!_open)
        {
            ShowValue();
        }

        StateHasChanged();
    }

    // While closed, the calendar waits on the range's start, or today, so it opens there with no round trip.
    private void ShowValue()
    {
        _anchor = null;
        _focus = Clamp(StartDate ?? EndDate ?? Today);
        _firstMonth = FirstOfMonth(_focus);
    }

    private Task OnToggleAsync(bool open)
    {
        _open = open;
        if (!open)
        {
            ShowValue();
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    // The browser moves focus among the days it shows; a date in another month brings that month into view.
    private Task OnShowDateAsync(DateOnly date)
    {
        _focus = date;
        if (date < _firstMonth || date >= _firstMonth.AddMonths(2))
        {
            _firstMonth = date < _firstMonth ? FirstOfMonth(date) : FirstOfMonth(date).AddMonths(-1);
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    // A call from the browser renders only when asked to, unlike a click.
    private async Task OnPickAsync(DateOnly day)
    {
        await PickAsync(day);
        StateHasChanged();
    }

    private void ShowMonths(int months)
    {
        _firstMonth = _firstMonth.AddMonths(months);
        _focus = _focus.AddMonths(months);
    }

    // The first pick starts a range and the second ends it, unless it comes before the first, in which case it starts a
    // new range.
    private async Task PickAsync(DateOnly day)
    {
        // The panel can't open while the field is disabled or read-only, but a pick that still arrives changes nothing.
        if (Disabled || ReadOnly || IsUnavailable(day))
        {
            return;
        }

        _focus = day;
        if (_anchor is not { } start || day < start)
        {
            _anchor = day;
            Announcer.Polite(ChooseEndLabel);
            return;
        }

        await CommitAsync(start, day);
        Announcer.Polite(string.Format(DateCulture, ChosenLabel, start.ToString("D", DateCulture),
            day.ToString("D", DateCulture)));
        await CloseAsync();
    }

    private async Task ChoosePresetAsync(DateRangePreset<TDate> preset)
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        ClearTexts();
        await SetValueAsync(preset.Range);
        await CloseAsync();
    }

    private Task SetStartTextAsync(string? text)
    {
        _startText = text;
        return CommitTextsAsync();
    }

    private Task SetEndTextAsync(string? text)
    {
        _endText = text;
        return CommitTextsAsync();
    }

    // Typed dates commit only once both read as dates in order; until then the text stays for the user to fix.
    private async Task CommitTextsAsync()
    {
        DateOnly? start = StartDate;
        DateOnly? end = EndDate;
        string? startError = null;
        string? endError = null;
        _startBad = _startText is not null && !TryRead(_startText, BadStartLabel, out start, out startError);
        _endBad = _endText is not null && !TryRead(_endText, BadEndLabel, out end, out endError);
        if (!_startBad && !_endBad && start > end)
        {
            _endBad = true;
            endError = EndBeforeStartLabel;
        }

        if (_startBad || _endBad)
        {
            ShowParseError(string.Join(" ", new[] { startError, endError }.OfType<string>()), null);
            return;
        }

        await CommitAsync(start, end);
        ShowValue();
    }

    private bool TryRead(string text, string badFormat, out DateOnly? date, out string? error)
    {
        date = null;
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!DateOnly.TryParseExact(text.Trim(), DateFormat, DateCulture, DateTimeStyles.None, out var read))
        {
            error = string.Format(DateCulture, badFormat, FormatHint);
            return false;
        }

        if (IsUnavailable(read))
        {
            error = string.Format(DateCulture, UnavailableLabel, text.Trim());
            return false;
        }

        date = read;
        return true;
    }

    private async Task ClearAsync()
    {
        ClearTexts();
        await SetValueAsync(default);
        ShowValue();
        // The clear button goes once the dates do, so focus moves to the first box instead of the page.
        await _startInput.FocusAsync();
    }

    private Task CommitAsync(DateOnly? start, DateOnly? end)
    {
        ClearTexts();
        return SetValueAsync(new DateRange<TDate>(Keep(start, Value.Start), Keep(end, Value.End)));
    }

    private async Task CloseAsync()
    {
        if (_handle is not null)
        {
            await _handle.InvokeVoidAsync("close");
        }
    }

    private void ClearTexts()
    {
        _startText = null;
        _endText = null;
        _startBad = false;
        _endBad = false;
    }

    // An error from Validation belongs to both boxes; one from typing belongs to the box that has it.
    private string? AriaInvalid(bool bad) => ShowsInvalid && (bad || !(_startBad || _endBad)) ? "true" : null;

    // A required range needs both of its dates.
    private protected override bool IsMissing(DateRange<TDate> value) => value.Start is null || value.End is null;

    private bool IsUnavailable(DateOnly date) =>
        CalendarDates.IsUnavailable(date, Min, Max, IsDateDisabled);

    private bool IsSelected(DateOnly date) => _anchor is { } anchor ? date == anchor
        : (StartDate, EndDate) switch
        {
            ({ } start, { } end) => date >= start && date <= end,
            ({ } start, null) => date == start,
            (null, { } end) => date == end,
            _ => false,
        };

    private bool IsStart(DateOnly date) => date == (_anchor ?? StartDate);

    private bool IsEnd(DateOnly date) => _anchor is null && date == EndDate;

    // The ends of the range name which end they are; the days between keep just their date, and aria-selected marks
    // them.
    private string DayName(DateOnly date)
    {
        var name = CalendarDates.DayName(date, DateCulture);
        name = IsStart(date) ? $"{name}, {StartLabel}" : name;
        return IsEnd(date) ? $"{name}, {EndLabel}" : name;
    }

    private string DayClass(DateOnly date) => ClassNames.Join("tb-date-range__day",
        IsSelected(date) ? "tb-date-range__day--selected" : null,
        IsStart(date) ? "tb-date-range__day--start" : null,
        IsEnd(date) ? "tb-date-range__day--end" : null,
        date == Today ? "tb-date-range__day--today" : null);

    private DateOnly Clamp(DateOnly date) => date < MinDate ? MinDate : date > MaxDate ? MaxDate : date;

    private string? FormatDate(DateOnly? date) => date?.ToString(DateFormat, DateCulture);


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

    private string StartLabel => Labels.Start ?? Localizer[TbStrings.DateRangePickerStart];
    private string EndLabel => Labels.End ?? Localizer[TbStrings.DateRangePickerEnd];
    private string OpenLabel => Labels.Open ?? Localizer[TbStrings.DateRangePickerOpen];
    private string PreviousMonthLabel => Labels.PreviousMonth ?? Localizer[TbStrings.DateRangePickerPreviousMonth];
    private string NextMonthLabel => Labels.NextMonth ?? Localizer[TbStrings.DateRangePickerNextMonth];
    private string PresetsLabel => Labels.Presets ?? Localizer[TbStrings.DateRangePickerPresets];
    private string ClearLabel => Labels.Clear ?? Localizer[TbStrings.DateRangePickerClear];
    private string ChooseEndLabel => Labels.ChooseEnd ?? Localizer[TbStrings.DateRangePickerChooseEnd];
    private string ChosenLabel => Labels.Chosen ?? Localizer[TbStrings.DateRangePickerChosen];
    private string BadStartLabel => Labels.BadStart ?? Localizer[TbStrings.DateRangePickerBadStart];
    private string BadEndLabel => Labels.BadEnd ?? Localizer[TbStrings.DateRangePickerBadEnd];
    private string UnavailableLabel => Labels.Unavailable ?? Localizer[TbStrings.DateRangePickerUnavailable];
    private string EndBeforeStartLabel => Labels.EndBeforeStart ?? Localizer[TbStrings.DateRangePickerEndBeforeStart];
}
