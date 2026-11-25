namespace Trailblazor;

/// <summary>What a pick in a <see cref="TbCalendar{TDate}"/> or <see cref="TbDatePicker{TDate}"/> chooses.</summary>
public enum CalendarPick
{
    /// <summary>A day.</summary>
    Day,

    /// <summary>A month, held as its first day, or as the first day <c>Min</c> allows in it.</summary>
    Month,

    /// <summary>A year, held as January 1, or as the first day <c>Min</c> allows in it.</summary>
    Year,
}
