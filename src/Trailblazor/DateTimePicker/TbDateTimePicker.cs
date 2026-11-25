using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using static Trailblazor.CalendarDates;

namespace Trailblazor;

/// <summary>Chooses a date and a time of day: the date typed into a box or picked in a calendar beside it, and the time
/// in the browser's own time box. Bind it with <c>@bind-Value</c> to a <see cref="DateTime"/> or
/// <see cref="DateTimeOffset"/>. It shows a <see cref="DateTime"/> as written, and a <see cref="DateTimeOffset"/> in
/// the app's time zone, which <see cref="TimeZone"/> explains; a date chosen with no time is midnight.</summary>
public partial class TbDateTimePicker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDateTime>
    where TDateTime : struct
{
    private ElementReference _dateInput;
    private bool _open;
    private bool _lastOpen;
    private TbCalendar<DateOnly>? _calendar;
    private string? _dateText;
    private string? _timeText;
    private bool _dateBad;
    private bool _timeBad;
    private TDateTime? _committed;

    /// <summary>Whether the calendar is open. Bind it with <c>@bind-Open</c> to open or close it from code; only a new
    /// value from the parent opens or closes it, so a render that passes the same value again leaves the user's
    /// choice alone.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the calendar opens or closes, by the user or by a pick that closes it.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>The earliest date and time that can be chosen. The calendar's days before its date can't be picked.
    /// </summary>
    [Parameter]
    public TDateTime? Min { get; set; }

    /// <summary>The latest date and time that can be chosen. The calendar's days after its date can't be picked.
    /// </summary>
    [Parameter]
    public TDateTime? Max { get; set; }

    /// <summary>Returns true for a day that can't be chosen, such as a weekend. The calendar still lets focus reach it.
    /// </summary>
    [Parameter]
    public Func<DateOnly, bool>? IsDateDisabled { get; set; }

    /// <summary>How far apart the times that can be chosen are, such as 15 minutes; the time box's arrows move by it. A
    /// step that isn't a whole number of minutes adds seconds to the box. Defaults to a minute.</summary>
    [Parameter]
    public TimeSpan? Step { get; set; }

    /// <summary>How the date box shows and reads dates, such as <c>MM/dd/yyyy</c>. Defaults to the culture's short
    /// date.</summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>The culture for the date's format, the month and day names, and the first day of the week. Defaults to
    /// the current culture. Dates are always Gregorian, like the grid, even for a culture with its own calendar, such as
    /// th-TH. The time box shows the time in the browser's language.</summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>Text shown in the date box while it's empty. It is not a label. Leave it null to show the format, such
    /// as MM/DD/YYYY. If you set it, give the format in the field's helper text, since the box only reads dates typed
    /// in that format.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>For a <see cref="DateTimeOffset"/>, the time zone it shows the value in; each date and time the user
    /// chooses takes this zone's offset. Left null, it uses the same zone as every other date the library shows: a
    /// <see cref="TimeZoneInfo"/> cascaded around it, then <see cref="TimeSettings.TimeZone"/>, then the local zone of
    /// the app's <see cref="TimeProvider"/>, which on a server is the server's. A static form shows the value in that
    /// zone but posts it with no offset, and Blazor reads it in the server's, so an unchanged value moves unless the
    /// two zones match.</summary>
    [Parameter]
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>Shows a button that clears the date and time.</summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>The words the picker shows and says to screen readers.</summary>
    [Parameter]
    public DateTimePickerLabels Labels { get; set; } = new();

    /// <summary>Extra CSS classes for the field's parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    [CascadingParameter]
    private TimeZoneInfo? CascadedTimeZone { get; set; }

    // The zone a DateTimeOffset shows in and takes its offset from.
    private TimeZoneInfo Zone => ShownTime.Zone(TimeZone, CascadedTimeZone, Services);

    // The label names the group of boxes, not one box, unless the browser's own box stands in for both.
    private protected override bool LabelTargetsInput => !RendererInfo.IsInteractive;

    // Its two boxes are named from the field's own ids, so an id it is given stays on its root.
    private protected override bool IdNamesControl => false;

    private protected override bool Pastable => SmartPasteField.Takes(typeof(TDateTime));

    // Its boxes don't go through TryParse, so a pasted value gets the checks they make: a disabled date is refused,
    // and the time as written, whatever offset follows it, takes the field's zone.
    private protected override bool TryTakePasted(string text, out TDateTime? taken)
    {
        taken = null;
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var read)
            || (IsDateDisabled?.Invoke(DateOnly.FromDateTime(read.DateTime)) ?? false))
        {
            return false;
        }

        taken = Combine(DateOnly.FromDateTime(read.DateTime), TimeOnly.FromDateTime(read.DateTime));
        return true;
    }

    private string DateId => $"{InputId}-date";

    private string TimeId => $"{InputId}-time";

    private CultureInfo DateCulture => CalendarDates.Gregorian(Culture ?? CultureInfo.CurrentCulture);

    private string DateFormat => Format ?? DateCulture.DateTimeFormat.ShortDatePattern;

    private bool ShowsSeconds => Step is { } step && step.TotalSeconds % 60 != 0;

    private string? StepSeconds => Step is { } step ? step.TotalSeconds.ToString(CultureInfo.InvariantCulture) : null;

    private DateTime? Chosen => Value is { } value ? WallClock(value) : null;

    private DateOnly? ChosenDate => Chosen is { } chosen ? DateOnly.FromDateTime(chosen) : null;

    private string? DateText => _dateText ?? ChosenDate?.ToString(DateFormat, DateCulture);

    private string? TimeText => _timeText ?? (Chosen is { } chosen ? TimeOfDay(TimeOnly.FromDateTime(chosen)) : null);

    private DateOnly? MinDate => Min is { } min ? DateOnly.FromDateTime(WallClock(min)) : null;

    private DateOnly? MaxDate => Max is { } max ? DateOnly.FromDateTime(WallClock(max)) : null;

    private bool HasEntry => Value is not null || _dateText is { Length: > 0 } || _timeText is { Length: > 0 };

    private static readonly IReadOnlyDictionary<string, object> GridAttributes =
        new Dictionary<string, object> { ["data-tb-popover-focus"] = "" };

    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) => _dateInput.FocusAsync(preventScroll);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (typeof(TDateTime) != typeof(DateTime) && typeof(TDateTime) != typeof(DateTimeOffset))
        {
            throw new InvalidOperationException($"{nameof(TbDateTimePicker<TDateTime>)} chooses DateTime or "
                + $"DateTimeOffset, not {typeof(TDateTime).Name}.");
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Open != _lastOpen)
        {
            _lastOpen = Open;
            _open = Open;
        }

        // A new value from the parent replaces what the user typed into either box.
        if (!EqualityComparer<TDateTime?>.Default.Equals(Value, _committed))
        {
            _committed = Value;
            ClearTexts();
        }
    }

    private protected override Task OnResetAsync()
    {
        ClearTexts();
        return Task.CompletedTask;
    }

    // A value outside Min and Max shows a message, however it arrived, since a typed date or time isn't held back.
    private protected override string? ValueError(TDateTime? value)
    {
        if (value is not { } chosen)
        {
            return null;
        }

        var early = Min is { } min && Comparer<TDateTime>.Default.Compare(chosen, min) < 0;
        var late = Max is { } max && Comparer<TDateTime>.Default.Compare(chosen, max) > 0;
        return (early || late, Min, Max) switch
        {
            (false, _, _) => null,
            (_, { } first, { } last) => string.Format(DateCulture, OutOfRangeLabel, Shown(first), Shown(last)),
            (_, { } first, null) => string.Format(DateCulture, AtLeastLabel, Shown(first)),
            (_, null, { } last) => string.Format(DateCulture, AtMostLabel, Shown(last)),
            _ => null,
        };
    }

    private Task SetDateTextAsync(string? text)
    {
        _dateText = text;
        return CommitTextsAsync();
    }

    private Task SetTimeTextAsync(string? text)
    {
        _timeText = text;
        return CommitTextsAsync();
    }

    // The boxes commit together once both can be read. A time with no date waits in its box for one, and a date with no
    // time is midnight. A picked day is taken as it is, since not every format writes and reads back the same day.
    private async Task CommitTextsAsync(DateOnly? picked = null)
    {
        if (picked is not null)
        {
            _dateText = null;
        }

        var date = picked ?? ChosenDate;
        TimeOnly? time = Chosen is { } chosen ? TimeOnly.FromDateTime(chosen) : null;
        string? dateError = null;
        _dateBad = _dateText is not null && !TryReadDate(_dateText, out date, out dateError);
        _timeBad = _timeText is not null && !TryReadTime(_timeText, out time);
        if (_dateBad || _timeBad)
        {
            ShowParseError(dateError ?? Message(TbStrings.FieldInvalid), null);
            return;
        }

        if (date is not { } day)
        {
            // The time stays in its box for the next date.
            _timeText = TimeText;
            _dateText = null;
            await CommitAsync(null);
            return;
        }

        ClearTexts();
        await CommitAsync(Combine(day, time ?? TimeOnly.MinValue));
    }

    private Task CommitAsync(TDateTime? value)
    {
        _committed = value;
        // A value that didn't change still takes back a message about text the user has since fixed.
        if (EqualityComparer<TDateTime?>.Default.Equals(value, Value))
        {
            ClearParseError();
        }

        return SetValueAsync(value);
    }

    private bool TryReadDate(string text, out DateOnly? date, out string? error)
    {
        date = null;
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!CalendarDates.TryReadDate(text, DateFormat, DateCulture, out var read))
        {
            error = string.Format(DateCulture, BadDateLabel, FormatHint(DateFormat, Localizer));
            return false;
        }

        if (IsDateDisabled?.Invoke(read) ?? false)
        {
            error = string.Format(DateCulture, UnavailableLabel, text.Trim());
            return false;
        }

        date = read;
        return true;
    }

    // The time box reads and writes 24-hour times, whatever the user sees.
    private static bool TryReadTime(string text, out TimeOnly? time)
    {
        time = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!TimeOnly.TryParseExact(text, ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFF"], CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var read))
        {
            return false;
        }

        time = read;
        return true;
    }

    // A pick keeps the time and closes the calendar, so focus goes back to its button.
    private async Task PickAsync(DateOnly day)
    {
        await CommitTextsAsync(day);
        await SetOpenAsync(false);
    }

    private async Task SetOpenAsync(bool open)
    {
        if (open != _open)
        {
            _open = open;
            await OpenChanged.InvokeAsync(open);
        }
    }

    // The picker sets the value when a day is picked, so the calendar's own copy needs no handling.
    private static Task KeepCalendarValue(DateOnly? date) => Task.CompletedTask;

    private async Task OnOpenChangedAsync(bool open)
    {
        if (!open)
        {
            _calendar?.ShowChosen();
        }

        await SetOpenAsync(open);
    }

    private async Task ClearAsync()
    {
        ClearTexts();
        await CommitAsync(null);
        // The clear button goes once the value does, so focus moves to the date box instead of the page.
        await _dateInput.FocusAsync();
    }

    private void ClearTexts()
    {
        _dateText = null;
        _timeText = null;
        _dateBad = false;
        _timeBad = false;
    }

    // A message from typing belongs to the box that has it; any other belongs to both.
    private string? AriaInvalid(bool bad) => ShowsInvalid && (bad || !(_dateBad || _timeBad)) ? "true" : null;

    private string TimeOfDay(TimeOnly time) =>
        time.ToString(ShowsSeconds ? "HH:mm:ss" : "HH:mm", CultureInfo.InvariantCulture);

    private string Shown(TDateTime value) => WallClock(value).ToString("g", DateCulture);

    // A DateTime as written, and a DateTimeOffset in the zone it shows in.
    private DateTime WallClock(TDateTime value) => value is DateTimeOffset offset
        ? TimeZoneInfo.ConvertTime(offset, Zone).DateTime : (DateTime)(object)value;

    // A DateTime keeps its kind. A DateTimeOffset takes the zone's offset for that time.
    private TDateTime Combine(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time);
        if (typeof(TDateTime) == typeof(DateTime))
        {
            return (TDateTime)(object)(Value is DateTime old ? DateTime.SpecifyKind(local, old.Kind) : local);
        }

        return (TDateTime)(object)new DateTimeOffset(local, Zone.GetUtcOffset(local));
    }

    private string DateLabel => Labels.Date ?? Localizer[TbStrings.DateTimePickerDate];
    private string TimeLabel => Labels.Time ?? Localizer[TbStrings.DateTimePickerTime];
    private string OpenLabel => Labels.Open ?? Localizer[TbStrings.DatePickerOpen];
    private string ClearLabel => Labels.Clear ?? Localizer[TbStrings.DatePickerClear];
    private string BadDateLabel => Labels.BadDate ?? Localizer[TbStrings.DatePickerBadDate];
    private string UnavailableLabel => Labels.Unavailable ?? Localizer[TbStrings.DatePickerUnavailable];
    private string OutOfRangeLabel => Labels.OutOfRange ?? Localizer[TbStrings.DateTimePickerOutOfRange];
    private string AtLeastLabel => Labels.AtLeast ?? Localizer[TbStrings.TimePickerAtLeast];
    private string AtMostLabel => Labels.AtMost ?? Localizer[TbStrings.TimePickerAtMost];
}
