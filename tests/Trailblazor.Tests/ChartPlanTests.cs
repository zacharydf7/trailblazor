using System.Globalization;

namespace Trailblazor.Tests;

public class ChartPlanTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void Build_ShouldPutBarsSideBySideFromZero_WhenNotStacked()
    {
        var plan = Build(["A", "B"], [Bars("One", 10, 20), Bars("Two", 30, 40)]);

        Assert.Equal(new ChartTicks(0, 40, 10), plan.Ticks);
        AssertBar(new(0.075, 0.75, 0.175, 0.25, ChartBarEnd.Top), plan.Series[0].Marks[0]);
        AssertBar(new(0.25, 0.25, 0.175, 0.75, ChartBarEnd.Top), plan.Series[1].Marks[0]);
        AssertBar(new(0.575, 0.5, 0.175, 0.5, ChartBarEnd.Top), plan.Series[0].Marks[1]);
    }

    [Fact]
    public void Build_ShouldPileBarsAndRoundOnlyTheOutermost_WhenStacked()
    {
        var plan = Build(["A"], [Bars("One", 10), Bars("Two", 30)], ChartStacking.Stacked);

        Assert.Equal(new ChartTicks(0, 40, 10), plan.Ticks);
        AssertBar(new(0.15, 0.75, 0.7, 0.25, ChartBarEnd.None), plan.Series[0].Marks[0]);
        AssertBar(new(0.15, 0, 0.7, 0.75, ChartBarEnd.Top), plan.Series[1].Marks[0]);
    }

    [Fact]
    public void Build_ShouldRoundTheLowerOfTwoBars_WhenTheUpperOneOfAStackIsEmpty()
    {
        var plan = Build(["A"], [Bars("One", 10), Bars("Two", 0)], ChartStacking.Stacked);

        Assert.Equal(ChartBarEnd.Top, plan.Series[0].Marks[0]!.Value.End);
    }

    [Fact]
    public void Build_ShouldRoundBothEndsOfAStack_WhenItHasAValueOnEachSideOfZero()
    {
        var plan = Build(["A"], [Bars("Gain", 5), Bars("Loss", -3)], ChartStacking.Stacked);

        Assert.Equal(ChartBarEnd.Top, plan.Series[0].Marks[0]!.Value.End);
        Assert.Equal(ChartBarEnd.Bottom, plan.Series[1].Marks[0]!.Value.End);
    }

    [Fact]
    public void Build_ShouldHangABarFromZero_WhenItsValueIsBelowZero()
    {
        var plan = Build(["A", "B"], [Bars("Change", -5, 5)]);

        Assert.Equal(new ChartTicks(-6, 6, 2), plan.Ticks);
        AssertBar(new(0.075, 0.5, 0.35, 5d / 12, ChartBarEnd.Bottom), plan.Series[0].Marks[0]);
        AssertBar(new(0.575, 1d / 12, 0.35, 5d / 12, ChartBarEnd.Top), plan.Series[0].Marks[1]);
    }

    [Fact]
    public void Build_ShouldStartTheAxisWhereTheValuesDo_WhenEverySeriesIsALine()
    {
        var plan = Build(["A", "B"], [Line("Level", 980, 1020)]);

        Assert.Equal(new ChartTicks(980, 1020, 10), plan.Ticks);
        Assert.Equal("M2500 10000l5000 -10000", plan.Series[0].Path);
    }

    [Fact]
    public void Build_ShouldReachZero_WhenALineSharesTheChartWithBars()
    {
        var plan = Build(["A", "B"], [Line("Level", 980, 1020), Bars("Count", 100, 200)]);

        Assert.Equal(0, plan.Ticks.Min);
    }

    [Fact]
    public void Build_ShouldLabelEveryTickWithTheStepsDecimals_WhenTheStepIsAFraction()
    {
        var plan = Build(["A", "B"], [Bars("Rain", 0.4, 2.5)]);

        Assert.Equal(["0.0", "0.5", "1.0", "1.5", "2.0", "2.5"], plan.ValueTicks.Select(tick => tick.Text));
        Assert.Equal([true, false, false, false, false, false], plan.ValueTicks.Select(tick => tick.Zero));
        Assert.Equal([1, 0.8, 0.6, 0.4, 0.2, 0], plan.ValueTicks.Select(tick => Math.Round(tick.Y, 9)));
    }

    [Fact]
    public void Build_ShouldDrawSharesOfEachPlacesTotal_WhenStackedAsPercent()
    {
        var plan = Build(["A"], [Bars("One", 1), Bars("Two", 3)], ChartStacking.Percent);

        Assert.Equal(["0%", "20%", "40%", "60%", "80%", "100%"], plan.ValueTicks.Select(tick => tick.Text));
        AssertBar(new(0.15, 0.75, 0.7, 0.25, ChartBarEnd.None), plan.Series[0].Marks[0]);
        AssertBar(new(0.15, 0, 0.7, 0.75, ChartBarEnd.Top), plan.Series[1].Marks[0]);
        Assert.Equal(["1"], plan.Series[0].Text);
    }

    [Fact]
    public void Build_ShouldKeepTheEndsAndStepTheAppSet_WhenTheAxisHasThem()
    {
        var plan = Build(["A"], [Bars("One", 60)], axis: new(Min: 0, Max: 95, Step: 25, Format: "0 kg"));

        Assert.Equal(["0 kg", "25 kg", "50 kg", "75 kg"], plan.ValueTicks.Select(tick => tick.Text));
        Assert.Equal(["60 kg"], plan.Series[0].Text);
        Assert.Equal(1 - 60d / 95, plan.Series[0].Marks[0]!.Value.Y, 9);
    }

    [Fact]
    public void Build_ShouldLeaveAGap_WhenAValueIsMissing()
    {
        var plan = Build(["A", "B", "C"], [Bars("Count", null, 5, 5), Line("Level", 1, null, 3)]);

        Assert.Null(plan.Series[0].Marks[0]);
        Assert.Equal([null, "5", "5"], plan.Series[0].Text);
        Assert.Null(plan.Series[1].Points[1]);
        Assert.Equal(2, plan.Series[1].Path.Count(letter => letter == 'M'));
    }

    [Fact]
    public void Build_ShouldDrawNothingAndKeepAnAxis_WhenThereAreNoValues()
    {
        var plan = Build([], []);

        Assert.Equal(new ChartTicks(0, 1, 0.2), plan.Ticks);
        Assert.Empty(plan.Series);
    }

    [Theory]
    [InlineData(12, 1)]
    [InlineData(48, 1)]
    [InlineData(49, 2)]
    [InlineData(1000, 32)]
    public void LabelEvery_ShouldKeepTheLabelsToWhatAPlotCanShow_WhenTheAxisIsLong(int places, int every)
    {
        var plan = Build([.. Enumerable.Range(0, places).Select(place => $"{place}")], []);

        Assert.Equal(every, plan.LabelEvery);
    }

    [Fact]
    public void Stride_ShouldBeWidestForTheFirstLabel_WhenRead()
    {
        var plan = Build([.. Enumerable.Range(0, 1000).Select(place => $"{place}")], []);

        Assert.Equal([1024, 32, 64, 32, 128], new[] { 0, 32, 64, 96, 128 }.Select(plan.Stride));
    }

    [Fact]
    public void Middle_ShouldBeHalfwayAcrossThePlace_WhenRead()
    {
        var plan = Build(["A", "B", "C", "D"], []);

        Assert.Equal([0.125, 0.875], [plan.Middle(0), plan.Middle(3)]);
    }

    private static ChartPlan Build(string[] places, ChartSeriesValues[] series,
        ChartStacking stacking = ChartStacking.None, ChartValueAxis axis = default) =>
        ChartPlan.Build(places, series, stacking, axis, English);

    private static ChartSeriesValues Bars(string name, params double?[] values) =>
        new(name, true, ChartCurve.Straight, values);

    private static ChartSeriesValues Line(string name, params double?[] values) =>
        new(name, false, ChartCurve.Straight, values);

    private static void AssertBar(ChartBar expected, ChartBar? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.End, actual.Value.End);
        Assert.Equal(Rounded(expected.X, expected.Y, expected.Width, expected.Height),
            Rounded(actual.Value.X, actual.Value.Y, actual.Value.Width, actual.Value.Height));
    }

    private static double[] Rounded(params double[] shares) => [.. shares.Select(share => Math.Round(share, 9))];
}
