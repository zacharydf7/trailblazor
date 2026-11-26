namespace Trailblazor.Tests;

public class ChartStackTests
{
    [Fact]
    public void Pile_ShouldStartEachBarWhereTheLastEnded_WhenValuesAreAboveZero()
    {
        var piled = ChartStack.Pile([[1, 2], [3, 4]], asShares: false);

        Assert.Equal(Bars((0, 1), (0, 2)), piled[0]);
        Assert.Equal(Bars((1, 4), (2, 6)), piled[1]);
    }

    [Fact]
    public void Pile_ShouldPileDownFromZero_WhenAValueIsBelowZero()
    {
        var piled = ChartStack.Pile([[5], [-3], [2], [-1]], asShares: false);

        Assert.Equal(Bars((0, 5), (0, -3), (5, 7), (-3, -4)), piled.Select(series => series[0]));
    }

    [Fact]
    public void Pile_ShouldTakeNoRoom_WhenAValueIsMissing()
    {
        var piled = ChartStack.Pile([[null], [2]], asShares: false);

        Assert.Equal(Bars(null, (0, 2)), piled.Select(series => series[0]));
    }

    [Fact]
    public void Pile_ShouldAddUpToOne_WhenAskedForShares()
    {
        var piled = ChartStack.Pile([[1, 0], [3, 0]], asShares: true);

        Assert.Equal(Bars((0, 0.25), (0, 0)), piled[0]);
        Assert.Equal(Bars((0.25, 1), (0, 0)), piled[1]);
    }

    [Fact]
    public void Pile_ShouldCountAValueBelowZeroAsNothing_WhenAskedForShares()
    {
        var piled = ChartStack.Pile([[-4], [1], [1]], asShares: true);

        Assert.Equal(Bars((0, 0), (0, 0.5), (0.5, 1)), piled.Select(series => series[0]));
    }

    [Fact]
    public void Pile_ShouldBeEmpty_WhenThereAreNoSeries()
    {
        Assert.Empty(ChartStack.Pile([], asShares: false));
    }

    private static (double From, double To)?[] Bars(params (double From, double To)?[] bars) => bars;
}
