using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SparklineScenarioTests : BunitContext
{
    public SparklineScenarioTests() => this.AddTrailblazor();

    [Fact]
    public void HikersByTrail_ShouldHaveASparklineNamedForItsRow_WhenTheTableIsShown()
    {
        var cut = Render<SparklineTableExample>();

        // A sparkline is a picture, found by its role and its name, which says what its shape shows.
        var picture = cut.Find("svg[role=img][aria-label='Ridge loop by day, busiest at the weekend']");

        // It has a bar for each day. Test the values the app gives it, not the shape it draws.
        Assert.Equal(7, picture.QuerySelectorAll("rect").Length);
    }
}
