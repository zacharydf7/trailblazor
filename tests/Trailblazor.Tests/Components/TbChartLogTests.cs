using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// A logarithmic value axis, for values that span several powers of ten.
public class TbChartLogTests : BunitContext
{
    public TbChartLogTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public void Axis_ShouldMarkThePowersOfTenThatHoldTheValues_WhenLogarithmic()
    {
        var cut = RenderChart([3, 40, 500, 40000]);

        Assert.Equal(["1 100%", "10 80%", "100 60%", "1,000 40%", "10,000 20%", "100,000 0%"], Ticks(cut));
    }

    [Fact]
    public void Axis_ShouldWriteEachMarkWithOnlyItsOwnDecimals_WhenTheValuesAreBelowOne()
    {
        var cut = RenderChart([0.02, 0.5, 7]);

        Assert.Equal(["0.01 100%", "0.1 66.667%", "1 33.333%", "10 0%"], Ticks(cut));
    }

    [Fact]
    public void Line_ShouldPlaceEachTimesTenAnEqualStepUp_WhenLogarithmic()
    {
        var cut = RenderChart([1, 100, 10000]);

        // On an axis from 1 to 10,000, 100 is halfway up.
        Assert.Equal(["10000", "5000", "0"], cut.Find("path.tb-chart__line").GetAttribute("data-points")!.Split(' ')
            .Select(point => point.Split(',')[1]));
    }

    [Fact]
    public void Line_ShouldHaveAGapAndTheTableTheValue_WhereAValueIsZeroOrBelow()
    {
        var cut = RenderChart([5, 0, -3, 50]);

        Assert.Equal(["-", "-"], cut.Find("path.tb-chart__line").GetAttribute("data-points")!.Split(' ')[1..3]);
        var chart = cut.Tb().Chart("Counts");
        Assert.Equal(["5", "0", "-3", "50"], chart.Places.Select(place => chart.Value("Count", place)));
        Assert.Equal(["1 100%", "10 50%", "100 0%"], Ticks(cut));
    }

    [Fact]
    public void Bar_ShouldRiseFromTheAxisLowEnd_WhenLogarithmic()
    {
        var cut = RenderChart([100, 1000], bars: true);

        // The axis starts a power of ten below the lowest bar, so 100 on an axis from 10 to 1,000 is halfway up.
        var bar = cut.FindAll("g[data-series='Count'] rect")[0];
        Assert.Equal(("50%", "50%"), (bar.GetAttribute("y"), bar.GetAttribute("height")));
        Assert.Equal(["10", "100", "1,000"], Ticks(cut).Select(tick => tick.Split(' ')[0]));
    }

    [Fact]
    public void Axis_ShouldRunBetweenTheAppsEnds_WhenItSetsThem()
    {
        var cut = RenderChart([30, 400], min: 5, max: 2000);

        // From 5 to 2,000 the marks are 10, 100 and 1,000, and the first sits above the axis's low end.
        var ticks = Ticks(cut);
        Assert.Equal(["10", "100", "1,000"], ticks.Select(tick => tick.Split(' ')[0]));
        Assert.NotEqual("100%", ticks[0].Split(' ')[1]);
    }

    [Theory]
    [InlineData(null, 5d, "1 69.897%")]
    [InlineData(5000d, null, "10,000 0%")]
    public void Axis_ShouldKeepAPowerOfTenOfHeight_WhenTheAppsEndIsBeyondEveryValue(double? min, double? max,
        string tick)
    {
        // A Max of 5 below every value gives an axis from 0.5 to 5, and a Min of 5,000 above them one up to 10,000.
        var cut = RenderChart([100, 200], min: min, max: max);

        Assert.Equal([tick], Ticks(cut));
    }

    [Fact]
    public void Axis_ShouldMarkPowersBelowTheSmallestDouble_WhenTheValuesAreTiny()
    {
        var cut = RenderChart([3e-18, 4e-17], format: "E0");

        Assert.Equal(["1E-018", "1E-017", "1E-016"], Ticks(cut).Select(tick => tick.Split(' ')[0]));
    }

    [Fact]
    public void Reference_ShouldBeListedAndNotDrawn_WhenItsValueIsZeroOrBelow()
    {
        var cut = RenderChart([5, 50], reference: 0);

        Assert.Equal(["Floor: 0"], cut.Tb().Chart("Counts").References);
        Assert.Empty(cut.FindAll("line.tb-chart__reference"));
    }

    [Fact]
    public void Area_ShouldBeFilledFromTheAxisLowEnd_WhenLogarithmic()
    {
        var cut = RenderChart([10, 1000], area: true);

        // The fill goes down from 1,000 at the top to the bottom of the plot and back along it.
        Assert.EndsWith("v10000l-5000 0z", cut.Find("path.tb-chart__area").GetAttribute("d")!);
    }

    [Theory]
    [InlineData(0d, null, "A Logarithmic TbYAxis needs a Min and a Max above zero.")]
    [InlineData(null, -5d, "A Logarithmic TbYAxis needs a Min and a Max above zero.")]
    public void Axis_ShouldThrow_WhenAnEndIsZeroOrBelow(double? min, double? max, string message)
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => RenderChart([1, 10], min: min, max: max));

        Assert.Equal(message, thrown.Message);
    }

    [Fact]
    public void Chart_ShouldThrow_WhenItStacksToPercentOnALogarithmicAxis()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            RenderChart([1, 10], bars: true, stacking: ChartStacking.Percent));

        Assert.Equal("A Logarithmic TbYAxis can't show a chart that stacks to Percent.", thrown.Message);
    }

    private static List<string> Ticks(IRenderedComponent<TbChart<double>> cut) =>
        [.. cut.FindAll(".tb-chart__y > span:not(.tb-chart__y-sizer)")
            .Select(tick => $"{tick.TextContent} {tick.GetAttribute("style")!["top: ".Length..]}")];

    private IRenderedComponent<TbChart<double>> RenderChart(double[] values, bool bars = false, double? min = null,
        double? max = null, ChartStacking stacking = ChartStacking.None, string? format = null,
        double? reference = null, bool area = false) =>
        Render<TbChart<double>>(parameters => parameters.Add(chart => chart.Items, values)
            .Add(chart => chart.X, value => Array.IndexOf(values, value).ToString(CultureInfo.InvariantCulture))
            .Add(chart => chart.Title, "Counts").Add(chart => chart.Stacking, stacking)
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbYAxis>(0);
                builder.AddComponentParameter(1, nameof(TbYAxis.Logarithmic), true);
                builder.AddComponentParameter(2, nameof(TbYAxis.Min), min);
                builder.AddComponentParameter(3, nameof(TbYAxis.Max), max);
                builder.AddComponentParameter(4, nameof(TbYAxis.Format), format);
                builder.CloseComponent();
                if (reference is { } value)
                {
                    builder.OpenComponent<TbReferenceLine>(5);
                    builder.AddComponentParameter(6, nameof(TbReferenceLine.Name), "Floor");
                    builder.AddComponentParameter(7, nameof(TbReferenceLine.Value), value);
                    builder.CloseComponent();
                }

                Series(builder, bars, area);
            }));

    private static void Series(RenderTreeBuilder builder, bool bars, bool area)
    {
        if (bars)
        {
            builder.OpenComponent<TbBarSeries<double>>(10);
        }
        else if (area)
        {
            builder.OpenComponent<TbAreaSeries<double>>(10);
        }
        else
        {
            builder.OpenComponent<TbLineSeries<double>>(10);
        }

        builder.AddComponentParameter(11, nameof(TbLineSeries<double>.Name), "Count");
        builder.AddComponentParameter(12, nameof(TbLineSeries<double>.Value), (Func<double, object?>)(value => value));
        builder.CloseComponent();
    }
}
