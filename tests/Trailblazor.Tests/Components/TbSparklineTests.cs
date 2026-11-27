using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Trailblazor.Tests.Components;

public class TbSparklineTests : BunitContext
{
    public TbSparklineTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldDrawALineFromItsLowestValueToItsHighest_WhenGivenValues()
    {
        var cut = RenderSparkline([10, 20, 40]);

        var picture = cut.Find("svg");
        Assert.Equal(("tb-sparkline", "img", "Jobs a week", "0 0 10000 10000", "none"), (picture.ClassName,
            picture.GetAttribute("role"), picture.GetAttribute("aria-label"), picture.GetAttribute("viewBox"),
            picture.GetAttribute("preserveAspectRatio")));
        // The first value is at the left edge and the bottom, the last at the right edge and the top, and 20 is a
        // third of the way up between 10 and 40.
        Assert.Equal(Path((0, 1), (0.5, 2 / 3d), (1, 0)), cut.Find("path.tb-sparkline__line").GetAttribute("d"));
        Assert.Empty(cut.FindAll(".tb-sparkline__area, .tb-sparkline__bar"));
    }

    [Fact]
    public void Render_ShouldFillFromZeroUnderTheLine_WhenAnArea()
    {
        var cut = RenderSparkline([10, 20], SparklineKind.Area);

        // The bottom is zero, not the lowest value, so 10 is halfway up.
        (double, double)?[] line = [(0, 0.5), (1, 0)];
        Assert.Equal(ChartPath.Line(line, ChartCurve.Straight), cut.Find("path.tb-sparkline__line").GetAttribute("d"));
        Assert.Equal(ChartPath.Area(line, [(0, 1), (1, 1)], ChartCurve.Straight),
            cut.Find("path.tb-sparkline__area").GetAttribute("d"));
    }

    [Fact]
    public void Render_ShouldDrawABarFromZeroForEachValue_WhenBars()
    {
        var cut = RenderSparkline([10d, -10d, null, 20d], SparklineKind.Bars);

        // From -10 to 20, zero is two thirds of the way down. Each value has a quarter of the width, and a bar
        // takes eight tenths of that. The third value is missing, and has no bar.
        Assert.Equal([("2.5%", "33.333%", "20%", "33.333%"), ("27.5%", "66.667%", "20%", "33.333%"),
            ("77.5%", "0%", "20%", "66.667%")], cut.FindAll("rect.tb-sparkline__bar").Select(bar =>
            (bar.GetAttribute("x"), bar.GetAttribute("y"), bar.GetAttribute("width"), bar.GetAttribute("height"))));
        Assert.Empty(cut.FindAll("path"));
    }

    [Fact]
    public void Render_ShouldMeasureBetweenMinAndMax_WhenTheyAreGiven()
    {
        var cut = Render<TbSparkline<int>>(parameters => parameters.Add(spark => spark.Values, [5, 15, 30])
            .Add(spark => spark.Min, 0).Add(spark => spark.Max, 20));

        // 30 is beyond the top, and is drawn on the picture's edge.
        Assert.Equal(Path((0, 0.75), (0.5, 0.25), (1, 0)), cut.Find("path.tb-sparkline__line").GetAttribute("d"));
    }

    [Theory]
    [InlineData(0d, null, new[] { -3, -1 }, 1d)]
    [InlineData(null, 0d, new[] { 2, 5 }, 0d)]
    public void Render_ShouldDrawOnTheEdge_WhenAnEndIsSetBeyondEveryValue(double? min, double? max, int[] values,
        double edge)
    {
        var cut = Render<TbSparkline<int>>(parameters => parameters.Add(spark => spark.Values, values)
            .Add(spark => spark.Min, min).Add(spark => spark.Max, max));

        // A bottom above every value puts them all on the bottom edge, and a top below them all on the top one.
        Assert.Equal(Path((0, edge), (1, edge)), cut.Find("path.tb-sparkline__line").GetAttribute("d"));
    }

    [Fact]
    public void Render_ShouldBreakTheLine_WhenAValueIsMissing()
    {
        var cut = RenderSparkline([1d, 3d, null, 2d, 3d]);

        Assert.Equal(Path((0, 1), (0.25, 0), null, (0.75, 0.5), (1, 0)),
            cut.Find("path.tb-sparkline__line").GetAttribute("d"));
    }

    [Theory]
    [InlineData(new[] { 7, 7, 7 }, "M0 5000")]
    [InlineData(new[] { 7 }, "M5000 5000h0")]
    public void Render_ShouldDrawAlongTheMiddle_WhenThereIsNothingToScaleBy(int[] values, string start)
    {
        var cut = Render<TbSparkline<int>>(parameters => parameters.Add(spark => spark.Values, values));

        // Values that are all the same run along the middle, and one value alone is a dot there.
        Assert.StartsWith(start, cut.Find("path.tb-sparkline__line").GetAttribute("d"));
        Assert.DoesNotContain("-", cut.Find("path.tb-sparkline__line").GetAttribute("d")![1..]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_ShouldDrawNothing_WhenThereAreNoValues(bool given)
    {
        var cut = Render<TbSparkline<double?>>(parameters => parameters.Add(spark => spark.Values,
            given ? [null, double.NaN] : null).Add(spark => spark.Kind, SparklineKind.Bars));

        Assert.Empty(cut.Find("svg").Children);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Render_ShouldHideThePictureFromScreenReaders_WhenItHasNoName(string? name)
    {
        var cut = Render<TbSparkline<int>>(parameters => parameters.Add(spark => spark.Values, [1, 2])
            .Add(spark => spark.AriaLabel, name));

        var picture = cut.Find("svg");
        Assert.Equal(("true", false, false), (picture.GetAttribute("aria-hidden"), picture.HasAttribute("role"),
            picture.HasAttribute("aria-label")));
    }

    [Fact]
    public void Render_ShouldTakeItsToneColorAndSize_WhenTheyAreGiven()
    {
        var cut = Render<TbSparkline<int>>(parameters => parameters.Add(spark => spark.Values, [1, 2])
            .Add(spark => spark.Tone, Tone.Danger).Add(spark => spark.Color, "rebeccapurple")
            .Add(spark => spark.Width, "8rem").Add(spark => spark.Height, "2rem")
            .Add(spark => spark.Class, "mine").Add(spark => spark.Style, "margin: 0"));

        var picture = cut.Find("svg");
        Assert.Equal("tb-sparkline tb-sparkline--danger mine", picture.ClassName);
        Assert.Equal("--tb-sparkline-width: 8rem; --tb-sparkline-height: 2rem; "
            + "--_tb-sparkline-color: rebeccapurple; margin: 0", picture.GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAValueIsNotANumber()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbSparkline<string>>(parameters =>
            parameters.Add(spark => spark.Values, ["many"])));

        Assert.Equal("TbSparkline got a value of type String, which isn't a number.", thrown.Message);
    }

    [Fact]
    public void Telemetry_ShouldCountAShow_WhenGivenATelemetryName()
    {
        using var shown = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.shown");

        Render<TbSparkline<int>>(parameters => parameters.Add(spark => spark.Values, [1, 2])
            .Add(spark => spark.TelemetryName, "hikers-this-week"));

        Assert.Single(shown.GetMeasurementSnapshot());
    }

    private IRenderedComponent<TbSparkline<double?>> RenderSparkline(double?[] values,
        SparklineKind kind = SparklineKind.Line) =>
        Render<TbSparkline<double?>>(parameters => parameters.Add(spark => spark.Values, values)
            .Add(spark => spark.Kind, kind).Add(spark => spark.AriaLabel, "Jobs a week"));

    // A line through these spots, each a share of the picture from its left and its top; null is a gap.
    private static string Path(params (double X, double Y)?[] spots) => ChartPath.Line(spots, ChartCurve.Straight);
}
