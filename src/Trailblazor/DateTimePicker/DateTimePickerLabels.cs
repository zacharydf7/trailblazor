namespace Trailblazor;

/// <summary>The words a <see cref="TbDateTimePicker{TDateTime}"/> shows and says to screen readers.</summary>
public sealed record DateTimePickerLabels
{
    /// <summary>Names the date box, after the field's label. Leave it null for the app's text for
    /// <see cref="TbStrings.DateTimePickerDate"/>.</summary>
    public string? Date { get; init; }

    /// <summary>Names the time box, after the field's label. Leave it null for the app's text for
    /// <see cref="TbStrings.DateTimePickerTime"/>.</summary>
    public string? Time { get; init; }

    /// <summary>Names the button that opens the calendar, and the calendar itself. Leave it null for the app's text for
    /// <see cref="TbStrings.DatePickerOpen"/>.</summary>
    public string? Open { get; init; }

    /// <summary>The words of the calendar that opens, such as its previous and next buttons. Leave it null for the
    /// app's text.</summary>
    public CalendarLabels? Calendar { get; init; }

    /// <summary>Names the button that clears the date and time. Leave it null for the app's text for
    /// <see cref="TbStrings.DatePickerClear"/>.</summary>
    public string? Clear { get; init; }

    /// <summary>The message for a date that can't be read, with <c>{0}</c> for the format, such as MM/DD/YYYY. Leave it
    /// null for the app's text for <see cref="TbStrings.DatePickerBadDate"/>.</summary>
    public string? BadDate { get; init; }

    /// <summary>The message for a typed date that <c>IsDateDisabled</c> rules out, with <c>{0}</c> for the date. Leave
    /// it null for the app's text for <see cref="TbStrings.DatePickerUnavailable"/>.</summary>
    public string? Unavailable { get; init; }

    /// <summary>The message for a value outside <c>Min</c> and <c>Max</c>, with <c>{0}</c> and <c>{1}</c> for the
    /// earliest and latest. Leave it null for the app's text for <see cref="TbStrings.DateTimePickerOutOfRange"/>.
    /// </summary>
    public string? OutOfRange { get; init; }

    /// <summary>The message for a value before <c>Min</c> when there is no <c>Max</c>, with <c>{0}</c> for the
    /// earliest. Leave it null for the app's text for <see cref="TbStrings.TimePickerAtLeast"/>.</summary>
    public string? AtLeast { get; init; }

    /// <summary>The message for a value after <c>Max</c> when there is no <c>Min</c>, with <c>{0}</c> for the latest.
    /// Leave it null for the app's text for <see cref="TbStrings.TimePickerAtMost"/>.</summary>
    public string? AtMost { get; init; }
}
