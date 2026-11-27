using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

// A chart turned on its side: its places run down it and its bars across.
public class TbChartHorizontalTests : BunitContext
{
    private static readonly Month[] Months = [new("Jan", 10, 2), new("Feb", 20, 0), new("Mar", 40, 6)];

    public TbChartHorizontalTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    private sealed record Month(string Name, int Completed, int Failed);

    [Fact]
    public void Render_ShouldRunItsBarsAcrossFromTheStart_WhenHorizontal()
    {
        var cut = RenderChart(Series<TbBarSeries<Month>>("Completed", month => month.Completed));

        // Each month has a third of the plot's height, and its bar reaches across by its value's share of 40.
        Assert.Equal([("0%", "5%", "25%", "23.333%"), ("0%", "38.333%", "50%", "23.333%"),
            ("0%", "71.667%", "100%", "23.333%")], cut.FindAll("g[data-series='Completed'] rect").Select(bar =>
            (bar.GetAttribute("x"), bar.GetAttribute("y"), bar.GetAttribute("width"), bar.GetAttribute("height"))));
        // The end that shows the value is the one toward the higher values.
        Assert.All(cut.FindAll("g[data-series='Completed'] rect"),
            bar => Assert.Equal("tb-chart__bar tb-chart__bar--end", bar.ClassName));
        Assert.Equal("tb-chart tb-chart--horizontal", cut.Find("figure").ClassName);
    }

    [Fact]
    public void Render_ShouldTellItsScriptWhereThePointsAreAndThatPlacesRunDown_WhenHorizontal()
    {
        var cut = RenderChart(Series<TbBarSeries<Month>>("Completed", month => month.Completed));

        // A point is the middle of its bar's value end: as far across as the value, and as far down as its place.
        Assert.Equal("2500,1667 5000,5000 10000,8333",
            cut.Find("g[data-series='Completed']").GetAttribute("data-points"));
        Assert.Equal("", cut.Find("[data-part=plot]").GetAttribute("data-horizontal"));
        Assert.False(RenderChart(false, Series<TbBarSeries<Month>>("Completed", month => month.Completed))
            .Find("[data-part=plot]").HasAttribute("data-horizontal"));
    }

    [Fact]
    public void Render_ShouldPutThePlacesDownTheSideAndTheValuesAlongTheBase_WhenHorizontal()
    {
        var cut = RenderChart(Series<TbBarSeries<Month>>("Completed", month => month.Completed));

        // A name is level with its bar's middle, and carries its stride for the stylesheet to thin the names by.
        Assert.Equal([("Jan", "top: 16.667%; --_tb-chart-stride: 4"), ("Feb", "top: 50%; --_tb-chart-stride: 1"),
            ("Mar", "top: 83.333%; --_tb-chart-stride: 2")],
            cut.FindAll(".tb-chart__y > span:not(.tb-chart__y-sizer)").Select(name =>
                (name.TextContent.Trim(), name.GetAttribute("style")!)));
        Assert.Equal("--_tb-chart-count: 3", cut.Find(".tb-chart__y").GetAttribute("style"));
        // Every name is there once more, unseen, to give the column the width of the widest.
        Assert.Equal(["Jan", "Feb", "Mar"], cut.FindAll(".tb-chart__y-sizer").Select(name => name.TextContent));
        // The value axis has four steps, and its first and last labels are on the plot's edges.
        Assert.Equal([("0", "start"), ("10", null), ("20", null), ("30", null), ("40", "end")],
            cut.FindAll(".tb-chart__x > span")
                .Select(mark => (mark.TextContent.Trim(), mark.GetAttribute("data-edge"))));
        Assert.Equal("--_tb-chart-count: 4; --_tb-chart-chars: 4", cut.Find(".tb-chart__x").GetAttribute("style"));
        Assert.Equal("inset-inline-start: 25%; --_tb-chart-stride: 1",
            cut.FindAll(".tb-chart__x > span")[1].GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldDrawItsGridLinesUpAndDown_WhenHorizontal()
    {
        var cut = RenderChart(Series<TbBarSeries<Month>>("Completed", month => month.Completed));

        Assert.Equal([("0%", "0%", "0%", "100%"), ("25%", "25%", "0%", "100%"), ("50%", "50%", "0%", "100%"),
            ("75%", "75%", "0%", "100%"), ("100%", "100%", "0%", "100%")], cut.FindAll("line.tb-chart__grid")
            .Select(line => (line.GetAttribute("x1"), line.GetAttribute("x2"), line.GetAttribute("y1"),
                line.GetAttribute("y2"))));
        Assert.Equal("tb-chart__grid tb-chart__grid--zero", cut.FindAll("line.tb-chart__grid")[0].ClassName);
    }

    [Fact]
    public void Render_ShouldRunABarBackFromZero_WhenItsValueIsBelowZero()
    {
        var cut = RenderChart(Series<TbBarSeries<Month>>("Change", month => month.Name == "Feb" ? -10 : 10));

        // Zero is halfway across. A bar below it runs back toward the start, and its start shows its value.
        var bars = cut.FindAll("g[data-series='Change'] rect");
        Assert.Equal([("50%", "50%", "tb-chart__bar tb-chart__bar--end"),
            ("0%", "50%", "tb-chart__bar tb-chart__bar--start")],
            bars.Take(2).Select(bar => (bar.GetAttribute("x"), bar.GetAttribute("width"), bar.ClassName)));
    }

    [Fact]
    public void Render_ShouldPartStackedBarsWithLinesUpAndDown_WhenHorizontalAndStacked()
    {
        var cut = RenderChart(true, parameters => parameters.Add(chart => chart.Stacking, ChartStacking.Stacked),
            Series<TbBarSeries<Month>>("Completed", month => month.Completed),
            Series<TbBarSeries<Month>>("Failed", month => month.Failed));

        // January's 10 completed end where its 2 failed start, a fifth of the way to 50, across its bar's height.
        var gap = cut.FindAll("line.tb-chart__gap")[0];
        Assert.Equal(("20%", "20%", "5%", "28.333%"),
            (gap.GetAttribute("x1"), gap.GetAttribute("x2"), gap.GetAttribute("y1"), gap.GetAttribute("y2")));
    }

    [Fact]
    public void Render_ShouldPlaceItsBarsDownTheChartByTheirX_WhenHorizontalOnAContinuousScale()
    {
        RenderFragment axis = builder =>
        {
            builder.OpenComponent<TbXAxis>(0);
            builder.AddComponentParameter(1, nameof(TbXAxis.Scale), ChartScale.Continuous);
            builder.CloseComponent();
        };

        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Completed).Add(chart => chart.Horizontal, true)
            .Add(chart => chart.Title, "Jobs").Add(chart => chart.ChildContent, builder =>
            {
                axis(builder);
                Series<TbBarSeries<Month>>("Failed", month => month.Failed)(builder);
            }));

        // The items are at 10, 20 and 40, with half the smallest gap as room at each end: 5 to 45 down the plot.
        Assert.Equal(["3.75%", "28.75%", "78.75%"],
            cut.FindAll("g[data-series='Failed'] rect").Select(bar => bar.GetAttribute("y")));
        Assert.Equal("1250 3750 8750", cut.Find("[data-part=plot]").GetAttribute("data-at"));
    }

    [Fact]
    public void Render_ShouldDrawWithNoValueLabels_WhenHorizontalAndItsValueAxisHoldsNoMark()
    {
        RenderFragment axis = builder =>
        {
            builder.OpenComponent<TbYAxis>(0);
            builder.AddComponentParameter(1, nameof(TbYAxis.Min), 12d);
            builder.AddComponentParameter(2, nameof(TbYAxis.Max), 18d);
            builder.AddComponentParameter(3, nameof(TbYAxis.Step), 10d);
            builder.CloseComponent();
        };

        var cut = RenderChart(axis, Series<TbBarSeries<Month>>("Completed", month => month.Completed));

        // No multiple of 10 lies between 12 and 18, so the axis along the base has nothing to label.
        Assert.Empty(cut.FindAll(".tb-chart__x > span"));
        Assert.Single(cut.FindAll("svg.tb-chart__marks"));
    }

    [Fact]
    public void Render_ShouldSwapWhereTheAxesTitlesShow_WhenHorizontal()
    {
        RenderFragment axes = builder =>
        {
            builder.OpenComponent<TbXAxis>(0);
            builder.AddComponentParameter(1, nameof(TbXAxis.Title), "Month");
            builder.CloseComponent();
            builder.OpenComponent<TbYAxis>(2);
            builder.AddComponentParameter(3, nameof(TbYAxis.Title), "Jobs");
            builder.CloseComponent();
        };

        var cut = RenderChart(axes, Series<TbBarSeries<Month>>("Completed", month => month.Completed));

        // The title of the axis up the side is above the plot, and the title of the one along the base under it.
        Assert.Equal(("tb-chart__y-title", "Month"),
            (cut.Find("[data-part=x-title]").ClassName, cut.Find("[data-part=x-title]").TextContent));
        Assert.Equal(("tb-chart__x-title", "Jobs"),
            (cut.Find("[data-part=y-title]").ClassName, cut.Find("[data-part=y-title]").TextContent));
    }

    [Fact]
    public void Render_ShouldSayWhichSeriesCannotBeTurned_WhenHorizontalAndASeriesIsNotBars()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => RenderChart(
            Series<TbBarSeries<Month>>("Completed", month => month.Completed),
            Series<TbLineSeries<Month>>("Failed", month => month.Failed)));

        Assert.Equal("A TbChart that is Horizontal draws bar series only, and \"Failed\" isn't one.", thrown.Message);
    }

    private IRenderedComponent<TbChart<Month>> RenderChart(params RenderFragment[] content) =>
        RenderChart(true, content);

    private IRenderedComponent<TbChart<Month>> RenderChart(bool horizontal, params RenderFragment[] content) =>
        RenderChart(horizontal, parameters => parameters, content);

    private IRenderedComponent<TbChart<Month>> RenderChart(bool horizontal,
        Func<ComponentParameterCollectionBuilder<TbChart<Month>>, ComponentParameterCollectionBuilder<TbChart<Month>>>
            more, params RenderFragment[] content) =>
        Render<TbChart<Month>>(parameters => more(parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .Add(chart => chart.Horizontal, horizontal).Add(chart => chart.ChildContent, builder =>
            {
                foreach (var one in content)
                {
                    one(builder);
                }
            })));

    private static RenderFragment Series<TSeries>(string name, Func<Month, object?> value)
        where TSeries : TbChartSeries<Month> => builder =>
    {
        builder.OpenComponent<TSeries>(10);
        builder.SetKey(name);
        builder.AddComponentParameter(11, nameof(TbChartSeries<Month>.Name), name);
        builder.AddComponentParameter(12, nameof(TbChartSeries<Month>.Value), value);
        builder.CloseComponent();
    };
}
