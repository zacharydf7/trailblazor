namespace Trailblazor.Tests;

public class ChartTicksTests
{
    [Theory]
    [InlineData(0, 100, 0, 100, 20)]
    [InlineData(0, 87, 0, 100, 20)]
    [InlineData(3, 47, 0, 50, 10)]
    [InlineData(-30, 70, -40, 80, 20)]
    [InlineData(0, 1234567, 0, 1400000, 200000)]
    [InlineData(0.1, 0.3, 0.1, 0.3, 0.05)]
    [InlineData(980, 1020, 980, 1020, 10)]
    public void Cover_ShouldPutTheEndsOnRoundSteps_WhenGivenARange(double lowest, double highest, double min,
        double max, double step)
    {
        var ticks = ChartTicks.Cover(lowest, highest);

        Assert.Equal(new ChartTicks(min, max, step), ticks);
    }

    [Theory]
    [InlineData(5, 5, 0, 5)]
    [InlineData(-5, -5, -5, 0)]
    [InlineData(0, 0, 0, 1)]
    public void Cover_ShouldReachFromZero_WhenGivenOneValue(double lowest, double highest, double min, double max)
    {
        var ticks = ChartTicks.Cover(lowest, highest);

        Assert.Equal((min, max), (ticks.Min, ticks.Max));
    }

    // 0.07 / 0.01 is a hair over 7 and 0.3 / 0.1 a hair under 3 in floating point.
    [Theory]
    [InlineData(0, 0.07, 5, 0.01)]
    [InlineData(0.3, 1.1, 8, 0.1)]
    public void Cover_ShouldKeepAnEndThatIsAlreadyOnAStep_WhenDivisionIsInexact(double lowest, double highest,
        int count, double step)
    {
        var ticks = ChartTicks.Cover(lowest, highest, count);

        Assert.Equal(new ChartTicks(lowest, highest, step), ticks);
    }

    [Fact]
    public void Cover_ShouldTakeAboutTheStepsAskedFor_WhenGivenACount()
    {
        var ticks = ChartTicks.Cover(0, 100, count: 10);

        Assert.Equal(10, ticks.Step);
    }

    [Fact]
    public void Values_ShouldBeExactDecimals_WhenTheStepIsAFraction()
    {
        double[] expected = [0.1, 0.15, 0.2, 0.25, 0.3];

        Assert.Equal(expected, ChartTicks.Cover(0.1, 0.3).Values);
    }

    [Fact]
    public void Values_ShouldRunFromEndToEnd_WhenTheRangeCrossesZero()
    {
        double[] expected = [-40, -20, 0, 20, 40, 60, 80];

        Assert.Equal(expected, ChartTicks.Cover(-30, 70).Values);
    }

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(0, 1, 1)]
    [InlineData(0.1, 0.3, 2)]
    [InlineData(0, 0.004, 3)]
    public void Decimals_ShouldBeWhatTheStepNeeds_WhenRead(double lowest, double highest, int decimals)
    {
        Assert.Equal(decimals, ChartTicks.Cover(lowest, highest).Decimals);
    }

    [Theory]
    [InlineData(2.5, 10, 1, new[] { 0, 2.5, 5, 7.5, 10 })]
    [InlineData(0.25, 1, 2, new[] { 0, 0.25, 0.5, 0.75, 1 })]
    [InlineData(15, 60, 0, new double[] { 0, 15, 30, 45, 60 })]
    public void Values_ShouldBeEveryMultipleOfTheStep_WhenTheStepHasMoreThanOneDigit(double step, double max,
        int decimals, double[] values)
    {
        var ticks = new ChartTicks(0, max, step);

        Assert.Equal(decimals, ticks.Decimals);
        Assert.Equal(values, ticks.Values);
    }

    [Theory]
    [InlineData(3, 100, 20, new double[] { 20, 40, 60, 80, 100 })]
    [InlineData(2.5, 8, 1, new double[] { 3, 4, 5, 6, 7, 8 })]
    [InlineData(45, 98, 10, new double[] { 50, 60, 70, 80, 90 })]
    [InlineData(0.5, 0.9, 1, new double[0])]
    public void Values_ShouldStayOnMultiplesOfTheStep_WhenAnEndIsNotOnOne(double min, double max, double step,
        double[] values)
    {
        Assert.Equal(values, new ChartTicks(min, max, step).Values);
    }

    [Fact]
    public void Share_ShouldBeTheValuesPlaceBetweenTheEnds_WhenCalled()
    {
        var ticks = new ChartTicks(-40, 80, 20);

        Assert.Equal((0, 1d / 3, 1), (ticks.Share(-40), ticks.Share(0), ticks.Share(80)));
    }
}
