using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

// Values written on a chart's marks.
public class TbChartValueTests : BunitContext
{
    private static readonly Month[] Months = [new("Jan", 10, 30), new("Feb", -20, 10), new("Mar", 40, null)];

    public TbChartValueTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    private sealed record Month(string Name, int Change, int? Booked);

    [Fact]
    public void Bars_ShouldHaveTheirValuesPastTheirEnds_WhenTheSeriesShowsValues()
    {
        var cut = RenderChart(Bars("Change", month => month.Change, show: true));

        // The axis runs from -20 to 40. Each label is centered on its bar, at its value end: over a bar that grows
        // up, and under one that hangs down.
        var bars = cut.FindAll("g[data-series='Change'] rect");
        var labels = cut.FindAll(".tb-chart__values > span");
        Assert.Equal(["10", "-20", "40"], labels.Select(label => label.TextContent));
        Assert.Equal(["above", "below", "above"], labels.Select(label => label.GetAttribute("data-side")));
        for (var place = 0; place < 3; place++)
        {
            var style = Style(labels[place]);
            var (x, width) = (Share(bars[place], "x"), Share(bars[place], "width"));
            var end = place == 1 ? Share(bars[place], "y") + Share(bars[place], "height") : Share(bars[place], "y");
            Assert.Equal(x + width / 2, Share(style["inset-inline-start"]), 3);
            Assert.Equal(end, Share(style["inset-block-start"]), 3);
            // A label has its bar's width for room, and the series' longest label, "-20", says how much it needs.
            Assert.Equal(width, double.Parse(style["--_tb-chart-wide"], CultureInfo.InvariantCulture), 3);
            Assert.Equal(("1", "3"), (style["--_tb-chart-high"], style["--_tb-chart-chars"]));
        }

        // The values are for the eye: the data table gives them to screen readers.
        Assert.Equal("true", cut.Find("[data-part=values]").GetAttribute("aria-hidden"));
        // The chart keeps room past both of the plot's ends, since a bar hangs down.
        Assert.Equal("tb-chart__body tb-chart__body--values tb-chart__body--values-low",
            cut.Find(".tb-chart__body").ClassName);
        Assert.Equal("--_tb-chart-value-chars: 3", cut.Find(".tb-chart__body").GetAttribute("style"));
    }

    [Fact]
    public void Bars_ShouldHaveTheirValuesInTheirMiddles_WhenStacked()
    {
        var cut = RenderChart(parameters => parameters.Add(chart => chart.Stacking, ChartStacking.Stacked),
            Bars("Change", month => Math.Abs(month.Change), show: true), Bars("Booked",
                month => month.Booked * (month.Name == "Jan" ? 100 : 1), show: true));

        // Each label is in the middle of its own bar, which limits it both ways, by its own text's length, not by
        // the longest label of its series, "3,000".
        var bar = cut.FindAll("g[data-series='Booked'] rect")[1];
        var label = cut.FindAll(".tb-chart__values > span")[4];
        var style = Style(label);
        Assert.Equal(("10", "middle", "2"), (label.TextContent, label.GetAttribute("data-side"),
            style["--_tb-chart-chars"]));
        // Labels in a stack stay inside the plot, so the chart keeps no room past it.
        Assert.Equal("tb-chart__body", cut.Find(".tb-chart__body").ClassName);
        Assert.Equal(Share(bar, "y") + Share(bar, "height") / 2, Share(style["inset-block-start"]), 3);
        Assert.Equal(Share(bar, "height"), double.Parse(style["--_tb-chart-high"], CultureInfo.InvariantCulture), 3);
    }

    [Fact]
    public void Bars_ShouldBeLimitedByTheirLengthAndThickness_WhenStackedInAHorizontalChart()
    {
        var cut = RenderChart(parameters => parameters.Add(chart => chart.Stacking, ChartStacking.Stacked)
            .Add(chart => chart.Horizontal, true), Bars("Change", month => Math.Abs(month.Change), show: true));

        // Across the plot the label has the bar's length for its text, and the bar's thickness for its height.
        var (bar, style) = (cut.Find("g[data-series='Change'] rect"), Style(cut.Find(".tb-chart__values > span")));
        Assert.Equal(Share(bar, "width"), double.Parse(style["--_tb-chart-wide"], CultureInfo.InvariantCulture), 3);
        Assert.Equal(Share(bar, "height"), double.Parse(style["--_tb-chart-high"], CultureInfo.InvariantCulture), 3);
    }

    [Fact]
    public void Bars_ShouldHaveTheirValuesBesideTheirEnds_WhenTheChartIsHorizontal()
    {
        var cut = RenderChart(parameters => parameters.Add(chart => chart.Horizontal, true),
            Bars("Change", month => month.Change, show: true));

        // In a horizontal chart a label is past the bar's end along the plot, and has to fit the bar's thickness.
        var bars = cut.FindAll("g[data-series='Change'] rect");
        var labels = cut.FindAll(".tb-chart__values > span");
        Assert.Equal(["end", "start", "end"], labels.Select(label => label.GetAttribute("data-side")));
        var (style, bar) = (Style(labels[0]), bars[0]);
        Assert.Equal(Share(bar, "x") + Share(bar, "width"), Share(style["inset-inline-start"]), 3);
        Assert.Equal(Share(bar, "y") + Share(bar, "height") / 2, Share(style["inset-block-start"]), 3);
        Assert.Equal("1", style["--_tb-chart-wide"]);
        Assert.Equal(Share(bar, "height"), double.Parse(style["--_tb-chart-high"], CultureInfo.InvariantCulture), 3);
    }

    [Fact]
    public void Line_ShouldHaveItsValuesOverItsPointsWithAPlaceForRoom_WhenTheSeriesShowsValues()
    {
        var cut = RenderChart(Line("Booked", month => month.Booked, show: true));

        // March has no value, so it has no label. The room for a label is a place's width, a third of the plot.
        var labels = cut.FindAll(".tb-chart__values > span");
        Assert.Equal(["30", "10"], labels.Select(label => label.TextContent));
        Assert.All(labels, label => Assert.Equal("point", label.GetAttribute("data-side")));
        var style = Style(labels[0]);
        Assert.Equal(1 / 6d, Share(style["inset-inline-start"]), 3);
        Assert.Equal(0, Share(style["inset-block-start"]), 3);
        Assert.Equal(1 / 3d, double.Parse(style["--_tb-chart-wide"], CultureInfo.InvariantCulture), 3);
    }

    [Fact]
    public void StackedAreas_ShouldHaveTheirValuesOverTheTopsOfTheirParts_WhenTheSeriesShowValues()
    {
        var cut = RenderChart(parameters => parameters.Add(chart => chart.Stacking, ChartStacking.Stacked),
            Area("Change", month => Math.Abs(month.Change), show: true), Area("Booked", month => month.Booked,
                show: true));

        // January's 30 booked sit on its 10 changed, so its label is at the stack's top, 40, which is the top of
        // the axis, and February's 10 booked on its 20 changed is at 30.
        var labels = cut.FindAll(".tb-chart__values > span");
        Assert.Equal(["10", "20", "40", "30", "10"], labels.Select(label => label.TextContent));
        Assert.Equal(0, Share(Style(labels[3])["inset-block-start"]), 3);
        Assert.Equal(1 - 30 / 40d, Share(Style(labels[4])["inset-block-start"]), 3);
        Assert.All(labels, label => Assert.Equal("point", label.GetAttribute("data-side")));
    }

    [Fact]
    public void Line_ShouldHaveItsFirstAndLastLabelsStartAndEndAtThePlotsEdges_WhenTheScaleIsContinuous()
    {
        var cut = Render<TbChart<Month>>(parameters => parameters
            .Add(chart => chart.Items, [new("1", 4, 30), new("2", 8, 10), new("5", 6, 20)])
            .Add(chart => chart.X, month => int.Parse(month.Name, CultureInfo.InvariantCulture))
            .Add(chart => chart.Title, "Jobs by month").Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbXAxis>(0);
                builder.AddComponentParameter(1, nameof(TbXAxis.Scale), ChartScale.Continuous);
                builder.CloseComponent();
                Line("Booked", month => month.Booked, show: true)(builder);
            }));

        var labels = cut.FindAll(".tb-chart__values > span");
        Assert.Equal(["start", null, "end"], labels.Select(label => label.GetAttribute("data-edge")));
        // Places 1, 2 and 5 lie at 0, 1/4 and 1 of the plot. A label on an edge reaches toward its neighbor as far
        // as one and a half centered ones, so the series' labels have two thirds of the first gap, 1/4.
        Assert.All(labels, label => Assert.Equal(1 / 6d,
            double.Parse(Style(label)["--_tb-chart-wide"], CultureInfo.InvariantCulture), 4));
    }

    [Fact]
    public void Values_ShouldBeWrittenAsTheDataTableWritesThem_WhenTheAxisHasAFormat()
    {
        var cut = RenderChart(Axis("0 'cm'"), Bars("Change", month => month.Change, show: true));

        Assert.Equal(["10 cm", "-20 cm", "40 cm"], cut.FindAll(".tb-chart__values > span").Select(label =>
            label.TextContent));
        Assert.Equal("6", Style(cut.Find(".tb-chart__values > span"))["--_tb-chart-chars"]);
    }

    [Fact]
    public void Values_ShouldBeLeftOut_WhenTheSeriesDoesNotShowThemOrIsHidden()
    {
        var cut = RenderChart(Bars("Change", month => month.Change), Bars("Booked", month => month.Booked,
            show: true, hidden: true));

        Assert.Empty(cut.FindAll(".tb-chart__values"));
        Assert.Equal("tb-chart__body", cut.Find(".tb-chart__body").ClassName);
    }

    [Fact]
    public void Values_ShouldBeDrawnAgain_WhenASeriesStartsShowingThem()
    {
        // The same value function both times, so only ShowValues changes.
        Func<Month, object?> change = month => month.Change;
        var cut = RenderChart(Bars("Change", change));

        cut.Render(parameters => parameters.Add(chart => chart.ChildContent, Bars("Change", change, show: true)));

        Assert.Equal(3, cut.FindAll(".tb-chart__values > span").Count);
    }

    private IRenderedComponent<TbChart<Month>> RenderChart(params RenderFragment[] content) =>
        RenderChart(parameters => parameters, content);

    private IRenderedComponent<TbChart<Month>> RenderChart(
        Func<ComponentParameterCollectionBuilder<TbChart<Month>>, ComponentParameterCollectionBuilder<TbChart<Month>>>
            more, params RenderFragment[] content) =>
        Render<TbChart<Month>>(parameters => more(parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .Add(chart => chart.ChildContent, builder =>
            {
                foreach (var one in content)
                {
                    one(builder);
                }
            })));

    private static RenderFragment Bars(string name, Func<Month, object?> value, bool show = false,
        bool hidden = false) => Series<TbBarSeries<Month>>(name, value, show, hidden);

    private static RenderFragment Line(string name, Func<Month, object?> value, bool show = false) =>
        Series<TbLineSeries<Month>>(name, value, show, false);

    private static RenderFragment Area(string name, Func<Month, object?> value, bool show = false) =>
        Series<TbAreaSeries<Month>>(name, value, show, false);

    private static RenderFragment Series<TSeries>(string name, Func<Month, object?> value, bool show, bool hidden)
        where TSeries : TbChartSeries<Month> => builder =>
    {
        builder.OpenComponent<TSeries>(10);
        builder.SetKey(name);
        builder.AddComponentParameter(11, nameof(TbChartSeries<Month>.Name), name);
        builder.AddComponentParameter(12, nameof(TbChartSeries<Month>.Value), value);
        builder.AddComponentParameter(13, nameof(TbBarSeries<Month>.ShowValues), show);
        builder.AddComponentParameter(14, nameof(TbChartSeries<Month>.Hidden), hidden);
        builder.CloseComponent();
    };

    private static RenderFragment Axis(string format) => (RenderTreeBuilder builder) =>
    {
        builder.OpenComponent<TbYAxis>(20);
        builder.AddComponentParameter(21, nameof(TbYAxis.Format), format);
        builder.CloseComponent();
    };

    // A label's style, as its declarations.
    private static Dictionary<string, string> Style(IElement label) => label.GetAttribute("style")!
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(declaration => declaration.Split(':', 2, StringSplitOptions.TrimEntries))
        .ToDictionary(pair => pair[0], pair => pair[1]);

    // A length written as a percentage, as a share.
    private static double Share(string percent) =>
        double.Parse(percent.TrimEnd('%'), CultureInfo.InvariantCulture) / 100;

    private static double Share(IElement element, string attribute) => Share(element.GetAttribute(attribute)!);
}
