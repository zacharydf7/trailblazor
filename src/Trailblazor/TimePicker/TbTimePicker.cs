using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Chooses a time of day in the browser's own time box, which shows it in the user's language. Bind it with
/// <c>@bind-Value</c> to a <see cref="TimeOnly"/>, or to a <see cref="TimeSpan"/> from midnight that is under 24 hours.
/// </summary>
public partial class TbTimePicker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TTime> where TTime
    : struct
{
    private ElementReference _input;

    /// <summary>The earliest time that can be chosen. A <see cref="Min"/> after <see cref="Max"/> is a range across
    /// midnight, such as 10 PM to 6 AM.</summary>
    [Parameter]
    public TTime? Min { get; set; }

    /// <summary>The latest time that can be chosen.</summary>
    [Parameter]
    public TTime? Max { get; set; }

    /// <summary>How far apart the times that can be chosen are, such as 15 minutes; the browser's arrows move by it. A
    /// step that isn't a whole number of minutes adds seconds to the box. Defaults to a minute.</summary>
    [Parameter]
    public TimeSpan? Step { get; set; }

    /// <summary>The words the picker shows.</summary>
    [Parameter]
    public TimePickerLabels Labels { get; set; } = new();

    /// <summary>Extra CSS classes for the field's parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    private bool ShowsSeconds => Step is { } step && step.TotalSeconds % 60 != 0;

    private string? StepSeconds => Step is { } step
        ? step.TotalSeconds.ToString(CultureInfo.InvariantCulture)
        : null;

    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) => _input.FocusAsync(preventScroll);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (typeof(TTime) != typeof(TimeOnly) && typeof(TTime) != typeof(TimeSpan))
        {
            throw new InvalidOperationException(
                $"{nameof(TbTimePicker<TTime>)} chooses TimeOnly or TimeSpan, not {typeof(TTime).Name}.");
        }
    }

    // The time box reads and writes 24-hour times, whatever the user sees.
    private protected override string? FormatValue(TTime? value) => IsoOf(value);

    private protected override bool TryParse(string? text, out TTime? value, out string? error)
    {
        value = null;
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!TimeOnly.TryParseExact(text, ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFF"], CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var time))
        {
            error = Message(TbStrings.FieldInvalid);
            return false;
        }

        value = FromTime(time);
        return true;
    }

    // A time outside Min and Max shows a message, however it arrived, since the browser lets one be typed. A Min after
    // Max is a range across midnight, as the browser's time box reads it.
    private protected override string? ValueError(TTime? value)
    {
        if (value is not { } chosen)
        {
            return null;
        }

        var time = ToTime(chosen);
        if (Min is { } min && Max is { } max)
        {
            var (first, last) = (ToTime(min), ToTime(max));
            var inside = first <= last ? time >= first && time <= last : time >= first || time <= last;
            return inside ? null : string.Format(CultureInfo.CurrentCulture, OutOfRangeLabel, Shown(min), Shown(max));
        }

        if (Min is { } earliest && time < ToTime(earliest))
        {
            return string.Format(CultureInfo.CurrentCulture, AtLeastLabel, Shown(earliest));
        }

        return Max is { } latest && time > ToTime(latest)
            ? string.Format(CultureInfo.CurrentCulture, AtMostLabel, Shown(latest))
            : null;
    }

    private string? IsoOf(TTime? value) =>
        value is { } time ? ToTime(time).ToString(ShowsSeconds ? "HH:mm:ss" : "HH:mm", CultureInfo.InvariantCulture)
            : null;

    private static string Shown(TTime time) => ToTime(time).ToString("t", CultureInfo.CurrentCulture);

    private static TimeOnly ToTime(TTime value) => value switch
    {
        TimeSpan span when span < TimeSpan.Zero || span >= TimeSpan.FromDays(1) => throw new InvalidOperationException(
            $"{nameof(TbTimePicker<TTime>)} takes a TimeSpan from midnight that is under 24 hours, not {span}."),
        TimeSpan span => TimeOnly.FromTimeSpan(span),
        _ => (TimeOnly)(object)value,
    };

    private static TTime FromTime(TimeOnly time) =>
        typeof(TTime) == typeof(TimeSpan) ? (TTime)(object)time.ToTimeSpan() : (TTime)(object)time;

    private string OutOfRangeLabel => Labels.OutOfRange ?? Localizer[TbStrings.TimePickerOutOfRange];
    private string AtLeastLabel => Labels.AtLeast ?? Localizer[TbStrings.TimePickerAtLeast];
    private string AtMostLabel => Labels.AtMost ?? Localizer[TbStrings.TimePickerAtMost];
}
