using System.Globalization;

namespace Trailblazor;

// What the app, or the kind of chart, asks of a continuous scale's ends and marks: the values at the plot's start and
// end, the distance between the marks of a number scale, and whether an end left to the chart sits on a mark, as a
// scatter's does, so that its outermost items are not on the plot's edge.
internal readonly record struct ChartAlongEnds(object? Min, object? Max, double? Step, bool Round);

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

    // The value at a continuous scale's start, how far it is to its end, which is zero for a scale of one value, and
    // whether it counts dates, for placing a value that isn't an item's.
    private readonly (double First, double Span, bool Times)? _scale;

    private ChartAlong(IReadOnlyList<double> at, double slot, IReadOnlyList<(double X, string Text)> marks,
        double steps, bool byValue, (double First, double Span, bool Times)? scale = null) =>
        (At, Slot, Marks, Steps, ByValue, _scale) = (at, slot, marks, steps, byValue, scale);

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
        CultureInfo culture, ChartAlongEnds ends = default)
    {
        if (values.Count == 0)
        {
            return new([], 1, [], 1, true);
        }

        var times = values.All(IsTime);
        double[] numbers = [.. values.Select(Number)];
        var (first, last) = (numbers.Min(), numbers.Max());
        var distinct = numbers.Distinct().Order().ToArray();
        // The smallest gap between two values, or null when every value is the same.
        double? apart = distinct.Length > 1 ? distinct.Zip(distinct.Skip(1), (one, next) => next - one).Min() : null;
        if (bars && apart is { } room)
        {
            (first, last) = (first - room / 2, last + room / 2);
        }

        (first, last) = Between(first, last, ends, times);
        if (first == last)
        {
            // One value has the whole plot as its place, and the axis marks it alone.
            return new([.. numbers.Select(_ => 0.5)], 1,
                [(0.5, ChartValues.Text(values[0], markFormat ?? format, culture))], 1, true, (first, 0, times));
        }

        // Values that are all the same lie between ends the app set, with a place as wide as a step of that range.
        var span = last - first;
        var gap = apart ?? ChartTicks.StepFor(span, NumberSteps);
        if (bars && apart is null)
        {
            // Their bars have half a place of room at an end the app left to the chart, as other bars do.
            (first, last) = (ends.Min is null ? first - gap / 2 : first, ends.Max is null ? last + gap / 2 : last);
            span = last - first;
        }
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
            if (ends.Step is { } given && span / given < ChartTicks.MostSteps
                && new ChartTicks(first, last, given).Values.Count >= 2)
            {
                step = given;
            }
            else
            {
                while (new ChartTicks(first, last, step).Values.Count < 2)
                {
                    step = ChartTicks.StepBelow(step);
                }
            }

            if (ends.Round)
            {
                // An end left to the chart moves out to the nearest mark, and an end the app set stays as given.
                var reach = ChartTicks.Reach(first, last, step);
                (first, last) = (ends.Min is null ? reach.Min : first, ends.Max is null ? reach.Max : last);
                span = last - first;
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

        return new([.. numbers.Select(number => (number - first) / span)], gap / span, marks, steps, true,
            (first, span, times));
    }

    // What a value has to be to have a place on this continuous scale: "a date" or "a finite number".
    public string Kind => _scale is { Times: true } ? "a date" : "a finite number";

    // Whether a value can be placed on this continuous scale: a date on a scale of dates, and a finite number on a
    // scale of numbers, as an end the app gives the axis has to be.
    public bool Fits(object value) => _scale is { } scale && (scale.Times ? IsTime(value)
        : ChartValues.IsNumber(value) && double.IsFinite(Convert.ToDouble(value, CultureInfo.InvariantCulture)));

    // Where a value that fits lies along a continuous scale, as a share of the plot from its left: below 0 or above 1
    // beyond the plot's ends. A scale of one value has it in the middle and nothing else on it. Null on an even
    // scale, where only the items have places.
    public double? Share(object value) => _scale is not { } scale ? null
        : scale.Span == 0 ? (Number(value) == scale.First ? 0.5 : double.NaN)
        : (Number(value) - scale.First) / scale.Span;

    // The plot's ends: the app's where it set them, and the items' own otherwise. An end the app set beyond every
    // item still needs a plot with some width, so the other end moves as far from it as the items span.
    private static (double First, double Last) Between(double first, double last, ChartAlongEnds ends, bool times)
    {
        bool Fits(object? end) => end is null || (times ? IsTime(end) : ChartValues.IsNumber(end));
        if (!Fits(ends.Min) || !Fits(ends.Max))
        {
            throw new InvalidOperationException($"{nameof(TbXAxis)} needs a {nameof(TbXAxis.Min)} and a "
                + $"{nameof(TbXAxis.Max)} of the kind its items' X values are: numbers for numbers, dates for dates.");
        }

        var width = last > first ? last - first : 1;
        var (min, max) = (ends.Min is null ? first : Number(ends.Min), ends.Max is null ? last : Number(ends.Max));
        return max > min || (ends.Min is null && ends.Max is null) ? (min, max)
            : ends.Max is null ? (min, min + width) : (max - width, max);
    }

    // Whether an axis's two ends are in order. Ends that aren't both numbers or both dates can't be compared, and
    // are told apart from the items once there are items.
    public static bool InOrder(object? min, object? max) => min is null || max is null
        || IsTime(min) != IsTime(max) || !(IsTime(min) || (ChartValues.IsNumber(min) && ChartValues.IsNumber(max)))
        || Number(min) < Number(max);

    private static bool IsTime(object? value) => value is DateTime or DateTimeOffset or DateOnly;

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
