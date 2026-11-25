using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Trailblazor;

// The date arithmetic and names shared by the calendar and the date range picker's calendar.
internal static partial class CalendarDates
{
    private static readonly ConditionalWeakTable<CultureInfo, CultureInfo> GregorianCultures = [];

    // The grid is Gregorian, so a culture with another calendar, such as th-TH, names and reads dates in Gregorian too.
    public static CultureInfo Gregorian(CultureInfo culture) => culture.DateTimeFormat.Calendar is GregorianCalendar
        || !culture.OptionalCalendars.OfType<GregorianCalendar>().Any()
        ? culture
        : GregorianCultures.GetValue(culture, static other =>
        {
            var copy = (CultureInfo)other.Clone();
            copy.DateTimeFormat.Calendar = other.OptionalCalendars.OfType<GregorianCalendar>().First();
            return CultureInfo.ReadOnly(copy);
        });

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

    // A week's number by the culture's rule, in the Gregorian calendar the grid shows, read from the day that rule
    // decides by, so a week across New Year has one number in both months: its last day for FirstDay, its fourth for
    // FirstFourDayWeek (the ISO 8601 number for Monday weeks) and its first for FirstFullWeek.
    public static int WeekNumber(DateOnly?[] week, CultureInfo culture)
    {
        var at = Array.FindIndex(week, date => date is not null);
        var format = culture.DateTimeFormat;
        var offset = format.CalendarWeekRule switch
        {
            CalendarWeekRule.FirstDay => 6,
            CalendarWeekRule.FirstFourDayWeek => 3,
            _ => 0,
        };
        // Clamped, so the first week of year 1 and the last of 9999 don't step outside the dates DateOnly holds.
        var day = DateOnly.FromDayNumber(Math.Clamp(week[at]!.Value.DayNumber - at + offset,
            DateOnly.MinValue.DayNumber, DateOnly.MaxValue.DayNumber));
        return GridCalendar.GetWeekOfYear(day.ToDateTime(TimeOnly.MinValue), format.CalendarWeekRule,
            format.FirstDayOfWeek);
    }

    private static readonly GregorianCalendar GridCalendar = new();

    public static IEnumerable<int> WeekDays(CultureInfo culture) => Enumerable.Range(0, 7)
        .Select(day => ((int)culture.DateTimeFormat.FirstDayOfWeek + day) % 7);

    // The day is written unpadded, as its button shows it: a culture's long date can pad it (the invariant culture
    // writes "04"), and the accessible name must contain the visible label.
    public static string DayName(DateOnly date, CultureInfo culture) =>
        date.ToString(PaddedDay().Replace(culture.DateTimeFormat.LongDatePattern, "d"), culture);

    public static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    // The date a pick holds: a month's first day, or January 1 for a year, moved up to min when min falls inside it.
    public static DateOnly Picked(CalendarPick pick, DateOnly date, DateOnly min)
    {
        var first = pick switch
        {
            CalendarPick.Month => FirstOfMonth(date),
            CalendarPick.Year => new DateOnly(date.Year, 1, 1),
            _ => date,
        };
        return first < min ? min : first;
    }

    // What the grid and the date picker's box tell the testing helpers about Pick, or nothing for a day.
    public static string? PickMark(CalendarPick pick) =>
        pick == CalendarPick.Day ? null : pick.ToString().ToLowerInvariant();

    // A month or a year can't be chosen when it lies wholly before min or after max.
    public static bool IsUnavailableMonth(DateOnly month, DateOnly min, DateOnly max) =>
        month < FirstOfMonth(min) || month > max;

    public static bool IsUnavailableYear(int year, DateOnly min, DateOnly max) => year < min.Year || year > max.Year;

    // A culture's short date without its day, such as M/yyyy from M/d/yyyy or MM.yyyy from dd.MM.yyyy.
    public static string MonthPattern(string datePattern) => DayAndSeparator().Replace(datePattern, "", 1);

    // A date before min, after max, or one isDisabled rules out can't be chosen.
    public static bool IsUnavailable<TDate>(DateOnly date, TDate? min, TDate? max, Func<TDate, bool>? isDisabled)
        where TDate : struct =>
        (min is { } first && date < ToDate(first)) || (max is { } last && date > ToDate(last))
        || (isDisabled?.Invoke(FromDate<TDate>(date)) ?? false);

    public static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static DateOnly ToDate<TDate>(TDate value) where TDate : struct =>
        value is DateTime time ? DateOnly.FromDateTime(time) : (DateOnly)(object)value;

    public static TDate FromDate<TDate>(DateOnly date) where TDate : struct => typeof(TDate) == typeof(DateTime)
        ? (TDate)(object)date.ToDateTime(TimeOnly.MinValue)
        : (TDate)(object)date;

    // A date the user left alone keeps its time of day; only a changed date becomes midnight.
    public static TDate? Keep<TDate>(DateOnly? date, TDate? old) where TDate : struct =>
        date is not { } day ? null : old is { } kept && ToDate(kept) == day ? kept : FromDate<TDate>(day);

    // The format as people write it, such as MM/DD/YYYY, in the localizer's letters, for a placeholder and messages.
    public static string FormatHint(string format, ITbLocalizer localizer) => DatePart().Replace(format,
        match => string.Concat(Enumerable.Repeat(localizer[match.Value[0] switch
        {
            'd' => TbStrings.DateHintDay,
            'M' => TbStrings.DateHintMonth,
            _ => TbStrings.DateHintYear,
        }], match.Value[0] == 'y' ? match.Length : 2)));

    // Typed text may leave out a day's or month's leading zero, or give the year in two digits.
    public static bool TryReadDate(string text, string format, CultureInfo culture, out DateOnly date)
    {
        var loose = PaddedMonth().Replace(PaddedDay().Replace(format, "d"), "M");
        return DateOnly.TryParseExact(text.Trim(), [loose, FullYear().Replace(loose, "yy")], culture,
            DateTimeStyles.None, out date);
    }

    [GeneratedRegex("^d{1,2}[^dMy]*|[^dMy]*(?<!d)d{1,2}(?!d)")]
    private static partial Regex DayAndSeparator();

    [GeneratedRegex("(?<!d)dd(?!d)")]
    private static partial Regex PaddedDay();

    [GeneratedRegex("(?<!M)MM(?!M)")]
    private static partial Regex PaddedMonth();

    [GeneratedRegex("(?<!y)yyyy(?!y)")]
    private static partial Regex FullYear();

    [GeneratedRegex("(?<!d)d{1,2}(?!d)|(?<!M)M{1,2}(?!M)|y+")]
    private static partial Regex DatePart();
}
