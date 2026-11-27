using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// Drawing a very long series through a few of its points, which keep its shape.
public class TbChartThinningTests : BunitContext
{
    public TbChartThinningTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public void Keep_ShouldKeepEveryPlace_WhenThereAreNoMorePointsThanMost()
    {
        var points = Line(100, _ => 1);

        Assert.Equal(Enumerable.Range(0, 100), ChartThinning.Keep(points, 100));
        Assert.Equal(Enumerable.Range(0, 100), ChartThinning.Keep(points, 0));
    }

    [Fact]
    public void Keep_ShouldKeepTheEndsAPeakAndADip_WhenThinningAFlatLine()
    {
        var points = Line(1000, place => place switch { 400 => 0.1, 700 => 0.9, _ => 0.5 });

        var kept = ChartThinning.Keep(points, 50);

        Assert.Equal(50, kept.Count);
        Assert.Equal(kept.Order(), kept);
        Assert.Subset(kept.ToHashSet(), new HashSet<int> { 0, 400, 700, 999 });
    }

    [Fact]
    public void Keep_ShouldKeepEachRunsEndsAndTheStartOfEachGap_WhenTheLineBreaks()
    {
        // Two runs of 500 points with a gap of 100 between them.
        var points = Line(1100, place => 0.5);
        for (var place = 500; place < 600; place++)
        {
            points[place] = null;
        }

        var kept = ChartThinning.Keep(points, 100);

        Assert.Subset(kept.ToHashSet(), new HashSet<int> { 0, 499, 500, 600, 1099 });
        Assert.DoesNotContain(501, kept);
        Assert.InRange(kept.Count, 99, 103);
    }

    [Fact]
    public void Line_ShouldBeDrawnThroughAThousandPointsAndKeepEveryItem_WhenItHasMore()
    {
        var cut = RenderChart(5000);

        Assert.Equal(999, Segments(cut.Find("path.tb-chart__line")));
        Assert.Equal(5000, cut.Find("path.tb-chart__line").GetAttribute("data-points")!.Split(' ').Length);
        Assert.Equal(5000, cut.Tb().Chart("Readings").Places.Count);
    }

    [Fact]
    public void Line_ShouldBeDrawnThroughEveryPoint_WhenItShowsMarkers()
    {
        var cut = RenderChart(1500, markers: true);

        // Each marker sits on the line.
        Assert.Equal(1499, Segments(cut.Find("path.tb-chart__line")));
    }

    [Fact]
    public void Areas_ShouldEachBeThinnedOnTheirOwn_WhenNotStacked()
    {
        var cut = RenderChart(3000, areas: true, stacking: ChartStacking.None);

        Assert.All(cut.FindAll("path.tb-chart__line"), line => Assert.InRange(Segments(line), 995, 1005));
    }

    [Fact]
    public void Line_ShouldStillRunOnToThePlotsEdges_WhenALongSeriesIsZoomed()
    {
        var cut = RenderChart(5000, view: new ChartView(100.5, 4000.5));

        var places = Places(cut.Find("path.tb-chart__line").GetAttribute("d")!);
        Assert.Equal((0, ChartPath.Grid), (places[0], places[^1]));
        Assert.InRange(places.Count, 995, 1005);
    }

    [Fact]
    public void Chart_ShouldThrow_WhenMaxDrawnPointsIsBelowZero()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => RenderChart(10, most: -1));

        Assert.Equal("TbChart needs a MaxDrawnPoints of 0 or more.", thrown.Message);
    }

    [Fact]
    public void Line_ShouldBeDrawnThroughEveryPoint_WhenMaxDrawnPointsIsZero()
    {
        var cut = RenderChart(5000, most: 0);

        Assert.Equal(4999, Segments(cut.Find("path.tb-chart__line")));
    }

    [Fact]
    public void StackedAreas_ShouldBeThinnedAtTheSamePlaces_WhenLong()
    {
        var cut = RenderChart(3000, areas: true, stacking: ChartStacking.Stacked);

        // Each area's line runs through the same places, so the upper area sits on the lower one.
        var lines = cut.FindAll("path.tb-chart__line").Select(line => Places(line.GetAttribute("d")!)).ToList();
        Assert.Equal(2, lines.Count);
        Assert.Equal(lines[0], lines[1]);
        Assert.InRange(lines[0].Count, 1000, 2000);
    }

    private static (double X, double Y)?[] Line(int count, Func<int, double> y) =>
        [.. Enumerable.Range(0, count).Select(place => ((double, double)?)((double)place / count, y(place)))];

    private static int Segments(AngleSharp.Dom.IElement path) => Regex.Count(path.GetAttribute("d")!, "l");

    // Where along the plot each point of a straight path is.
    private static List<int> Places(string d)
    {
        var at = int.Parse(Regex.Match(d, @"^M(-?\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
        List<int> places = [at];
        foreach (Match step in Regex.Matches(d, @"l(-?\d+) "))
        {
            at += int.Parse(step.Groups[1].Value, CultureInfo.InvariantCulture);
            places.Add(at);
        }

        return places;
    }

    // A chart with the default MaxDrawnPoints unless `most` is given.
    private IRenderedComponent<TbChart<int>> RenderChart(int count, int? most = null, bool areas = false,
        ChartStacking stacking = ChartStacking.Stacked, bool markers = false, ChartView? view = null) =>
        Render<TbChart<int>>(parameters =>
        {
            parameters.Add(chart => chart.Items, Enumerable.Range(0, count)).Add(chart => chart.X, number => number)
                .Add(chart => chart.Title, "Readings").Add(chart => chart.Stacking, areas ? stacking : default)
                .Add(chart => chart.ChildContent, builder =>
                {
                    builder.OpenComponent<TbXAxis>(0);
                    builder.AddComponentParameter(1, nameof(TbXAxis.Scale), ChartScale.Continuous);
                    builder.AddComponentParameter(2, nameof(TbXAxis.Zoomable), view is not null);
                    builder.AddComponentParameter(3, nameof(TbXAxis.View), view);
                    builder.CloseComponent();
                    // Two series that wander differently, so each would keep other places of its own.
                    Series(builder, 4, areas, markers, "Low", number => Math.Sin(number / 7.0) + 2);
                    Series(builder, 10, areas, markers, "High", number => Math.Cos(number / 3.0) + 2);
                });
            if (most is { } given)
            {
                parameters.Add(chart => chart.MaxDrawnPoints, given);
            }
        });

    private static void Series(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, int sequence,
        bool area, bool markers, string name, Func<int, double> value)
    {
        if (!area && name == "High")
        {
            return;
        }

        if (area)
        {
            builder.OpenComponent<TbAreaSeries<int>>(sequence);
        }
        else
        {
            builder.OpenComponent<TbLineSeries<int>>(sequence);
        }

        builder.AddComponentParameter(sequence + 1, nameof(TbLineSeries<int>.Name), name);
        builder.AddComponentParameter(sequence + 2, nameof(TbLineSeries<int>.Value),
            (Func<int, object?>)(number => value(number)));
        if (markers)
        {
            builder.AddComponentParameter(sequence + 3, nameof(TbLineSeries<int>.Markers), true);
        }

        builder.CloseComponent();
    }
}
