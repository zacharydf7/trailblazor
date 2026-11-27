namespace Trailblazor.Tests;

public class GaugeArcTests
{
    [Fact]
    public void Path_ShouldRunClockwiseFromTheLowerLeftToTheLowerRight_WhenGivenTheWholeArc()
    {
        // Three quarters of a circle around the middle of a box 100 wide, so the long way around.
        Assert.Equal("M18.18 81.82 A45 45 0 1 1 81.82 81.82", GaugeArc.Path(0, 1, 45));
    }

    [Fact]
    public void Path_ShouldEndAtTheTop_WhenGivenTheFirstHalfOfTheArc()
    {
        Assert.Equal("M18.18 81.82 A45 45 0 0 1 50 5", GaugeArc.Path(0, 0.5, 45));
    }

    [Fact]
    public void Path_ShouldStartWhereTheShareBeforeItEnds_WhenGivenALaterPart()
    {
        Assert.Equal("M50 10 A40 40 0 0 1 78.28 78.28", GaugeArc.Path(0.5, 1, 40));
    }

    [Theory]
    [InlineData(0.5, 0.5)]
    [InlineData(0.6, 0.4)]
    public void Path_ShouldBeEmpty_WhenItHasNoLength(double from, double to)
    {
        Assert.Equal("", GaugeArc.Path(from, to, 45));
    }
}
