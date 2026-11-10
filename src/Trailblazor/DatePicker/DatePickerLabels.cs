namespace Trailblazor;

/// <summary>The words a <see cref="TbDatePicker{TDate}"/> shows and says to screen readers.</summary>
public sealed record DatePickerLabels
{
    /// <summary>Names the button that opens the calendar, and the calendar itself. Leave it null for the app's text for
    /// <see cref="TbStrings.DatePickerOpen"/>.</summary>
    public string? Open { get; init; }

    /// <summary>The words of the calendar that opens, such as its previous and next buttons. Leave it null for the
    /// app's text.</summary>
    public CalendarLabels? Calendar { get; init; }

    /// <summary>Names the button that clears the date. Leave it null for the app's text for
    /// <see cref="TbStrings.DatePickerClear"/>.</summary>
    public string? Clear { get; init; }

    /// <summary>The message for a date that can't be read, with <c>{0}</c> for the format, such as MM/DD/YYYY. Leave it
    /// null for the app's text for <see cref="TbStrings.DatePickerBadDate"/>.</summary>
    public string? BadDate { get; init; }

    /// <summary>The message for a typed date that can't be chosen, with <c>{0}</c> for the date. Leave it null for the
    /// app's text for <see cref="TbStrings.DatePickerUnavailable"/>.</summary>
    public string? Unavailable { get; init; }
}
