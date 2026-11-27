using System.Globalization;
using System.Numerics;

namespace Trailblazor;

// One series as a chart was given it: a value for each place along the chart, null where there is none. A hidden
// series isn't drawn and has no say in the axis, and its values are still written for the data table. A filled one
// is an area: a line with what is under it filled.
internal sealed record ChartSeriesValues(string Name, bool Bars, ChartCurve Curve, IReadOnlyList<double?> Values,
    bool Hidden = false, bool Filled = false);

// The value axis as the app set it; null leaves a choice to the chart.
internal readonly record struct ChartValueAxis(double? Min, double? Max, double? Step, string? Format);

// A bar as shares of the plot, from its left and top. Its value end is the one that gets rounded corners; a bar
// inside a stack has none, nor has one cut off at the plot's edge, and Up says which way it grows from the bar
// before it.
internal readonly record struct ChartBar(double X, double Y, double Width, double Height, ChartBarEnd End,
    bool Up = true);

internal enum ChartBarEnd
{
    // Against the next bar of its stack, with a seam between the two.
    None,
    Top,
    Bottom,

    // Cut off at the plot's edge, because the bar's value is beyond the axis.
    Cut,
}

// One series as a chart draws it: bars, or a line's path with the points it passes through, and for an area the
// shape under that line. A hidden series has only its text.
internal sealed record ChartSeriesPlan(string Name, bool Bars, string Path, IReadOnlyList<(double X, double Y)?> Points,
    IReadOnlyList<ChartBar?> Marks, IReadOnlyList<string?> Text, bool Hidden = false, string Fill = "");

// Everything a chart draws, worked out from its values: where each mark, grid line and label goes, as shares of the
// plot, so the markup is the same at every size and nothing is measured.
internal sealed class ChartPlan
{
    // The share of a place that its bars take; the rest is the gap between one place's bars and the next's.
    private const double BarsShare = 0.7;

    // The share of its slot that a bar beside others fills, so neighbors don't touch.
    private const double BesideShare = 0.9;

    // No plot has room for more labels than this, so a longer axis leaves the rest out of the markup.
    private const int MostLabels = 48;

    private ChartPlan(IReadOnlyList<string> places, ChartTicks ticks, IReadOnlyList<ChartSeriesPlan> series,
        IReadOnlyList<(double Y, string Text, bool Zero)> valueTicks, ChartAlong along,
        Func<double, string> referenceText)
    {
        ReferenceText = referenceText;
        Places = places;
        Ticks = ticks;
        Series = series;
        ValueTicks = valueTicks;
        Along = along;
        LabelEvery = 1;
        while (places.Count / LabelEvery > MostLabels)
        {
            LabelEvery *= 2;
        }
    }

    public IReadOnlyList<string> Places { get; }

    // Where the places are along the chart.
    public ChartAlong Along { get; }

    public ChartTicks Ticks { get; }

    public IReadOnlyList<ChartSeriesPlan> Series { get; }

    // Each tick of the value axis: its share of the plot from the top, its label, and whether it is the zero line.
    public IReadOnlyList<(double Y, string Text, bool Zero)> ValueTicks { get; }

    // Writes a value of the axis for a reference line or band: as the data table writes the series' values, or as
    // the axis writes its marks on a chart of shares, whose table holds amounts and not shares.
    public Func<double, string> ReferenceText { get; }

    // A long axis renders a label for every second, fourth or eighth place, and so on.
    public int LabelEvery { get; }

    // The middle of a place, as a share of the plot from its left.
    public double Middle(int place) => Along.At[place];

    // A place's label shows whenever labels this many places apart have room, so thinning keeps them evenly spaced:
    // every eighth, then every fourth, and so on. The first label's stride is the widest the axis holds, so it shows
    // on any plot with room for one label.
    public int Stride(int place) => Stride(place, Places.Count);

    // The labels along the chart: each with its share of the plot from the left, its text, and its stride. An even
    // scale labels its places, and a continuous one its marks.
    public IEnumerable<(double X, string Text, int Stride)> Labels => Along.ByValue
        ? Along.Marks.Select((mark, index) => (mark.X, mark.Text, Stride(index, Along.Marks.Count)))
        : Enumerable.Range(0, (Places.Count + LabelEvery - 1) / LabelEvery).Select(index => index * LabelEvery)
            .Select(place => (Middle(place), Places[place], Stride(place)));

    internal static int Stride(int index, int count) =>
        index == 0 ? (int)BitOperations.RoundUpToPowerOf2((uint)count) : index & -index;

    // `alsoReach` are values the axis has to reach besides the series' own, such as a reference line's.
    public static ChartPlan Build(IReadOnlyList<string> places, IReadOnlyList<ChartSeriesValues> series,
        ChartStacking stacking, ChartValueAxis axis, CultureInfo culture, ChartAlong? along = null,
        IReadOnlyList<double>? alsoReach = null)
    {
        along ??= ChartAlong.Even(places.Count);
        // Bars pile with bars and areas with areas.
        var bars = series.Where(one => one.Bars && !one.Hidden).ToList();
        var areas = series.Where(one => one.Filled && !one.Hidden).ToList();
        var (piled, filled) = (Pile(bars, stacking), Pile(areas, stacking));

        // Bars and areas are read from zero, so their axis has to reach it; lines alone may start wherever the values
        // do.
        var reached = piled.Concat(filled).SelectMany(one => one).OfType<(double From, double To)>()
            .SelectMany(part => new[] { part.From, part.To })
            .Concat(series.Where(one => !one.Bars && !one.Filled && !one.Hidden).SelectMany(one => one.Values)
                .OfType<double>())
            .ToList();
        reached.AddRange(alsoReach ?? []);
        var shares = stacking == ChartStacking.Percent && bars.Count + areas.Count > 0;
        var ticks = TicksFor(reached, axis, shares);

        var format = axis.Format ?? (shares ? "P0" : "N" + ticks.Decimals);
        var valueTicks = ticks.Values
            .Select(value => (1 - ticks.Share(value), value.ToString(format, culture), value == 0)).ToList();

        // A value shows up to three decimals, or one more than the axis's marks have when they are finer than that,
        // so the table never reads a small value as zero.
        var valueFormat = axis.Format ?? "#,0." + new string('#', Math.Max(3, ticks.Decimals + 1));
        var planned = new List<ChartSeriesPlan>(series.Count);
        foreach (var one in series)
        {
            var text = one.Values.Select(value => value?.ToString(valueFormat, culture)).ToList();
            if (one.Hidden)
            {
                planned.Add(new(one.Name, one.Bars, "", [], [], text, Hidden: true));
            }
            else if (one.Bars)
            {
                var slot = bars.IndexOf(one);
                planned.Add(new(one.Name, true, "", [], Bars(piled, slot, along, stacking, ticks), text));
            }
            else if (one.Filled)
            {
                // An area's line runs along the top of its part of the pile, and its floor along the bottom.
                var parts = filled[areas.IndexOf(one)];
                (double X, double Y)?[] points = [.. parts.Select((part, place) => part is { } at
                    ? ((double, double)?)(along.At[place], 1 - ticks.Share(at.To)) : null)];
                (double X, double Y)?[] floor = [.. parts.Select((part, place) => part is { } at
                    ? ((double, double)?)(along.At[place], 1 - ticks.Share(at.From)) : null)];
                planned.Add(new(one.Name, false, ChartPath.Line(points, one.Curve), points, [], text,
                    Fill: ChartPath.Area(points, floor, one.Curve)));
            }
            else
            {
                (double X, double Y)?[] points = [.. one.Values.Select((value, place) =>
                    value is { } at ? ((double, double)?)(along.At[place], 1 - ticks.Share(at)) : null)];
                planned.Add(new(one.Name, false, ChartPath.Line(points, one.Curve), points, [], text));
            }
        }

        return new(places, ticks, planned, valueTicks, along,
            value => value.ToString(shares ? format : valueFormat, culture));
    }

    // Where each value of these series starts and ends: from zero, or on top of the series before it.
    private static (double From, double To)?[][] Pile(List<ChartSeriesValues> series, ChartStacking stacking) =>
        stacking == ChartStacking.None
            ? [.. series.Select(one => one.Values.Select(value => value is { } to ? ((double, double)?)(0, to) : null)
                .ToArray())]
            : ChartStack.Pile([.. series.Select(one => one.Values)], stacking == ChartStacking.Percent);

    private static ChartTicks TicksFor(List<double> reached, ChartValueAxis axis, bool shares)
    {
        var lowest = axis.Min ?? (reached.Count == 0 ? 0 : reached.Min());
        var highest = axis.Max ?? (shares ? 1 : reached.Count == 0 ? 1 : reached.Max());
        // An end left to the chart sits on a multiple of the step, the app's or the chart's own. An end the app set
        // is kept as given, even off a step, and the marks stay on multiples of the step.
        var ticks = axis.Step is { } step && (highest - lowest) / step < ChartTicks.MostSteps
            ? ChartTicks.Reach(lowest, highest, step) : ChartTicks.Cover(lowest, highest);
        var (min, max) = (axis.Min ?? ticks.Min, axis.Max ?? ticks.Max);
        if (max > min)
        {
            return new(min, max, ticks.Step);
        }

        // An end the app set beyond every value still needs an axis with some height: the other end moves.
        return axis is { Min: null, Max: not null }
            ? new(max - ticks.Step, max, ticks.Step) : new(min, min + ticks.Step, ticks.Step);
    }

    private static ChartBar?[] Bars((double From, double To)?[][] piled, int slot, ChartAlong along,
        ChartStacking stacking, ChartTicks ticks)
    {
        var stacked = stacking != ChartStacking.None;
        var places = along.At.Count;
        var slotWidth = BarsShare * along.Slot / (stacked ? 1 : piled.Length);
        var width = slotWidth * (stacked || piled.Length == 1 ? 1 : BesideShare);
        var marks = new ChartBar?[places];
        for (var place = 0; place < places; place++)
        {
            if (piled[slot][place] is not { } bar)
            {
                continue;
            }

            var left = along.At[place] - BarsShare * along.Slot / 2 + (stacked ? 0 : slot * slotWidth)
                + (slotWidth - width) / 2;
            // A bar stops at the plot's edge when an end of the axis that the app set is short of it.
            var top = Math.Clamp(1 - ticks.Share(Math.Max(bar.From, bar.To)), 0, 1);
            var bottom = Math.Clamp(1 - ticks.Share(Math.Min(bar.From, bar.To)), 0, 1);
            var up = bar.To >= bar.From;
            // In a stack only the bar farthest from zero on its side has a free end. A bar whose value is beyond the
            // axis is cut off square: a rounded end would say the value is there. A stack's total can be a rounding
            // error past the axis's end, which is no reason to cut it.
            var outermost = !stacked || !piled.Skip(slot + 1).Any(later =>
                later[place] is { } other && other.To != other.From && other.To >= other.From == up);
            var cut = ticks.Share(bar.To) is < -ChartTicks.Slack or > 1 + ChartTicks.Slack;
            marks[place] = new(left, top, width, bottom - top, !outermost ? ChartBarEnd.None
                : cut ? ChartBarEnd.Cut : up ? ChartBarEnd.Top : ChartBarEnd.Bottom, up);
        }

        return marks;
    }
}
