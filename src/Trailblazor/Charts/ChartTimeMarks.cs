namespace Trailblazor;

// Round times for a time axis to mark: a step from a second to a century, on boundaries of the clock and the
// calendar, such as the hour or the first of a month.
internal static class ChartTimeMarks
{
    private enum Unit
    {
        Second,
        Minute,
        Hour,
        Day,
        Month,
        Year,
    }

    // The steps an axis may take, shortest first, each with the kind of thing its marks are: a time to the second
    // ("T"), a time ("t"), a day of a month ("M"), a month of a year ("Y") or a year.
    private static readonly (Unit Unit, int Count, string Format)[] Steps =
    [
        (Unit.Second, 1, "T"), (Unit.Second, 5, "T"), (Unit.Second, 15, "T"), (Unit.Second, 30, "T"),
        (Unit.Minute, 1, "t"), (Unit.Minute, 5, "t"), (Unit.Minute, 15, "t"), (Unit.Minute, 30, "t"),
        (Unit.Hour, 1, "t"), (Unit.Hour, 3, "t"), (Unit.Hour, 6, "t"), (Unit.Hour, 12, "t"),
        (Unit.Day, 1, "M"), (Unit.Day, 2, "M"), (Unit.Day, 7, "M"), (Unit.Day, 14, "M"),
        (Unit.Month, 1, "Y"), (Unit.Month, 3, "Y"), (Unit.Month, 6, "Y"),
        (Unit.Year, 1, "yyyy"), (Unit.Year, 2, "yyyy"), (Unit.Year, 5, "yyyy"), (Unit.Year, 10, "yyyy"),
        (Unit.Year, 20, "yyyy"), (Unit.Year, 50, "yyyy"), (Unit.Year, 100, "yyyy"),
    ];

    // The marks from one time to another, the format that says what kind they are, and how many steps fit between
    // the two times. The step is the shortest that gives no more than `most` marks and is no shorter than `finest`,
    // the items' own spacing, so daily values aren't marked by the hour.
    public static (IReadOnlyList<DateTime> Marks, string Format, double Steps) Between(DateTime from, DateTime to,
        int most, TimeSpan finest = default)
    {
        var span = (to - from).Ticks;
        var marking = Steps.Where(one => First(from, one.Unit, one.Count) <= to).ToList();
        if (marking.Count == 0)
        {
            // No round time lies between the two, so the axis marks its ends.
            return ([from, to], Steps[0].Format, 1);
        }

        var roomy = marking.Where(one => span / Length(one.Unit, one.Count) <= most).ToList();
        var step = marking[^1];
        if (roomy.Count > 0)
        {
            // The step is the first as long as the items' spacing, or the longest there is room for. Two items far
            // apart may have fewer than two round times between them at that step, such as no new year between
            // March and November. The longest shorter step that puts two marks there then marks the axis.
            var spaced = roomy.FindIndex(one => Length(one.Unit, one.Count) >= finest.Ticks);
            var longest = spaced >= 0 ? spaced : roomy.Count - 1;
            step = roomy.Take(longest + 1).LastOrDefault(one => Marks(from, to, one.Unit, one.Count).Count >= 2,
                roomy[longest]);
        }

        return (Marks(from, to, step.Unit, step.Count), step.Format, span / Length(step.Unit, step.Count));
    }

    private static List<DateTime> Marks(DateTime from, DateTime to, Unit unit, int count)
    {
        var marks = new List<DateTime>();
        for (var mark = First(from, unit, count); mark <= to; mark = Next(mark, unit, count))
        {
            marks.Add(mark);
        }

        return marks;
    }

    // About how long a step is, in ticks: months and years by their mean length.
    private static double Length(Unit unit, int count) => count * unit switch
    {
        Unit.Second => TimeSpan.TicksPerSecond,
        Unit.Minute => TimeSpan.TicksPerMinute,
        Unit.Hour => TimeSpan.TicksPerHour,
        Unit.Day => TimeSpan.TicksPerDay,
        Unit.Month => TimeSpan.TicksPerDay * 30.4375,
        _ => TimeSpan.TicksPerDay * 365.25,
    };

    // The first mark at or after a time: a whole number of steps into its day for times, the start of a day for
    // days, a year whose number divides by the step, and a month a whole number of steps after January.
    private static DateTime First(DateTime from, Unit unit, int count)
    {
        switch (unit)
        {
            case Unit.Year:
                var year = new DateTime(from.Year, 1, 1);
                year = year < from ? year.AddYears(1) : year;
                return year.AddYears((count - year.Year % count) % count);
            case Unit.Month:
                var month = new DateTime(from.Year, from.Month, 1);
                month = month < from ? month.AddMonths(1) : month;
                return month.AddMonths((count - (month.Month - 1) % count) % count);
            case Unit.Day:
                return from.Date < from ? from.Date.AddDays(1) : from.Date;
            default:
                var length = (long)Length(unit, count);
                return from.Date.AddTicks((from.TimeOfDay.Ticks + length - 1) / length * length);
        }
    }

    private static DateTime Next(DateTime mark, Unit unit, int count) => unit switch
    {
        Unit.Year => mark.AddYears(count),
        Unit.Month => mark.AddMonths(count),
        _ => mark.AddTicks((long)Length(unit, count)),
    };
}
