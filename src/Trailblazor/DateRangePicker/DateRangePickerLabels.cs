namespace Trailblazor;

/// <summary>The words a <see cref="TbDateRangePicker{TDate}"/> shows and says to screen readers.</summary>
public sealed record DateRangePickerLabels
{
    /// <summary>Names the start date's box, after the field's label, and is added to the start day's name in the
    /// calendar. Leave it null for the app's text for <see cref="TbStrings.DateRangePickerStart"/>.</summary>
    public string? Start { get; init; }

    /// <summary>Names the end date's box, after the field's label, and is added to the end day's name in the
    /// calendar. Leave it null for the app's text for <see cref="TbStrings.DateRangePickerEnd"/>.</summary>
    public string? End { get; init; }

    /// <summary>Names the button that opens the calendar, and the calendar itself. Leave it null for the app's text for
    /// <see cref="TbStrings.DateRangePickerOpen"/>. </summary>
    public string? Open { get; init; }

    /// <summary>Names the button that shows the month before. Leave it null for the app's text for <see
    /// cref="TbStrings.DateRangePickerPreviousMonth"/>.</summary>
    public string? PreviousMonth { get; init; }

    /// <summary>Names the button that shows the month after. Leave it null for the app's text for <see
    /// cref="TbStrings.DateRangePickerNextMonth"/>.</summary>
    public string? NextMonth { get; init; }

    /// <summary>Names the group of presets. Leave it null for the app's text for <see
    /// cref="TbStrings.DateRangePickerPresets"/>.</summary>
    public string? Presets { get; init; }

    /// <summary>Names the button that clears both dates. Leave it null for the app's text for <see
    /// cref="TbStrings.DateRangePickerClear"/>.</summary>
    public string? Clear { get; init; }

    /// <summary>Said once the start is picked in the calendar. Leave it null for the app's text for <see
    /// cref="TbStrings.DateRangePickerChooseEnd"/>.</summary>
    public string? ChooseEnd { get; init; }

    /// <summary>Said once the end is picked in the calendar, with <c>{0}</c> and <c>{1}</c> for the two dates. Leave
    /// it null for the app's text for <see cref="TbStrings.DateRangePickerChosen"/>.</summary>
    public string? Chosen { get; init; }

    /// <summary>The message for a start date that can't be read, with <c>{0}</c> for the format, such as MM/DD/YYYY.
    /// Leave it null for the app's text for <see cref="TbStrings.DateRangePickerBadStart"/>.</summary>
    public string? BadStart { get; init; }

    /// <summary>The message for an end date that can't be read, with <c>{0}</c> for the format. Leave it null for the
    /// app's text for <see cref="TbStrings.DateRangePickerBadEnd"/>.</summary>
    public string? BadEnd { get; init; }

    /// <summary>The message for a typed date that can't be chosen, with <c>{0}</c> for the date. Leave it null for the
    /// app's text for <see cref="TbStrings.DateRangePickerUnavailable"/>.</summary>
    public string? Unavailable { get; init; }

    /// <summary>The message for an end date before the start. Leave it null for the app's text for <see
    /// cref="TbStrings.DateRangePickerEndBeforeStart"/>. </summary>
    public string? EndBeforeStart { get; init; }
}
