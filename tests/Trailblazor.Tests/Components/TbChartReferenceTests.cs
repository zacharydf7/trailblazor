using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// Reference lines and bands: marks at values of a chart's value axis, such as a target or a normal range.
public class TbChartReferenceTests : BunitContext
{
    private static readonly Month[] Months = [new("Jan", 10), new("Feb", 20), new("Mar", 40)];

    public TbChartReferenceTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    private sealed record Month(string Name, int Completed);

    [Fact]
    public void Line_ShouldCrossThePlotAtItsValueWithItsNameAndValueAtItsEnd_WhenInsideTheAxis()
    {
        var cut = RenderChart(Line(30, "Target", Tone.Danger));

        // The axis runs from 0 to 40, so 30 is a quarter of the way down the plot.
        var line = cut.Find("line.tb-chart__reference");
        Assert.Equal(("tb-chart__reference tb-chart__tone--danger", "0%", "100%", "25%", "25%"), (line.ClassName,
            line.GetAttribute("x1"), line.GetAttribute("x2"), line.GetAttribute("y1"), line.GetAttribute("y2")));
        var name = cut.Find("[data-part=plot] > [data-part=reference]");
        Assert.Equal(("Target: 30", "tb-chart__reference-name", "inset-block-start: 25%"), (name.TextContent,
            name.ClassName, name.GetAttribute("style")));
        // Screen readers read that text after the drawing: nothing hides it from them.
        Assert.Null(name.Closest("[aria-hidden]"));
        // The list under the data table has it too, with its tone for tests to read.
        var listed = cut.Find("[data-part=table] [data-part=references] li");
        Assert.Equal(("Target: 30", "danger"), (listed.TextContent, listed.GetAttribute("data-tone")));
    }

    [Theory]
    [InlineData(55, "0", "60", "8.333%", true)]
    [InlineData(-5, "-10", "40", "90%", false)]
    public void Line_ShouldHaveTheAxisReachIt_WhenItIsBeyondEveryValue(double value, string lowest, string highest,
        string down, bool textUnder)
    {
        var cut = RenderChart(Line(value, "Limit"));

        var marks = cut.FindAll(".tb-chart__y > span:not(.tb-chart__y-sizer)").Select(mark => mark.TextContent.Trim())
            .ToList();
        Assert.Equal((lowest, highest), (marks[0], marks[^1]));
        Assert.Equal(down, cut.Find("line.tb-chart__reference").GetAttribute("y1"));
        // Without a tone it has no tone's class.
        Assert.Equal("tb-chart__reference", cut.Find("line.tb-chart__reference").ClassName);
        // A line near the top of the axis has its text under it, where there is room.
        Assert.Equal(textUnder,
            cut.Find("[data-part=reference]").ClassList.Contains("tb-chart__reference-name--under"));
    }

    [Fact]
    public void Band_ShouldTintThePlotBetweenItsValuesWithItsTextInItsCorner_WhenInsideTheAxis()
    {
        var cut = RenderChart(Band(10, 25, "Normal", Tone.Success));

        // 25 of 40 is 37.5% down from the top, and the band is 15 of 40 high, also 37.5%.
        var band = cut.Find("rect.tb-chart__band");
        Assert.Equal(("tb-chart__band tb-chart__tone--success", "0%", "37.5%", "100%", "37.5%"), (band.ClassName,
            band.GetAttribute("x"), band.GetAttribute("y"), band.GetAttribute("width"),
            band.GetAttribute("height")));
        var name = cut.Find("[data-part=reference]");
        Assert.Equal(("Normal: 10 to 25",
            "tb-chart__reference-name tb-chart__reference-name--band tb-chart__reference-name--under",
            "inset-block-start: 37.5%"), (name.TextContent, name.ClassName, name.GetAttribute("style")));
    }

    [Fact]
    public void References_ShouldPutBandsUnderTheMarksAndLinesOverThem_WhenDrawn()
    {
        var cut = RenderChart(Line(30, "Target"), Band(10, 25, "Normal"));

        List<string> drawn = [.. cut.Find("svg.tb-chart__marks").Children
            .Where(child => child.ClassList.Contains("tb-chart__band") || child.HasAttribute("data-series")
                || child.ClassList.Contains("tb-chart__reference"))
            .Select(child => child.LocalName)];
        Assert.Equal(["rect", "g", "line"], drawn);
        // They are listed in the order written.
        Assert.Equal(["Target: 30", "Normal: 10 to 25"], cut.Tb().Chart("Jobs by month").References);
    }

    [Fact]
    public void References_ShouldRunUpThePlotAtTheirValues_WhenTheChartIsHorizontal()
    {
        var cut = RenderChart(parameters => parameters.Add(chart => chart.Horizontal, true), Line(30, "Target"),
            Band(10, 25, "Normal"), Line(10, "Floor"), Band(25, 35, "High"));

        // Values grow across a horizontal chart, so 30 of 40 is three quarters of the way across.
        var line = cut.Find("line.tb-chart__reference");
        Assert.Equal(("75%", "75%", "0%", "100%"), (line.GetAttribute("x1"), line.GetAttribute("x2"),
            line.GetAttribute("y1"), line.GetAttribute("y2")));
        var band = cut.Find("rect.tb-chart__band");
        Assert.Equal(("25%", "0%", "37.5%", "100%"), (band.GetAttribute("x"), band.GetAttribute("y"),
            band.GetAttribute("width"), band.GetAttribute("height")));
        // A text starts at its line, or at its band's lower edge. Past the middle of the plot it ends at its line, or
        // at its band's higher edge: the line at 30 and the band from 25 to 35 do. Each is no wider than the plot is
        // on its side, so it stays inside the plot.
        Assert.Equal(["inset-inline-end: 25%; max-inline-size: 75%", "inset-inline-start: 25%; max-inline-size: 75%",
            "inset-inline-start: 25%; max-inline-size: 75%", "inset-inline-end: 12.5%; max-inline-size: 87.5%"],
            cut.FindAll("[data-part=reference]").Select(name => name.GetAttribute("style")));
    }

    [Fact]
    public void References_ShouldBeCutOffOrLeftOutOfThePlotAndStayListed_WhenTheAppSetsTheAxisEnd()
    {
        var cut = RenderChart(YAxis(40), Line(55, "Limit"), Band(30, 55, "High"), Band(50, 60, "Beyond"));

        // The axis stops at 40 as asked. The line and the last band are beyond it, and the first band shows from 30
        // to the top.
        Assert.Empty(cut.FindAll("line.tb-chart__reference"));
        var band = Assert.Single(cut.FindAll("rect.tb-chart__band"));
        Assert.Equal(("0%", "25%"), (band.GetAttribute("y"), band.GetAttribute("height")));
        Assert.Equal(["High: 30 to 55"], cut.FindAll("[data-part=reference]").Select(name => name.TextContent));
        Assert.Equal(["Limit: 55", "High: 30 to 55", "Beyond: 50 to 60"],
            cut.Tb().Chart("Jobs by month").References);
    }

    [Fact]
    public void References_ShouldWriteTheirValuesAsTheAxisDoesOrAsLabelsSay_WhenGivenAFormatOrLabels()
    {
        var cut = RenderChart(parameters => parameters.Add(chart => chart.Labels,
            new() { ReferenceLine = "{0} at {1}", ReferenceBand = "{0}, from {1} up to {2}" }),
            YAxis(null, "0 'cm'"), Line(30, "Target"), Band(10, 25, "Normal"));

        Assert.Equal(["Target at 30 cm", "Normal, from 10 cm up to 25 cm"],
            cut.Tb().Chart("Jobs by month").References);
    }

    [Fact]
    public void References_ShouldBeSharesWrittenAsTheAxisWritesThem_WhenTheChartStacksToPercent()
    {
        var cut = RenderChart(parameters => parameters.Add(chart => chart.Stacking, ChartStacking.Percent),
            Line(0.8, "Goal"), Line(80, "Not a share"));

        // The axis runs from 0% to 100% whatever a reference says, so 0.8 is a fifth of the way down the plot and a
        // value above 1 is beyond the plot.
        Assert.Equal("20%", Assert.Single(cut.FindAll("line.tb-chart__reference")).GetAttribute("y1"));
        Assert.Equal(["Goal: 80%", $"Not a share: {80.ToString("P0", CultureInfo.CurrentCulture)}"],
            cut.Tb().Chart("Jobs by month").References);
    }

    [Fact]
    public void References_ShouldSetTheAxisAndBeDrawn_WhenNoItemHasAValue()
    {
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbBarSeries<Month>>(0);
                builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), "Completed");
                builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value), (Func<Month, object?>)(_ => null));
                builder.CloseComponent();
                Line(30, "Target")(builder);
                Band(10, 20, "Normal")(builder);
            }));

        // The axis runs between the references, 10 and 30, and both are drawn on it.
        Assert.Equal("0%", cut.Find("line.tb-chart__reference").GetAttribute("y1"));
        var band = cut.Find("rect.tb-chart__band");
        Assert.Equal(("50%", "50%"), (band.GetAttribute("y"), band.GetAttribute("height")));
    }

    [Fact]
    public void References_ShouldStayListedAndLeaveThePlot_WhenEverySeriesIsHidden()
    {
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbBarSeries<Month>>(0);
                builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), "Completed");
                builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value), (Func<Month, object?>)(month =>
                    month.Completed));
                builder.AddComponentParameter(3, nameof(TbBarSeries<Month>.Hidden), true);
                builder.CloseComponent();
                Line(30, "Target")(builder);
            }));

        // The data table stays while every series is hidden, and so does the list under it.
        Assert.Empty(cut.FindAll(".tb-chart__reference, [data-part=reference]"));
        Assert.Equal(["Target: 30"], cut.Tb().Chart("Jobs by month").References);
    }

    [Fact]
    public void References_ShouldBeLeftOut_WhenTheChartHasNothingToDraw()
    {
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, [])
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .Add(chart => chart.ChildContent, builder =>
            {
                Series(builder);
                Line(30, "Target")(builder);
            }));

        Assert.Empty(cut.FindAll(".tb-chart__reference, [data-part=reference], [data-part=references]"));
    }

    [Fact]
    public void References_ShouldDrawAgain_WhenOneChangesOrLeaves()
    {
        var cut = RenderChart(Line(30, "Target"), Band(10, 25, "Normal"));

        cut.Render(parameters => parameters.Add(chart => chart.ChildContent, builder =>
        {
            Series(builder);
            Line(20, "Target")(builder);
            Band(10, 25, "Normal")(builder);
        }));

        Assert.Equal(["Target: 20", "Normal: 10 to 25"], cut.Tb().Chart("Jobs by month").References);
        Assert.Equal("50%", cut.Find("line.tb-chart__reference").GetAttribute("y1"));

        // A change of tone alone draws it again too.
        cut.Render(parameters => parameters.Add(chart => chart.ChildContent, builder =>
        {
            Series(builder);
            Line(20, "Target", Tone.Warning)(builder);
            Band(10, 25, "Normal")(builder);
        }));

        Assert.Equal("tb-chart__reference tb-chart__tone--warning", cut.Find("line.tb-chart__reference").ClassName);

        cut.Render(parameters => parameters.Add(chart => chart.ChildContent, builder =>
        {
            Series(builder);
            Line(20, "Target", Tone.Warning)(builder);
        }));

        Assert.Equal(["Target: 20"], cut.Tb().Chart("Jobs by month").References);
        Assert.Empty(cut.FindAll("rect.tb-chart__band"));
    }

    [Theory]
    [InlineData(" ", 10, 20)]
    [InlineData("Normal", 20, 10)]
    [InlineData("Normal", 10, 10)]
    [InlineData("Normal", double.NaN, 10)]
    [InlineData("Normal", 10, double.PositiveInfinity)]
    public void Band_ShouldThrow_WhenItHasNoNameOrItsValuesAreNotInOrderAndFinite(string name, double from, double to)
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => RenderChart(Band(from, to, name)));

        Assert.Equal("TbReferenceBand needs a Name and a finite From below its To.", thrown.Message);
    }

    [Theory]
    [InlineData(null, 30)]
    [InlineData("Target", double.NaN)]
    public void Line_ShouldThrow_WhenItHasNoNameOrItsValueIsNotFinite(string? name, double value)
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => RenderChart(Line(value, name)));

        Assert.Equal("TbReferenceLine needs a Name and a finite Value.", thrown.Message);
    }

    [Fact]
    public void Line_ShouldThrow_WhenItIsOutsideAChart()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbReferenceLine>(parameters => parameters
            .Add(line => line.Value, 1).Add(line => line.Name, "Target")));

        Assert.Equal("TbReferenceLine must be inside a TbChart.", thrown.Message);
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
                Series(builder);
                foreach (var one in content)
                {
                    one(builder);
                }
            })));

    private static void Series(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbBarSeries<Month>>(0);
        builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), "Completed");
        builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value), (Func<Month, object?>)(month =>
            month.Completed));
        builder.CloseComponent();
    }

    private static RenderFragment Line(double value, string? name, Tone? tone = null) => builder =>
    {
        builder.OpenComponent<TbReferenceLine>(10);
        builder.AddComponentParameter(11, nameof(TbReferenceLine.Value), value);
        builder.AddComponentParameter(12, nameof(TbReferenceLine.Name), name);
        builder.AddComponentParameter(13, nameof(TbReferenceLine.Tone), tone);
        builder.CloseComponent();
    };

    private static RenderFragment Band(double from, double to, string name, Tone? tone = null) => builder =>
    {
        builder.OpenComponent<TbReferenceBand>(20);
        builder.AddComponentParameter(21, nameof(TbReferenceBand.From), from);
        builder.AddComponentParameter(22, nameof(TbReferenceBand.To), to);
        builder.AddComponentParameter(23, nameof(TbReferenceBand.Name), name);
        builder.AddComponentParameter(24, nameof(TbReferenceBand.Tone), tone);
        builder.CloseComponent();
    };

    private static RenderFragment YAxis(double? max, string? format = null) => builder =>
    {
        builder.OpenComponent<TbYAxis>(30);
        builder.AddComponentParameter(31, nameof(TbYAxis.Max), max);
        builder.AddComponentParameter(32, nameof(TbYAxis.Format), format);
        builder.CloseComponent();
    };
}
