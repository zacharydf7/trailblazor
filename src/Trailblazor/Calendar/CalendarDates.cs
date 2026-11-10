using System.Globalization;
using System.Text.RegularExpressions;

namespace Trailblazor;

// The date arithmetic and names shared by the calendar and the date range picker's calendar.
internal static partial class CalendarDates
{
    public static void ThrowUnlessDate<TDate>(string component) where TDate : struct
    {
        if (typeof(TDate) != typeof(DateOnly) && typeof(TDate) != typeof(DateTime))
        {
            throw new InvalidOperationException($"{component} chooses DateOnly or DateTime, not {typeof(TDate).Name}.");
        }
    }

    // The month's days in weeks, with null for the days of the months either side.
    public static IEnumerable<DateOnly?[]> Weeks(DateOnly month, CultureInfo culture)
    {
        var lead = ((int)month.DayOfWeek - (int)culture.DateTimeFormat.FirstDayOfWeek + 7) % 7;
        var length = DateTime.DaysInMonth(month.Year, month.Month);
        for (var first = -lead; first < length; first += 7)
        {
            yield return [.. Enumerable.Range(first, 7)
                .Select(day => day >= 0 && day < length ? month.AddDays(day) : (DateOnly?)null)];
        }
    }

    public static IEnumerable<int> WeekDays(CultureInfo culture) => Enumerable.Range(0, 7)
        .Select(day => ((int)culture.DateTimeFormat.FirstDayOfWeek + day) % 7);

    // The day is written unpadded, as its button shows it: a culture's long date can pad it (the invariant culture
    // writes "04"), and the accessible name must contain the visible label.
    public static string DayName(DateOnly date, CultureInfo culture) =>
        date.ToString(PaddedDay().Replace(culture.DateTimeFormat.LongDatePattern, "d"), culture);

    public static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    public static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static DateOnly ToDate<TDate>(TDate value) where TDate : struct =>
        value is DateTime time ? DateOnly.FromDateTime(time) : (DateOnly)(object)value;

    public static TDate FromDate<TDate>(DateOnly date) where TDate : struct => typeof(TDate) == typeof(DateTime)
        ? (TDate)(object)date.ToDateTime(TimeOnly.MinValue)
        : (TDate)(object)date;

    // A date the user left alone keeps its time of day; only a changed date becomes midnight.
    public static TDate? Keep<TDate>(DateOnly? date, TDate? old) where TDate : struct =>
        date is not { } day ? null : old is { } kept && ToDate(kept) == day ? kept : FromDate<TDate>(day);

    [GeneratedRegex("(?<!d)dd(?!d)")]
    private static partial Regex PaddedDay();
}
