using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using static Trailblazor.CalendarDates;

namespace Trailblazor;

/// <summary>Chooses one date, typed into a box or picked in a calendar that opens beside it. Bind it with
/// <c>@bind-Value</c> to a <see cref="DateOnly"/> or <see cref="DateTime"/>. It shows and returns exactly the date it is
/// given and never converts time zones; a date left alone keeps its time, and a newly picked or typed date is midnight.
/// </summary>
public partial class TbDatePicker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDate> where TDate
    : struct
{
    private ElementReference _input;
    private bool _open;
    private TbCalendar<TDate>? _calendar;

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

    /// <summary>How the box shows and reads dates, such as <c>MM/dd/yyyy</c>. Defaults to the culture's short date.
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>The culture for the format, the month and day names, and the first day of the week. Defaults to the
    /// current culture. Dates are always Gregorian, like the grid, even for a culture with its own calendar, such as
    /// th-TH.</summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>Text shown in the box while it's empty. It is not a label. Leave it null to show the format, such as
    /// MM/DD/YYYY. If you set it, give the format in the field's helper text, since the box only reads dates typed in
    /// that format.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Shows a button that clears the date.</summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>The words the picker shows and says to screen readers.</summary>
    [Parameter]
    public DatePickerLabels Labels { get; set; } = new();

    /// <summary>Extra CSS classes for the field's parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    private CultureInfo DateCulture => CalendarDates.Gregorian(Culture ?? CultureInfo.CurrentCulture);

    private string DateFormat => Format ?? DateCulture.DateTimeFormat.ShortDatePattern;

    private DateOnly? Chosen => Value is { } value ? ToDate(value) : null;

    private static readonly IReadOnlyDictionary<string, object> GridAttributes =
        new Dictionary<string, object> { ["data-tb-popover-focus"] = "" };

    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) => _input.FocusAsync(preventScroll);

    /// <inheritdoc />
    protected override void OnInitialized() => ThrowUnlessDate<TDate>(nameof(TbDatePicker<TDate>));

    private protected override string? FormatValue(TDate? value) =>
        value is { } date ? ToDate(date).ToString(DateFormat, DateCulture) : null;

    // Only text in the field's format is read, and a date that can't be chosen is refused like one that can't be read.
    private protected override bool TryParse(string? text, out TDate? value, out string? error)
    {
        value = null;
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!TryReadDate(text, DateFormat, DateCulture, out var date))
        {
            error = string.Format(DateCulture, BadDateLabel, FormatHint(DateFormat, Localizer));
            return false;
        }

        if (IsUnavailable(date))
        {
            error = string.Format(DateCulture, UnavailableLabel, text.Trim());
            return false;
        }

        value = Keep(date, Value);
        return true;
    }

    // Any pick sets the date, which also takes back a message about typed text, and closes the calendar, so focus goes
    // back to its button.
    private async Task PickAsync(TDate date)
    {
        await SetValueAsync(Keep(ToDate(date), Value));
        _open = false;
    }

    // The picker sets the date when a day is picked, so the calendar's own copy needs no handling.
    private static Task KeepCalendarValue(TDate? date) => Task.CompletedTask;

    private void OnOpenChanged(bool open)
    {
        _open = open;
        if (!open)
        {
            _calendar?.ShowChosen();
        }
    }

    private async Task ClearAsync()
    {
        await SetValueAsync(null);
        // The clear button goes once the date does, so focus moves to the box instead of the page.
        await _input.FocusAsync();
    }

    private bool IsUnavailable(DateOnly date) => CalendarDates.IsUnavailable(date, Min, Max, IsDateDisabled);

    private string OpenLabel => Labels.Open ?? Localizer[TbStrings.DatePickerOpen];
    private string ClearLabel => Labels.Clear ?? Localizer[TbStrings.DatePickerClear];
    private string BadDateLabel => Labels.BadDate ?? Localizer[TbStrings.DatePickerBadDate];
    private string UnavailableLabel => Labels.Unavailable ?? Localizer[TbStrings.DatePickerUnavailable];
}
