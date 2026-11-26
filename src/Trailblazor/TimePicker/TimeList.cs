using System.Globalization;

namespace Trailblazor;

// The list of times that the time picker and the date-time picker's time box share.
internal static class TimeList
{
    // A step under five minutes would make the list too long to scroll, so the list goes by half hours then.
    public static List<TimeOnly> From(TimeOnly first, TimeSpan? step, Func<TimeOnly, bool> takes)
    {
        var by = step is { } given && given >= TimeSpan.FromMinutes(5) ? given : TimeSpan.FromMinutes(30);
        var times = new List<TimeOnly>();
        for (var offset = TimeSpan.Zero; offset < TimeSpan.FromDays(1); offset += by)
        {
            var time = first.Add(offset);
            if (takes(time))
            {
                times.Add(time);
            }
        }

        return times;
    }

    // The list can run past midnight, so a time's place is how far it comes after the list's first.
    public static TimeOnly? Start(List<TimeOnly> times, TimeOnly? chosen)
    {
        if (chosen is not { } time || times.Count == 0)
        {
            return null;
        }

        var first = times[0];
        var after = times.FindIndex(listed => listed - first >= time - first);
        return times[after < 0 ? 0 : after];
    }

    // A 24-hour time, which the browser's own box and the testing helpers give, reads in every culture.
    public static bool TryRead(string text, CultureInfo culture, out TimeOnly time) =>
        TimeOnly.TryParseExact(text.Trim(), ["H:mm", "HH:mm:ss", "HH:mm:ss.FFF"], CultureInfo.InvariantCulture,
            DateTimeStyles.None, out time)
        || TimeOnly.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out time);
}
