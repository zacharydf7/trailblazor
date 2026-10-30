namespace Trailblazor;

/// <summary>The words a <see cref="TbDateRangePicker{TDate}"/> shows and says to screen readers.</summary>
public sealed record DateRangePickerLabels
{
    /// <summary>Names the start date's box, after the field's label. Defaults to "Start date".</summary>
    public string Start { get; init; } = "Start date";

    /// <summary>Names the end date's box, after the field's label. Defaults to "End date".</summary>
    public string End { get; init; } = "End date";

    /// <summary>Names the button that opens the calendar, and the calendar itself. Defaults to "Choose dates".
    /// </summary>
    public string Open { get; init; } = "Choose dates";

    /// <summary>Names the button that shows the month before. Defaults to "Previous month".</summary>
    public string PreviousMonth { get; init; } = "Previous month";

    /// <summary>Names the button that shows the month after. Defaults to "Next month".</summary>
    public string NextMonth { get; init; } = "Next month";

    /// <summary>Names the group of presets. Defaults to "Presets".</summary>
    public string Presets { get; init; } = "Presets";

    /// <summary>Names the button that clears both dates. Defaults to "Clear".</summary>
    public string Clear { get; init; } = "Clear";

    /// <summary>Said once the start is picked in the calendar. Defaults to "Now choose the end date.".</summary>
    public string ChooseEnd { get; init; } = "Now choose the end date.";

    /// <summary>The message for a start date that can't be read, with <c>{0}</c> for the format, such as
    /// MM/DD/YYYY. Defaults to "Enter the start date as {0}.".</summary>
    public string BadStart { get; init; } = "Enter the start date as {0}.";

    /// <summary>The message for an end date that can't be read, with <c>{0}</c> for the format. Defaults to
    /// "Enter the end date as {0}.".</summary>
    public string BadEnd { get; init; } = "Enter the end date as {0}.";

    /// <summary>The message for a typed date that can't be chosen, with <c>{0}</c> for the date. Defaults to
    /// "{0} can't be chosen.".</summary>
    public string Unavailable { get; init; } = "{0} can't be chosen.";

    /// <summary>The message for an end date before the start. Defaults to "The end date is before the start date.".
    /// </summary>
    public string EndBeforeStart { get; init; } = "The end date is before the start date.";
}
