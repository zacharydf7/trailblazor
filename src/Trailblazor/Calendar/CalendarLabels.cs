namespace Trailblazor;

/// <summary>The words a <see cref="TbCalendar{TDate}"/> says to screen readers.</summary>
public sealed record CalendarLabels
{
    /// <summary>Names the button that shows the month before. Leave it null for the app's text for
    /// <see cref="TbStrings.CalendarPreviousMonth"/>.</summary>
    public string? PreviousMonth { get; init; }

    /// <summary>Names the button that shows the month after. Leave it null for the app's text for
    /// <see cref="TbStrings.CalendarNextMonth"/>.</summary>
    public string? NextMonth { get; init; }

    /// <summary>Names the button that shows the year before, while months are shown. Leave it null for the app's text
    /// for <see cref="TbStrings.CalendarPreviousYear"/>.</summary>
    public string? PreviousYear { get; init; }

    /// <summary>Names the button that shows the year after, while months are shown. Leave it null for the app's text
    /// for <see cref="TbStrings.CalendarNextYear"/>.</summary>
    public string? NextYear { get; init; }

    /// <summary>Names the button that shows the decade before, while years are shown. Leave it null for the app's text
    /// for <see cref="TbStrings.CalendarPreviousDecade"/>.</summary>
    public string? PreviousDecade { get; init; }

    /// <summary>Names the button that shows the decade after, while years are shown. Leave it null for the app's text
    /// for <see cref="TbStrings.CalendarNextDecade"/>.</summary>
    public string? NextDecade { get; init; }

    /// <summary>Names the title button while days are shown, which shows the year's months, with <c>{0}</c> for the
    /// month and year. Leave it null for the app's text for <see cref="TbStrings.CalendarChooseMonth"/>.</summary>
    public string? ChooseMonth { get; init; }

    /// <summary>Names the title button while months are shown, which shows the decade's years, with <c>{0}</c> for the
    /// year. Leave it null for the app's text for <see cref="TbStrings.CalendarChooseYear"/>.</summary>
    public string? ChooseYear { get; init; }
}
