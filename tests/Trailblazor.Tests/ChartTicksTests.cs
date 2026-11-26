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

    [Fact]
    public void Share_ShouldBeTheValuesPlaceBetweenTheEnds_WhenCalled()
    {
        var ticks = new ChartTicks(-40, 80, 20);

        Assert.Equal((0, 1d / 3, 1), (ticks.Share(-40), ticks.Share(0), ticks.Share(80)));
    }
}
