using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

// A chart whose TbXAxis has a continuous scale, which places each item by its X value.
public class TbChartScaleTests : BunitContext
{
    private static readonly Reading[] Readings =
        [new(new(2026, 7, 1), 10), new(new(2026, 7, 2), 20), new(new(2026, 7, 9), 40)];

    public TbChartScaleTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    private sealed record Reading(DateOnly Day, int Depth);

    [Fact]
    public void Render_ShouldPlaceEachItemByItsDateAndLabelTheMarks_WhenTheScaleIsContinuous()
    {
        var cut = RenderChart(ChartScale.Continuous, format: null, bars: false);

        // July 2 is an eighth of the way from July 1 to July 9.
        Assert.Equal(["0%", "12.5%", "100%"],
            cut.FindAll("circle.tb-chart__marker").Select(marker => marker.GetAttribute("cx")));
        var plot = cut.Find("[data-part=plot]");
        Assert.Equal(("0 1250 10000", "7/1/2026\n7/2/2026\n7/9/2026"),
            (plot.GetAttribute("data-at"), plot.GetAttribute("data-places")));
        // The axis marks each day, not each item, and says how many steps apart its ends are.
        var labels = cut.FindAll(".tb-chart__x > span");
        Assert.Equal(["Jul 1", "Jul 2", "Jul 3", "Jul 4", "Jul 5", "Jul 6", "Jul 7", "Jul 8", "Jul 9"],
            labels.Select(label => label.TextContent.Trim()));
        Assert.Equal("inset-inline-start: 50%; --_tb-chart-stride: 4", labels[4].GetAttribute("style"));
        // The marks on the plot's edges keep their labels inside it.
        Assert.Equal([("start", "inset-inline-start: 0%; --_tb-chart-stride: 16"), (null, "inset-inline-start: 12.5%; "
            + "--_tb-chart-stride: 1"), ("end", "inset-inline-end: 0; --_tb-chart-stride: 8")],
            new[] { labels[0], labels[1], labels[8] }
                .Select(label => (label.GetAttribute("data-edge"), label.GetAttribute("style"))));
        // With a label on each edge the two may be the only ones showing, each reaching inward, so labels are given
        // twice their five characters of room.
        Assert.Equal("--_tb-chart-count: 8; --_tb-chart-chars: 10", cut.Find(".tb-chart__x").GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldGiveLabelsHalfAsMuchRoomAgain_WhenOneLabelIsOnAnEdge()
    {
        // Nine days are marked every second day from July 1, so the first mark is on the plot's edge and the last,
        // July 9, is a day short of the other: one and a half times five characters, rounded up.
        var cut = Render<TbChart<Reading>>(parameters => parameters
            .Add(chart => chart.Items, [Readings[0], Readings[1], new(new(2026, 7, 10), 40)])
            .Add(chart => chart.X, reading => reading.Day).Add(chart => chart.Title, "Snow depth")
            .Add(chart => chart.ChildContent, Content(ChartScale.Continuous, null, null, bars: false)));

        Assert.Equal(["start", null, null, null, null],
            cut.FindAll(".tb-chart__x > span").Select(label => label.GetAttribute("data-edge")));
        Assert.Equal("--_tb-chart-count: 4.5; --_tb-chart-chars: 8", cut.Find(".tb-chart__x").GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldGiveLabelsOnlyTheirOwnRoom_WhenNoLabelIsOnAnEdge()
    {
        // With bars the plot has room at its ends, so no mark is on an edge.
        var cut = RenderChart(ChartScale.Continuous, format: null, bars: true);

        Assert.All(cut.FindAll(".tb-chart__x > span"), label => Assert.False(label.HasAttribute("data-edge")));
        Assert.Equal("--_tb-chart-count: 4.5; --_tb-chart-chars: 5", cut.Find(".tb-chart__x").GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldDrawAndListTheItemsInTheOrderGiven_WhenTheItemsAreNotInOrderOfX()
    {
        var cut = Render<TbChart<Reading>>(parameters => parameters
            .Add(chart => chart.Items, [Readings[2], Readings[0], Readings[1]])
            .Add(chart => chart.X, reading => reading.Day).Add(chart => chart.Title, "Snow depth")
            .Add(chart => chart.ChildContent, Content(ChartScale.Continuous, null, null, bars: false)));

        // The line goes from July 9 back to July 1, and the keys and the table follow the same order.
        var plot = cut.Find("[data-part=plot]");
        Assert.Equal(("10000 0 1250", "7/9/2026\n7/1/2026\n7/2/2026"),
            (plot.GetAttribute("data-at"), plot.GetAttribute("data-places")));
        Assert.Equal("M10000 0l-10000 10000l1250 -3333", cut.Find("path[data-series='Depth']").GetAttribute("d"));
    }

    [Fact]
    public void Render_ShouldWriteTheItemsAsFormatSaysAndTheMarksAsMarkFormatSays_WhenGivenBoth()
    {
        var cut = RenderChart(ChartScale.Continuous, "MMMM d", bars: false, markFormat: "M/d");

        Assert.Equal(["7/1", "7/2", "7/3"], cut.FindAll(".tb-chart__x > span").Take(3)
            .Select(label => label.TextContent.Trim()));
        Assert.Equal(["July 1", "July 2", "July 9"],
            cut.FindAll("[data-part=table] tbody th").Select(heading => heading.TextContent));
    }

    [Fact]
    public void Render_ShouldStillTellItsScriptWhereTheItemsAre_WhenFewRoundTimesLieBetweenThem()
    {
        var cut = Render<TbChart<Reading>>(parameters => parameters
            .Add(chart => chart.Items, [new(new(2001, 3, 1), 10), new(new(2001, 11, 1), 20)])
            .Add(chart => chart.X, reading => reading.Day).Add(chart => chart.Title, "Snow depth")
            .Add(chart => chart.ChildContent, Content(ChartScale.Continuous, null, null, bars: false)));

        // No new year lies between the two, and the chart is still a continuous one, marked by the quarter.
        Assert.Equal("0 10000", cut.Find("[data-part=plot]").GetAttribute("data-at"));
        Assert.Equal(["Apr 2001", "Jul 2001", "Oct 2001"],
            cut.FindAll(".tb-chart__x > span").Select(label => label.TextContent.Trim()));
    }

    [Fact]
    public void Render_ShouldGiveABarTheSmallestGapAndRoomAtTheEnds_WhenTheScaleIsContinuous()
    {
        var cut = RenderChart(ChartScale.Continuous, format: null, bars: true);

        // Nine days with half a day's room at each end: a bar has seven tenths of a day.
        var bars = cut.FindAll("g[data-series='Depth'] rect");
        Assert.Equal([("1.667%", "7.778%"), ("12.778%", "7.778%"), ("90.556%", "7.778%")],
            bars.Select(bar => (bar.GetAttribute("x"), bar.GetAttribute("width"))));
    }

    [Fact]
    public void Render_ShouldKeepOnePlaceForEachItem_WhenTheScaleIsEven()
    {
        var cut = RenderChart(ChartScale.Even, format: null, bars: false);

        Assert.Equal(["16.667%", "50%", "83.333%"],
            cut.FindAll("circle.tb-chart__marker").Select(marker => marker.GetAttribute("cx")));
        Assert.False(cut.Find("[data-part=plot]").HasAttribute("data-at"));
        Assert.Equal(["7/1/2026", "7/2/2026", "7/9/2026"],
            cut.FindAll(".tb-chart__x > span").Select(label => label.TextContent.Trim()));
    }

    [Fact]
    public void Render_ShouldSayWhatAnXMustBe_WhenTheScaleIsContinuousAndAnXIsText()
    {
        RenderFragment axis = builder => AddAxis(builder, ChartScale.Continuous, null);

        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbChart<string>>(parameters => parameters
            .Add(chart => chart.Items, ["Jul"]).Add(chart => chart.X, name => name).Add(chart => chart.Title, "Depth")
            .Add(chart => chart.ChildContent, axis)));

        Assert.Equal("A chart with a continuous scale needs every item's X to be a number or a date, and got Jul "
            + "(String).", thrown.Message);
    }

    private IRenderedComponent<TbChart<Reading>> RenderChart(ChartScale scale, string? format, bool bars,
        string? markFormat = null) =>
        Render<TbChart<Reading>>(parameters => parameters.Add(chart => chart.Items, Readings)
            .Add(chart => chart.X, reading => reading.Day).Add(chart => chart.Title, "Snow depth")
            .Add(chart => chart.ChildContent, Content(scale, format, markFormat, bars)));

    // The axis and one series named Depth, of bars or a line.
    private static RenderFragment Content(ChartScale scale, string? format, string? markFormat, bool bars) =>
        builder =>
        {
            AddAxis(builder, scale, format, markFormat);
            if (bars)
            {
                builder.OpenComponent<TbBarSeries<Reading>>(10);
            }
            else
            {
                builder.OpenComponent<TbLineSeries<Reading>>(10);
            }

            builder.AddComponentParameter(11, nameof(TbChartSeries<Reading>.Name), "Depth");
            builder.AddComponentParameter(12, nameof(TbChartSeries<Reading>.Value),
                (Func<Reading, object?>)(reading => reading.Depth));
            builder.CloseComponent();
        };

    private static void AddAxis(RenderTreeBuilder builder, ChartScale scale, string? format, string? markFormat = null)
    {
        builder.OpenComponent<TbXAxis>(0);
        builder.AddComponentParameter(1, nameof(TbXAxis.Scale), scale);
        builder.AddComponentParameter(2, nameof(TbXAxis.Format), format);
        builder.AddComponentParameter(3, nameof(TbXAxis.MarkFormat), markFormat);
        builder.CloseComponent();
    }
}
