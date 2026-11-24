using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class RatingScenarioTests : BunitContext
{
    public RatingScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task RidgeLoop_ShouldGiveTheAppItsStars_WhenRatedAndCleared()
    {
        var cut = Render<RatingExample>();
        var rating = cut.Tb().Rating("Rate the ridge loop");
        Assert.Equal(0, rating.Value);

        await rating.RateAsync(4);
        Assert.Equal(4, rating.Value);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "You gave it 4 of 5.");

        await rating.ClearAsync();
        Assert.Equal(0, rating.Value);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Not rated yet.");
    }
}
