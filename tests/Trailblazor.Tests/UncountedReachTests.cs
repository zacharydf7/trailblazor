namespace Trailblazor.Tests;

public class UncountedReachTests
{
    [Fact]
    public void Take_ShouldEndTheItemsWhereAnEmptyPageStarts_WhenThePageBeforeFilledUpToIt()
    {
        var reach = new UncountedReach();
        reach.Take(0, 20, hasMore: true);

        reach.Take(20, 0, hasMore: false);

        Assert.Equal((20, true, 20), (reach.Reached, reach.Ended, reach.Room(20)));
    }

    [Fact]
    public void Take_ShouldLearnNothing_WhenAnEmptyPageStartsPastTheItemsReached()
    {
        var reach = new UncountedReach();
        reach.Take(0, 20, hasMore: true);

        reach.Take(40, 0, hasMore: false);

        Assert.Equal((20, false, 30), (reach.Reached, reach.Ended, reach.Room(10)));
    }

    [Fact]
    public void Take_ShouldTakeBackTheEnd_WhenALaterPageReachesPastIt()
    {
        var reach = new UncountedReach();
        reach.Take(0, 5, hasMore: false);

        reach.Take(0, 10, hasMore: true);

        Assert.Equal((10, false), (reach.Reached, reach.Ended));
    }

    [Fact]
    public void Take_ShouldKeepTheEnd_WhenAnEarlierPageSaysMoreFollow()
    {
        var reach = new UncountedReach();
        reach.Take(0, 10, hasMore: true);
        reach.Take(10, 5, hasMore: false);

        reach.Take(0, 10, hasMore: true);

        Assert.Equal((15, true, 15), (reach.Reached, reach.Ended, reach.Room(10)));
    }
}
