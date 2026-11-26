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
        // Each bar fills nine tenths of its slot, so the two of a place don't touch.
        AssertBar(new(0.08375, 0.75, 0.1575, 0.25, ChartBarEnd.Top), plan.Series[0].Marks[0]);
        AssertBar(new(0.25875, 0.25, 0.1575, 0.75, ChartBarEnd.Top), plan.Series[1].Marks[0]);
        AssertBar(new(0.58375, 0.5, 0.1575, 0.5, ChartBarEnd.Top), plan.Series[0].Marks[1]);
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

        Assert.Equal((ChartBarEnd.Top, true), (plan.Series[0].Marks[0]!.Value.End, plan.Series[0].Marks[0]!.Value.Up));
        Assert.Equal((ChartBarEnd.Bottom, false),
            (plan.Series[1].Marks[0]!.Value.End, plan.Series[1].Marks[0]!.Value.Up));
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
    public void Build_ShouldStopABarAtThePlotsEdge_WhenTheAxisStartsAboveZero()
    {
        var plan = Build(["A"], [Bars("One", 10)], axis: new(Min: 5, Max: null, Step: null, Format: null));

        // The bar's foot at zero is below the axis, and its value end is in the plot and still rounded.
        Assert.Equal((5d, 10d), (plan.Ticks.Min, plan.Ticks.Max));
        AssertBar(new(0.15, 0, 0.7, 1, ChartBarEnd.Top), plan.Series[0].Marks[0]);
    }

    [Theory]
    [InlineData(10, 0d, 0.5)]
    [InlineData(-10, 0.5, 0.5)]
    public void Build_ShouldCutABarSquareAtThePlotsEdge_WhenItsValueIsBeyondTheAxis(double value, double y,
        double height)
    {
        var plan = Build(["A"], [Bars("One", value)], axis: new(Min: -5, Max: 5, Step: null, Format: null));

        // The value is off the axis, so the bar has no rounded end to say it stops there.
        AssertBar(new(0.15, y, 0.7, height, ChartBarEnd.Cut), plan.Series[0].Marks[0]);
    }

    [Fact]
    public void Build_ShouldKeepAStacksRoundedEnd_WhenItsTotalIsARoundingErrorPastTheAxis()
    {
        // These three shares add up to a hair over 1, the end of the axis.
        var plan = Build(["A"], [Bars("One", 6), Bars("Two", 23), Bars("Three", 1)], ChartStacking.Percent);

        Assert.Equal(ChartBarEnd.Top, plan.Series[2].Marks[0]!.Value.End);
    }

    [Fact]
    public void Build_ShouldMarkMultiplesOfAStepTheAppSet_WhenTheStepHasADecimal()
    {
        var plan = Build(["A"], [Bars("One", 9)], axis: new(Min: null, Max: 10, Step: 2.5, Format: null));

        Assert.Equal(["0.0", "2.5", "5.0", "7.5", "10.0"], plan.ValueTicks.Select(tick => tick.Text));
        Assert.Equal([1, 0.75, 0.5, 0.25, 0], plan.ValueTicks.Select(tick => tick.Y));
    }

    [Fact]
    public void Build_ShouldEndTheAxisOnTheStepTheAppSet_WhenItsEndsAreLeftToTheChart()
    {
        var plan = Build(["A"], [Bars("One", 60)], axis: new(Min: null, Max: null, Step: 25, Format: null));

        Assert.Equal(new ChartTicks(0, 75, 25), plan.Ticks);
        Assert.Equal(["0", "25", "50", "75"], plan.ValueTicks.Select(tick => tick.Text));
    }

    [Theory]
    [InlineData(100d, null, 100, 110)]
    [InlineData(null, -5d, -5.2, -5)]
    public void Build_ShouldKeepTheEndTheAppSetAndSomeHeight_WhenThatEndIsBeyondEveryValue(double? min, double? max,
        double from, double to)
    {
        var plan = Build(["A"], [Bars("One", 60)], axis: new(min, max, Step: null, Format: null));

        Assert.Equal((from, to), (plan.Ticks.Min, plan.Ticks.Max));
    }

    [Fact]
    public void Build_ShouldPassOverAStep_WhenItWouldGiveMoreMarksThanAnAxisCanShow()
    {
        var plan = Build(["A"], [Bars("One", 60)], axis: new(Min: null, Max: null, Step: 0.001, Format: null));

        Assert.Equal(10, plan.Ticks.Step);
    }

    [Fact]
    public void Build_ShouldHaveNoMarks_WhenNoMultipleOfTheStepIsBetweenTheEnds()
    {
        var plan = Build(["A"], [Line("One", 0.7)], axis: new(Min: 0.5, Max: 0.9, Step: 1, Format: null));

        Assert.Empty(plan.ValueTicks);
    }

    [Fact]
    public void Build_ShouldKeepTheDecimalsOfSmallValues_WhenWritingThemForTheTable()
    {
        var plan = Build(["A", "B"], [Bars("Dose", 0.0004, 0.00025)]);

        Assert.Equal(["0.0004", "0.00025"], plan.Series[0].Text);
    }

    [Fact]
    public void Build_ShouldNotReadValuesAsShares_WhenPercentStackingHasNoBars()
    {
        var plan = Build(["A", "B"], [Line("Level", 10, 40)], ChartStacking.Percent);

        Assert.Equal(["10", "15", "20", "25", "30", "35", "40"], plan.ValueTicks.Select(tick => tick.Text));
    }

    [Fact]
    public void Build_ShouldLeaveAHiddenSeriesOutOfTheDrawingAndTheAxis_WhenGivenOne()
    {
        var plan = Build(["A"], [Bars("One", 10) with { Hidden = true }, Bars("Two", 4),
            Line("Three", 100) with { Hidden = true }]);

        Assert.Equal((0d, 4d), (plan.Ticks.Min, plan.Ticks.Max));
        // The one bar that shows has the whole of the place's share, as a lone series does.
        AssertBar(new(0.15, 0, 0.7, 1, ChartBarEnd.Top), plan.Series[1].Marks[0]);
        // A hidden series keeps its text, for the data table.
        Assert.Equal((true, 0, "10"), (plan.Series[0].Hidden, plan.Series[0].Marks.Count, plan.Series[0].Text[0]));
        Assert.Equal((true, "", 0, "100"), (plan.Series[2].Hidden, plan.Series[2].Path, plan.Series[2].Points.Count,
            plan.Series[2].Text[0]));
    }

    [Fact]
    public void Build_ShouldPileOnlyTheSeriesThatShow_WhenOneOfAStackIsHidden()
    {
        var plan = Build(["A"], [Bars("One", 10), Bars("Two", 30) with { Hidden = true }, Bars("Three", 10)],
            ChartStacking.Stacked);

        Assert.Equal((0d, 20d), (plan.Ticks.Min, plan.Ticks.Max));
        AssertBar(new(0.15, 0.5, 0.7, 0.5, ChartBarEnd.None), plan.Series[0].Marks[0]);
        AssertBar(new(0.15, 0, 0.7, 0.5, ChartBarEnd.Top), plan.Series[2].Marks[0]);
    }

    [Fact]
    public void Build_ShouldFillAnAreaFromItsLineDownToZero_WhenGivenAnAreaSeries()
    {
        var plan = Build(["A", "B"], [Area("One", 10, 20)]);

        // An area is read from zero, as a bar is, so the axis reaches it.
        Assert.Equal((0d, 20d), (plan.Ticks.Min, plan.Ticks.Max));
        Assert.Equal(("M2500 5000l5000 -5000", "M2500 5000l5000 -5000v10000l-5000 0z"),
            (plan.Series[0].Path, plan.Series[0].Fill));
        Assert.Equal([(0.25, 0.5), (0.75, 0)], plan.Series[0].Points);
    }

    [Fact]
    public void Build_ShouldFillPastThePlotsEdge_WhenTheAxisStartsAboveZero()
    {
        var plan = Build(["A", "B"], [Area("One", 10, 20)], axis: new(Min: 10, Max: null, Step: null, Format: null));

        // Zero is as far below the plot as the plot is high, and the fills' drawing ends at the plot's edge.
        Assert.Equal((10d, 20d), (plan.Ticks.Min, plan.Ticks.Max));
        Assert.Equal("M2500 10000l5000 -10000v20000l-5000 0z", plan.Series[0].Fill);
    }

    [Fact]
    public void Build_ShouldPileAreasOnAreasAndBarsOnBars_WhenStacked()
    {
        var plan = Build(["A", "B"], [Area("One", 10, 20), Area("Two", 30, 20), Bars("Three", 5, 5)],
            ChartStacking.Stacked);

        Assert.Equal((0d, 40d), (plan.Ticks.Min, plan.Ticks.Max));
        // The second area's line is the total, and its floor is the first area's line.
        Assert.Equal(("M2500 0l5000 0", "M2500 0l5000 0v5000l-5000 2500z"), (plan.Series[1].Path, plan.Series[1].Fill));
        // The bar starts from zero, not from the top of the areas.
        AssertBar(new(0.075, 0.875, 0.35, 0.125, ChartBarEnd.Top), plan.Series[2].Marks[0]);
    }

    [Fact]
    public void Build_ShouldDrawAStackedAreasLowerEdgeWithItsOwnCurve_WhenTheAreaUnderItHasAnother()
    {
        var plan = Build(["A", "B", "C"], [Area("One", 10, 30, 10),
            Area("Two", 10, 10, 10) with { Curve = ChartCurve.Smooth }], ChartStacking.Stacked);

        // The area under it has a straight line, and this one comes back along curves: a stack shares one curve.
        Assert.DoesNotContain('c', plan.Series[0].Path);
        Assert.Matches(@"v\d+(c[-\d ]+){2}z$", plan.Series[1].Fill);
    }

    [Fact]
    public void Build_ShouldDrawAreasAsSharesOfEachPlacesTotal_WhenStackedAsPercent()
    {
        var plan = Build(["A", "B"], [Area("One", 1, 3), Area("Two", 3, 1)], ChartStacking.Percent);

        Assert.Equal(new ChartTicks(0, 1, 0.2), plan.Ticks);
        Assert.Equal([(0.25, 0.75), (0.75, 0.25)], plan.Series[0].Points);
        Assert.Equal([(0.25, 0d), (0.75, 0d)], plan.Series[1].Points);
        Assert.Equal(["1", "3"], plan.Series[0].Text);
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

    private static ChartSeriesValues Area(string name, params double?[] values) =>
        new(name, false, ChartCurve.Straight, values, Filled: true);

    private static void AssertBar(ChartBar expected, ChartBar? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.End, actual.Value.End);
        Assert.Equal(Rounded(expected.X, expected.Y, expected.Width, expected.Height),
            Rounded(actual.Value.X, actual.Value.Y, actual.Value.Width, actual.Value.Height));
    }

    private static double[] Rounded(params double[] shares) => [.. shares.Select(share => Math.Round(share, 9))];
}
