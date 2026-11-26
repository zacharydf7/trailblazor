using System.Globalization;

namespace Trailblazor;

// Where a chart's items sit along it. On an even scale each item is one place, all the same width. On a continuous
// one each item is placed by its X value, a number or a date, and the axis marks round values in place of items.
internal sealed class ChartAlong
{
    // A time axis has room for about this many marks, and a number axis has about this many steps, as the value
    // axis has.
    private const int MostMarks = 8;
    private const int NumberSteps = 5;

    // A step counts as no shorter than the items' own spacing when it is at least this share of it, so items 31 days
    // apart can still be marked by the month.
    private const double AboutAsLong = 0.9;

    private ChartAlong(IReadOnlyList<double> at, double slot, IReadOnlyList<(double X, string Text)> marks,
        double steps, bool byValue) => (At, Slot, Marks, Steps, ByValue) = (at, slot, marks, steps, byValue);

    // The middle of each item's place, as a share of the plot from its left.
    public IReadOnlyList<double> At { get; }

    // The share of the plot one place takes, split between its bars and the gap beside them.
    public double Slot { get; }

    // A continuous axis's marks, each with its share of the plot from the left and its text. An even axis has none:
    // it labels its places.
    public IReadOnlyList<(double X, string Text)> Marks { get; }

    // How many steps of its marks, or places, fit across the plot, which says how far apart two labels are.
    public double Steps { get; }

    // Whether the items are placed by their values, not one evenly spaced place each.
    public bool ByValue { get; }

    public static ChartAlong Even(int places) => new([.. Enumerable.Range(0, places)
        .Select(place => (place + 0.5) / places)], places == 0 ? 1 : 1d / places, [], places, false);

    // Places each item by its value. With bars, the plot has half the smallest gap between two values as room at
    // each end, and a place is as wide as that gap. `format` writes the items, and `markFormat` the marks; without
    // one, a number mark is written as the items are, and a time mark as its step suits.
    public static ChartAlong Continuous(IReadOnlyList<object?> values, bool bars, string? format, string? markFormat,
        CultureInfo culture)
    {
        if (values.Count == 0)
        {
            return new([], 1, [], 1, true);
        }

        var times = values.All(value => value is DateTime or DateTimeOffset or DateOnly);
        double[] numbers = [.. values.Select(Number)];
        var (first, last) = (numbers.Min(), numbers.Max());
        if (first == last)
        {
            // One value has the whole plot as its place, and the axis marks it alone.
            return new([.. numbers.Select(_ => 0.5)], 1,
                [(0.5, ChartValues.Text(values[0], markFormat ?? format, culture))], 1, true);
        }

        var distinct = numbers.Distinct().Order().ToArray();
        var gap = distinct.Zip(distinct.Skip(1), (one, next) => next - one).Min();
        if (bars)
        {
            (first, last) = (first - gap / 2, last + gap / 2);
        }

        var span = last - first;
        List<(double X, string Text)> marks;
        double steps;
        if (times)
        {
            var (moments, kind, count) = ChartTimeMarks.Between(new((long)first), new((long)last), MostMarks,
                new((long)(gap * AboutAsLong)));
            var pattern = markFormat ?? Short(kind, culture);
            marks = [.. moments.Select(moment => ((moment.Ticks - first) / span, moment.ToString(pattern, culture)))];
            steps = count;
        }
        else
        {
            // Whole numbers aren't marked by the half: the step is no shorter than the items' spacing. Where no two
            // marks that far apart lie on the axis, as between 1 and 4, it is the longest step that gives two.
            var step = Math.Max(ChartTicks.StepFor(span, NumberSteps), ChartTicks.StepNoLessThan(gap * AboutAsLong));
            while (new ChartTicks(first, last, step).Values.Count < 2)
            {
                step = ChartTicks.StepBelow(step);
            }

            var ticks = new ChartTicks(first, last, step);
            // Marks are written without digit grouping, as the items are: a year reads 2026, not 2,026.
            var pattern = markFormat ?? format ?? "F" + ticks.Decimals;
            // A format such as "D4" suits whole-number types only, so a whole mark of such items is written as a
            // whole number: a long, which holds any mark that an axis of smaller types has.
            var whole = Type.GetTypeCode(values[0]!.GetType()) is >= TypeCode.SByte and <= TypeCode.UInt64;
            marks = [.. ticks.Values.Select(value => ((value - first) / span,
                whole && value == Math.Floor(value) && Math.Abs(value) < long.MaxValue
                    ? ((long)value).ToString(pattern, culture) : value.ToString(pattern, culture)))];
            steps = span / ticks.Step;
        }

        return new([.. numbers.Select(number => (number - first) / span)], gap / span, marks, steps, true);
    }

    // A day or a month reads with its month's short name, in the order the culture writes them.
    private static string Short(string kind, CultureInfo culture) => kind switch
    {
        "M" => culture.DateTimeFormat.MonthDayPattern.Replace("MMMM", "MMM", StringComparison.Ordinal),
        "Y" => culture.DateTimeFormat.YearMonthPattern.Replace("MMMM", "MMM", StringComparison.Ordinal),
        _ => kind,
    };

    // A date counts in ticks, so dates and numbers are placed the same way.
    private static double Number(object? value) => value switch
    {
        DateTime moment => moment.Ticks,
        DateTimeOffset moment => moment.DateTime.Ticks,
        DateOnly date => date.ToDateTime(TimeOnly.MinValue).Ticks,
        not null when ChartValues.IsNumber(value)
            && Convert.ToDouble(value, CultureInfo.InvariantCulture) is var number && double.IsFinite(number) => number,
        _ => throw new InvalidOperationException("A chart with a continuous scale needs every item's X to be a "
            + $"number or a date, and got {(value is null ? "null" : $"{value} ({value.GetType().Name})")}."),
    };
}
