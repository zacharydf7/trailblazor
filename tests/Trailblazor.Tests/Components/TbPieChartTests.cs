using System.Diagnostics.Metrics;
using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbPieChartTests : BunitContext
{
    private static readonly Pass[] Passes = [new("Day", 420), new("Week", 260), new("Season", 120)];

    private readonly List<ChartPoint<Pass>> _clicked = [];

    public TbPieChartTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    private sealed record Pass(string Kind, double? Sold);

    [Fact]
    public void Render_ShouldDrawASliceForEachItemInItsOwnColor_WhenGivenItems()
    {
        var cut = RenderPie();

        var slices = cut.FindAll("svg.tb-chart__marks path");
        Assert.Equal([("tb-pie__slice tb-chart__series--1", "0"), ("tb-pie__slice tb-chart__series--2", "1"),
            ("tb-pie__slice tb-chart__series--3", "2")],
            slices.Select(slice => (slice.ClassName, slice.GetAttribute("data-place"))));
        // Day's 420 of 800 is more than half, so it goes around from the top in two arcs.
        Assert.StartsWith("M50 50 L50 0 A50 50 0 0 1 99.85 53.92 A50 50 0 0 1 ", slices[0].GetAttribute("d"));
        Assert.Equal("0 0 100 100", cut.Find("svg.tb-chart__marks").GetAttribute("viewBox"));
        Assert.Empty(cut.FindAll("[data-part=center]"));
    }

    [Fact]
    public void Render_ShouldNameTheFigureByItsTitleAndTheDrawingByItsKindAndSlices_WhenGivenATitle()
    {
        var cut = RenderPie();

        Assert.Equal("Passes sold", cut.Find("figure.tb-pie > figcaption").TextContent);
        var drawing = cut.Find("svg.tb-chart__marks");
        Assert.Equal(("img", "Pie chart: Day, Week, Season"),
            (drawing.GetAttribute("role"), drawing.GetAttribute("aria-label")));
        Assert.Equal("Passes sold", cut.Find("[data-part=table] table").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldListEachSliceWithItsShareInTheLegend_WhenDrawn()
    {
        var cut = RenderPie();

        var entries = cut.FindAll("[data-part=legend] > li");
        Assert.Equal(["Day 53%", "Week 33%", "Season 15%"],
            entries.Select(entry => string.Join(' ', entry.TextContent.Split((char[]?)null,
                StringSplitOptions.RemoveEmptyEntries))));
        // A key is drawn in its slice's color, and is hidden from screen readers.
        Assert.Equal([("tb-chart__key tb-chart__series--1", "true"), ("tb-chart__key tb-chart__series--2", "true"),
            ("tb-chart__key tb-chart__series--3", "true")], entries.Select(entry => entry.QuerySelector("svg")!)
            .Select(key => (key.ClassName, key.GetAttribute("aria-hidden"))));
    }

    [Fact]
    public void Render_ShouldHoldEveryValueAndShareInATableUnderTheChart_WhenDrawn()
    {
        var cut = RenderPie(parameters => parameters.Add(chart => chart.Format, "N0"),
            [new("Day", 1420), new("Week", 580), new("Lost", null), new("Refunded", 0)]);

        Assert.Equal("Data table", cut.Find("[data-part=table] > summary").TextContent);
        Assert.Equal(["", "Value", "Share"], cut.FindAll("thead tr > *").Select(cell => cell.TextContent));
        Assert.Equal(["Day|1,420|71%", "Week|580|29%", "Lost||", "Refunded|0|"], cut.FindAll("tbody tr")
            .Select(row => string.Join('|', row.Children.Select(cell => cell.TextContent))));
        var chart = cut.Tb().PieChart("Passes sold");
        Assert.Equal(["Day", "Week", "Lost", "Refunded"], chart.Slices);
        Assert.Equal(("580", "29%", ""), (chart.Value("Week"), chart.Share("Week"), chart.Share("Refunded")));
    }

    [Fact]
    public void Render_ShouldLeaveAnItemOutOfThePieAndTheLegend_WhenItsValueIsMissingOrNotAboveZero()
    {
        var cut = RenderPie(items: [new("Day", 30), new("Lost", null), new("Refunded", -4), new("Week", 10)]);

        // An item keeps the color of its place among all the items, and the script hears which items have a slice.
        Assert.Equal([("0", "tb-pie__slice tb-chart__series--1"), ("3", "tb-pie__slice tb-chart__series--4")],
            cut.FindAll("svg.tb-chart__marks path").Select(slice => (slice.GetAttribute("data-place")!,
                slice.ClassName!)));
        Assert.Equal(2, cut.FindAll("[data-part=legend] > li").Count);
        Assert.Equal("Pie chart: Day, Week", cut.Find("svg.tb-chart__marks").GetAttribute("aria-label"));
        var group = cut.Find("svg.tb-chart__marks g");
        Assert.Equal("30 (75%)\n\n-4\n10 (25%)", group.GetAttribute("data-values"));
        Assert.Equal(["-", "-"], group.GetAttribute("data-points")!.Split(' ')[1..3]);
    }

    [Fact]
    public void Render_ShouldWriteEachSlicesNameValueAndMiddleForItsScript_WhenDrawn()
    {
        var cut = RenderPie(items: [new("Day", 50), new("Week", 50)]);

        var plot = cut.Find("[data-part=plot]");
        Assert.Equal(("", "Day\nWeek", "8000 2000"),
            (plot.GetAttribute("data-round"), plot.GetAttribute("data-places"), plot.GetAttribute("data-at")));
        var group = cut.Find("svg.tb-chart__marks g");
        Assert.Equal(("", "0", "50 (50%)\n50 (50%)", "8000,5000 2000,5000"), (group.GetAttribute("data-series"),
            group.GetAttribute("data-number"), group.GetAttribute("data-values"), group.GetAttribute("data-points")));
        // The script fills a slice's name as a chart of several series does, with {1} for the series, which a pie
        // skips.
        var call = Assert.Single(JSInterop.Invocations["attachChart"]);
        Assert.Equal(("{0}: {2}, {3} of {4}", "", false),
            ((string)call.Arguments[2]!, (string)call.Arguments[3]!, (bool)call.Arguments[4]!));
        Assert.False(plot.HasAttribute("data-activates"));
    }

    [Fact]
    public void Render_ShouldLeaveAHoleAndFillItWithItsContent_WhenADonut()
    {
        var cut = RenderPie(parameters => parameters.Add(chart => chart.Donut, true)
            .AddChildContent("<b>100</b> passes"), [new("Day", 50), new("Week", 50)]);

        Assert.Equal("M50 0 A50 50 0 0 1 50 100 L50 80 A30 30 0 0 0 50 20 Z",
            cut.Find("svg.tb-chart__marks path").GetAttribute("d"));
        Assert.Equal("100 passes", cut.Find("[data-part=plot] > [data-part=center]").TextContent);
        Assert.Equal("Donut chart: Day, Week", cut.Find("svg.tb-chart__marks").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldDrawWithoutItsScript_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderPie(parameters => parameters.Add(chart => chart.OnSliceClick, _clicked.Add));

        Assert.Equal(3, cut.FindAll("svg.tb-chart__marks path").Count);
        Assert.Equal(3, cut.FindAll("[data-part=legend] > li").Count);
        Assert.Equal(3, cut.FindAll("[data-part=table] tbody tr").Count);
        // Nothing follows the pointer or the keys, so the plot doesn't say it can be pressed.
        Assert.Empty(JSInterop.Invocations);
        var plot = cut.Find("[data-part=plot]");
        Assert.False(plot.HasAttribute("data-activates") || plot.HasAttribute("data-commits"));
    }

    [Fact]
    public void Render_ShouldGiveASliceItsItemsTone_WhenGivenAnItemTone()
    {
        var cut = RenderPie(parameters => parameters.Add(chart => chart.ItemTone,
            pass => pass.Kind == "Week" ? Tone.Warning : null));

        const string toned = "tb-chart__series--2 tb-chart__tone--warning";
        Assert.Equal(["tb-pie__slice tb-chart__series--1", $"tb-pie__slice {toned}",
            "tb-pie__slice tb-chart__series--3"],
            cut.FindAll("svg.tb-chart__marks path").Select(slice => slice.ClassName));
        Assert.Equal($"tb-chart__key {toned}", cut.FindAll("[data-part=legend] svg")[1].ClassName);
        Assert.Equal([null, "warning", null],
            cut.FindAll("tbody tr > td:nth-child(2)").Select(cell => cell.GetAttribute("data-tone")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_ShouldSayThereIsNoData_WhenNoItemHasAValueAboveZero(bool items)
    {
        var cut = RenderPie(items: items ? [new("Day", 0), new("Week", null)] : []);

        var plot = cut.Find("[data-part=plot]");
        Assert.Equal(("No data", "tb-chart__plot tb-pie__plot tb-chart__plot--empty"),
            (plot.TextContent.Trim(), plot.ClassName));
        Assert.Empty(cut.FindAll("svg.tb-chart__marks, [data-part=legend]"));
        Assert.False(plot.HasAttribute("data-places"));
        // The items still have their rows in the table, and no items means no table.
        Assert.Equal(items ? 1 : 0, cut.FindAll("[data-part=table]").Count);
    }

    [Fact]
    public void Render_ShouldUseTheAppsWords_WhenLabelsAreGiven()
    {
        var labels = new PieChartLabels
        {
            Table = "Numbers", Summary = "{1} as a {0}", Pie = "pie", Slice = "{0} has {1}, slice {2} of {3}",
            ValueAndShare = "{0}, or {1}", Value = "Passes", Share = "Part",
        };

        var cut = RenderPie(parameters => parameters.Add(chart => chart.Labels, labels));

        Assert.Equal("Numbers", cut.Find("[data-part=table] > summary").TextContent);
        Assert.Equal("Day, Week, Season as a pie", cut.Find("svg.tb-chart__marks").GetAttribute("aria-label"));
        Assert.Equal(["", "Passes", "Part"], cut.FindAll("thead tr > *").Select(cell => cell.TextContent));
        Assert.StartsWith("420, or 53%\n", cut.Find("svg.tb-chart__marks g").GetAttribute("data-values"));
        Assert.Equal("{0} has {2}, slice {3} of {4}", Assert.Single(JSInterop.Invocations["attachChart"]).Arguments[2]);
        var empty = RenderPie(parameters => parameters.Add(chart => chart.Labels,
            new PieChartLabels { Empty = "No passes sold" }), []);
        Assert.Equal("No passes sold", empty.Find("[data-part=plot]").TextContent.Trim());
    }

    [Fact]
    public async Task Click_ShouldTellTheAppTheItemItsLabelAndItsValue_WhenASliceIsActivated()
    {
        var cut = RenderPie(parameters => parameters.Add(chart => chart.OnSliceClick, _clicked.Add));
        Assert.Equal("", cut.Find("[data-part=plot]").GetAttribute("data-activates"));

        await cut.Tb().PieChart().ClickSliceAsync("Week");

        Assert.Equal([new(Passes[1], "Week", 260)], _clicked);
        Assert.Equal("1", cut.Find("[data-part=plot]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task ClickSliceAsync_ShouldThrow_WhenThereIsNoSliceToClickOrNothingHearsIt()
    {
        var silent = RenderPie().Tb().PieChart();
        var cut = RenderPie(parameters => parameters.Add(chart => chart.OnSliceClick, _clicked.Add),
            [new("Day", 5), new("Refunded", 0)]);
        var chart = cut.Tb().PieChart();

        Assert.Equal("The chart has no OnSliceClick, so a click on a slice does nothing.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => silent.ClickSliceAsync("Day"))).Message);
        Assert.Equal("\"Refunded\" has no slice.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => chart.ClickSliceAsync("Refunded"))).Message);
        Assert.Equal("The chart has no item labeled \"Month\".",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => chart.ClickSliceAsync("Month"))).Message);
        Assert.Empty(_clicked);
    }

    [Fact]
    public void Render_ShouldThrow_WhenAValueIsNotANumber()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbPieChart<Pass>>(parameters => parameters
            .Add(chart => chart.Items, Passes).Add(chart => chart.Label, pass => pass.Kind)
            .Add(chart => chart.Value, pass => pass.Kind)));

        Assert.Equal("TbPieChart got a value of type String for \"Day\", which isn't a number.", thrown.Message);
    }

    [Fact]
    public void Render_ShouldSetItsDiameterAndPartClasses_WhenTheyAreGiven()
    {
        var classes = new ChartClasses
        {
            Title = "my-title", Legend = "my-legend", Plot = "my-plot", Table = "my-table",
        };

        var cut = RenderPie(parameters => parameters.Add(chart => chart.Diameter, "12rem")
            .Add(chart => chart.Classes, classes).Add(chart => chart.Style, "margin: 0"));

        Assert.Equal("--tb-pie-diameter: 12rem; margin: 0", cut.Find("figure").GetAttribute("style"));
        Assert.Equal(["tb-chart__title my-title", "tb-chart__plot tb-pie__plot my-plot",
            "tb-chart__legend tb-pie__legend my-legend", "tb-chart__table my-table"],
            new[] { "title", "plot", "legend", "table" }.Select(part => cut.Find($"[data-part={part}]").ClassName));
    }

    [Fact]
    public async Task Telemetry_ShouldCountAShowAndEachSliceActivated_WhenGivenATelemetryName()
    {
        var meters = Services.GetRequiredService<IMeterFactory>();
        using var shown = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.shown");
        using var used = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.used");
        var cut = RenderPie(parameters => parameters.Add(chart => chart.TelemetryName, "passes-by-kind")
            .Add(chart => chart.OnSliceClick, _clicked.Add));

        await cut.Tb().PieChart().ClickSliceAsync("Day");

        Assert.Single(shown.GetMeasurementSnapshot());
        var use = Assert.Single(used.GetMeasurementSnapshot());
        Assert.Equal(("activate", "pointer"), (use.Tags["trailblazor.action"], use.Tags["trailblazor.input"]));
    }

    private IRenderedComponent<TbPieChart<Pass>> RenderPie(
        Func<ComponentParameterCollectionBuilder<TbPieChart<Pass>>,
            ComponentParameterCollectionBuilder<TbPieChart<Pass>>>? more = null, Pass[]? items = null) =>
        Render<TbPieChart<Pass>>(parameters => (more ?? (same => same))(parameters
            .Add(chart => chart.Items, items ?? Passes)
            .Add(chart => chart.Label, pass => pass.Kind).Add(chart => chart.Value, pass => pass.Sold)
            .Add(chart => chart.Title, "Passes sold")));
}
