using System.Globalization;

namespace Trailblazor;

// A moment's distance from now in words, such as "in 3 days", and how soon those words can change.
internal static class RelativeTime
{
    private static readonly TimeSpan JustNow = TimeSpan.FromSeconds(45);

    public static (string Text, TimeSpan Refresh) Of(DateTimeOffset value, DateTimeOffset now, ITbLocalizer localizer)
    {
        var span = (value - now).Duration();
        if (span < JustNow)
        {
            return (localizer[TbStrings.TimestampJustNow], TimeSpan.FromSeconds(15));
        }

        var (count, past, ahead, refresh) = span switch
        {
            { TotalMinutes: < 45 } => (span.TotalMinutes, TbStrings.TimestampMinutesAgo, TbStrings.TimestampInMinutes,
                TimeSpan.FromSeconds(30)),
            { TotalHours: < 22 } => (span.TotalHours, TbStrings.TimestampHoursAgo, TbStrings.TimestampInHours,
                TimeSpan.FromMinutes(5)),
            { TotalDays: < 26 } => (span.TotalDays, TbStrings.TimestampDaysAgo, TbStrings.TimestampInDays,
                TimeSpan.FromHours(1)),
            { TotalDays: < 320 } => (span.TotalDays / 30.44, TbStrings.TimestampMonthsAgo,
                TbStrings.TimestampInMonths, TimeSpan.FromDays(1)),
            _ => (span.TotalDays / 365.25, TbStrings.TimestampYearsAgo, TbStrings.TimestampInYears,
                TimeSpan.FromDays(1)),
        };
        var whole = Math.Max(1, (int)Math.Round(count, MidpointRounding.AwayFromZero));
        var words = localizer.Plural(value > now ? ahead : past, whole);
        return (string.Format(CultureInfo.CurrentCulture, words, whole), refresh);
    }
}
