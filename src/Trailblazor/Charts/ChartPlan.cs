using System.Globalization;
using System.Numerics;

namespace Trailblazor;

// One series as a chart was given it: a value for each place along the chart, null where there is none. A hidden
// series isn't drawn and has no say in the axis, and its values are still written for the data table. A filled one
// is an area: a line with what is under it filled. Before and After are a line's values at the plot's start and end
// where a zoomed view cuts it between two items, so it runs on to the plot's edges. A line with markers at its
// points is drawn through every one of them, so no marker sits off it.
internal sealed record ChartSeriesValues(string Name, bool Bars, ChartCurve Curve, IReadOnlyList<double?> Values,
    bool Hidden = false, bool Filled = false, double? Before = null, double? After = null, bool Marked = false);

// The value axis as the app set it; null leaves a choice to the chart.
internal readonly record struct ChartValueAxis(double? Min, double? Max, double? Step, string? Format,
    bool Log = false);

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

    // `alsoReach` are values the axis has to reach besides the series' own, such as a reference line's. A line or
    // an area with more than `most` points is drawn through about that many of them; 0 draws them all, and so does a
    // series with markers.
    public static ChartPlan Build(IReadOnlyList<string> places, IReadOnlyList<ChartSeriesValues> series,
        ChartStacking stacking, ChartValueAxis axis, CultureInfo culture, ChartAlong? along = null,
        IReadOnlyList<double>? alsoReach = null, int most = 0)
    {
        along ??= ChartAlong.Even(places.Count);
        // On a logarithmic axis a value of zero or below has no place: it is drawn as a gap, and the data table still
        // writes it.
        var given = series;
        static double? Placed(double? value) => value > 0 ? value : null;
        if (axis.Log)
        {
            series = [.. series.Select(one => one with
            {
                Values = [.. one.Values.Select(Placed)], Before = Placed(one.Before), After = Placed(one.After),
            })];
        }

        // Bars pile with bars and areas with areas.
        var bars = series.Where(one => one.Bars && !one.Hidden).ToList();
        var areas = series.Where(one => one.Filled && !one.Hidden).ToList();
        // A line or an area with values at the plot's edges is drawn out to them, before its first place and after
        // its last; only its places have points.
        var edged = series.Any(one => one.Before is not null || one.After is not null);
        IReadOnlyList<double?> Drawn(ChartSeriesValues one) =>
            edged ? [one.Before, .. one.Values, one.After] : one.Values;
        double At(int index) =>
            !edged ? along.At[index] : index == 0 ? 0 : index > places.Count ? 1 : along.At[index - 1];
        IReadOnlyList<(double X, double Y)?> Kept((double X, double Y)?[] points) => edged ? points[1..^1] : points;
        var (piled, filled) = (Pile([.. bars.Select(one => one.Values)], stacking),
            Pile([.. areas.Select(Drawn)], stacking));

        // Bars and areas are read from zero, so their axis has to reach it; lines alone may start wherever the values
        // do.
        var reached = piled.Concat(filled).SelectMany(one => one).OfType<(double From, double To)>()
            .SelectMany(part => new[] { part.From, part.To })
            .Concat(series.Where(one => !one.Bars && !one.Filled && !one.Hidden).SelectMany(Drawn).OfType<double>())
            .ToList();
        reached.AddRange(alsoReach ?? []);
        var shares = stacking == ChartStacking.Percent && bars.Count + areas.Count > 0;
        var ticks = TicksFor(reached, axis, shares, bars.Count > 0);

        // A logarithmic axis writes each power of ten with only the decimals it needs: 0.01, 0.1, 1 and 10.
        var format = axis.Format ?? (shares ? "P0"
            : ticks.Log ? "#,0." + new string('#', Math.Max(1, ticks.Decimals)) : "N" + ticks.Decimals);
        var valueTicks = ticks.Values
            .Select(value => (1 - ticks.Share(value), value.ToString(format, culture), value == 0)).ToList();

        // A value shows up to three decimals, or one more than the axis's marks have when they are finer than that,
        // so the table never reads a small value as zero.
        var valueFormat = axis.Format ?? "#,0." + new string('#', Math.Max(3, ticks.Decimals + 1));
        (double X, double Y)?[] Tops((double From, double To)?[] parts) => [.. parts.Select((part, place) =>
            part is { } at ? ((double, double)?)(At(place), 1 - ticks.Share(at.To)) : null)];
        int Most(ChartSeriesValues one) => one.Marked ? 0 : most;
        // The areas of a stack are drawn through the same places, so they still meet.
        List<int>? stackPlaces = stacking == ChartStacking.None ? null : [.. filled
            .SelectMany((parts, index) => ChartThinning.Keep(Tops(parts), Most(areas[index]))).Distinct().Order()];
        (double X, double Y)?[] Thin((double X, double Y)?[] points, IEnumerable<int> keep) =>
            [.. keep.Select(place => points[place])];

        var planned = new List<ChartSeriesPlan>(series.Count);
        for (var index = 0; index < series.Count; index++)
        {
            var one = series[index];
            var text = given[index].Values.Select(value => value?.ToString(valueFormat, culture)).ToList();
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
                var points = Tops(parts);
                (double X, double Y)?[] floor = [.. parts.Select((part, place) => part is { } at
                    ? ((double, double)?)(At(place), 1 - ticks.Share(at.From)) : null)];
                var keep = stackPlaces ?? ChartThinning.Keep(points, Most(one));
                var (line, under) = (Thin(points, keep), Thin(floor, keep));
                planned.Add(new(one.Name, false, ChartPath.Line(line, one.Curve), Kept(points), [], text,
                    Fill: ChartPath.Area(line, under, one.Curve)));
            }
            else
            {
                (double X, double Y)?[] points = [.. Drawn(one).Select((value, place) =>
                    value is { } at ? ((double, double)?)(At(place), 1 - ticks.Share(at)) : null)];
                planned.Add(new(one.Name, false, ChartPath.Line(Thin(points, ChartThinning.Keep(points, Most(one))),
                    one.Curve), Kept(points), [], text));
            }
        }

        return new(places, ticks, planned, valueTicks, along,
            value => value.ToString(shares ? format : valueFormat, culture));
    }

    // Where each value of these series starts and ends: from zero, or on top of the series before it.
    private static (double From, double To)?[][] Pile(List<IReadOnlyList<double?>> series, ChartStacking stacking) =>
        stacking == ChartStacking.None
            ? [.. series.Select(values => values.Select(value => value is { } to ? ((double, double)?)(0, to) : null)
                .ToArray())]
            : ChartStack.Pile(series, stacking == ChartStacking.Percent);

    private static ChartTicks TicksFor(List<double> reached, ChartValueAxis axis, bool shares, bool bars)
    {
        if (axis.Log)
        {
            // Only values above zero have a place, and an axis with none runs from 1 to 10. With bars the axis starts
            // at the power of ten just below the lowest value, so the lowest bar has some height, as a bar from zero
            // would.
            List<double> above = [.. reached.Where(value => value > 0)];
            var least = above.Count == 0 ? 1 : above.Min();
            least = bars ? Math.Pow(10, Math.Ceiling(Math.Log10(least) - ChartTicks.Slack) - 1) : least;
            var powers = ChartTicks.Powers(axis.Min ?? least, axis.Max ?? (above.Count == 0 ? 10 : above.Max()));
            var (low, high) = (axis.Min ?? powers.Min, axis.Max ?? powers.Max);
            // An end the app set beyond every value still needs an axis with some height: the other end moves a
            // power of ten.
            return high > low ? powers with { Min = low, Max = high }
                : axis.Max is null ? powers with { Min = low, Max = low * 10 }
                : powers with { Min = high / 10, Max = high };
        }

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
