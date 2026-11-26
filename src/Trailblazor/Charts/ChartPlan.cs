using System.Globalization;
using System.Numerics;

namespace Trailblazor;

// One series as a chart was given it: a value for each place along the chart, null where there is none.
internal sealed record ChartSeriesValues(string Name, bool Bars, ChartCurve Curve, IReadOnlyList<double?> Values);

// The value axis as the app set it; null leaves a choice to the chart.
internal readonly record struct ChartValueAxis(double? Min, double? Max, double? Step, string? Format);

// A bar as shares of the plot, from its left and top. Its value end is the one that gets rounded corners; a bar
// inside a stack has none.
internal readonly record struct ChartBar(double X, double Y, double Width, double Height, ChartBarEnd End);

internal enum ChartBarEnd
{
    None,
    Top,
    Bottom,
}

// One series as a chart draws it: bars, or a line's path with the points it passes through.
internal sealed record ChartSeriesPlan(string Name, bool Bars, string Path, IReadOnlyList<(double X, double Y)?> Points,
    IReadOnlyList<ChartBar?> Marks, IReadOnlyList<string?> Text);

// Everything a chart draws, worked out from its values: where each mark, grid line and label goes, as shares of the
// plot, so the markup is the same at every size and nothing is measured.
internal sealed class ChartPlan
{
    // The share of a place that its bars take; the rest is the gap between one place's bars and the next's.
    private const double BarsShare = 0.7;

    // No plot has room for more labels than this, so a longer axis leaves the rest out of the markup.
    private const int MostLabels = 48;

    private ChartPlan(IReadOnlyList<string> places, ChartTicks ticks, IReadOnlyList<ChartSeriesPlan> series,
        IReadOnlyList<(double Y, string Text, bool Zero)> valueTicks)
    {
        Places = places;
        Ticks = ticks;
        Series = series;
        ValueTicks = valueTicks;
        LabelEvery = 1;
        while (places.Count / LabelEvery > MostLabels)
        {
            LabelEvery *= 2;
        }
    }

    public IReadOnlyList<string> Places { get; }

    public ChartTicks Ticks { get; }

    public IReadOnlyList<ChartSeriesPlan> Series { get; }

    // Each tick of the value axis: its share of the plot from the top, its label, and whether it is the zero line.
    public IReadOnlyList<(double Y, string Text, bool Zero)> ValueTicks { get; }

    // A long axis renders a label for every second, fourth or eighth place, and so on.
    public int LabelEvery { get; }

    // The middle of a place, as a share of the plot from its left.
    public double Middle(int place) => (place + 0.5) / Places.Count;

    // A place's label shows whenever labels this many places apart have room, so thinning keeps them evenly spaced:
    // every eighth, then every fourth, and so on. The first label's stride is the widest the axis holds, so it shows
    // on any plot with room for one label.
    public int Stride(int place) =>
        place == 0 ? (int)BitOperations.RoundUpToPowerOf2((uint)Places.Count) : place & -place;

    public static ChartPlan Build(IReadOnlyList<string> places, IReadOnlyList<ChartSeriesValues> series,
        ChartStacking stacking, ChartValueAxis axis, CultureInfo culture)
    {
        var bars = series.Where(one => one.Bars).ToList();
        (double From, double To)?[][] piled = stacking == ChartStacking.None
            ? [.. bars.Select(one => one.Values.Select(value => value is { } to ? ((double, double)?)(0, to) : null)
                .ToArray())]
            : ChartStack.Pile([.. bars.Select(one => one.Values)], stacking == ChartStacking.Percent);

        // Bars are read from zero, so their axis has to reach it; lines alone may start wherever the values do.
        var reached = piled.SelectMany(one => one).OfType<(double From, double To)>()
            .SelectMany(bar => new[] { bar.From, bar.To })
            .Concat(series.Where(one => !one.Bars).SelectMany(one => one.Values).OfType<double>()).ToList();
        var ticks = TicksFor(reached, axis, stacking == ChartStacking.Percent && bars.Count > 0);

        var format = axis.Format ?? (stacking == ChartStacking.Percent ? "P0" : "N" + ticks.Decimals);
        var valueTicks = ticks.Values.Where(value => value >= ticks.Min && value <= ticks.Max)
            .Select(value => (1 - ticks.Share(value), value.ToString(format, culture), value == 0)).ToList();

        var planned = new List<ChartSeriesPlan>(series.Count);
        foreach (var one in series)
        {
            var text = one.Values.Select(value => value?.ToString(axis.Format ?? "#,0.###", culture)).ToList();
            if (one.Bars)
            {
                var slot = bars.IndexOf(one);
                planned.Add(new(one.Name, true, "", [], Bars(piled, slot, places.Count, stacking, ticks), text));
            }
            else
            {
                (double X, double Y)?[] points = [.. one.Values.Select((value, place) =>
                    value is { } at ? ((double, double)?)((place + 0.5) / places.Count, 1 - ticks.Share(at)) : null)];
                planned.Add(new(one.Name, false, ChartPath.Line(points, one.Curve), points, [], text));
            }
        }

        return new(places, ticks, planned, valueTicks);
    }

    private static ChartTicks TicksFor(List<double> reached, ChartValueAxis axis, bool shares)
    {
        var lowest = axis.Min ?? (reached.Count == 0 ? 0 : reached.Min());
        var highest = axis.Max ?? (shares ? 1 : reached.Count == 0 ? 1 : reached.Max());
        var covered = ChartTicks.Cover(lowest, highest);
        // An end the app set is kept as given, even off a round step; the ticks between stay on round values.
        return new(axis.Min ?? covered.Min, axis.Max ?? covered.Max, axis.Step ?? covered.Step);
    }

    private static ChartBar?[] Bars((double From, double To)?[][] piled, int slot, int places, ChartStacking stacking,
        ChartTicks ticks)
    {
        var stacked = stacking != ChartStacking.None;
        var width = BarsShare / places / (stacked ? 1 : piled.Length);
        var marks = new ChartBar?[places];
        for (var place = 0; place < places; place++)
        {
            if (piled[slot][place] is not { } bar)
            {
                continue;
            }

            var left = (place + (1 - BarsShare) / 2) / places + (stacked ? 0 : slot * width);
            var top = 1 - ticks.Share(Math.Max(bar.From, bar.To));
            var bottom = 1 - ticks.Share(Math.Min(bar.From, bar.To));
            var up = bar.To >= bar.From;
            // In a stack only the bar farthest from zero on its side has a free end.
            var outermost = !stacked || !piled.Skip(slot + 1).Any(later =>
                later[place] is { } other && other.To != other.From && other.To >= other.From == up);
            marks[place] = new(left, top, width, bottom - top,
                !outermost ? ChartBarEnd.None : up ? ChartBarEnd.Top : ChartBarEnd.Bottom);
        }

        return marks;
    }
}
