using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ToggleGroupScenarioTests : BunitContext
{
    public ToggleGroupScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Seasons_ShouldGiveTheAppThePressedSeasons_WhenSeasonsAreTurnedOnAndOff()
    {
        var cut = Render<ToggleGroupMultipleExample>();
        var seasons = cut.Tb().ToggleGroup("Seasons open");
        Assert.Equal(["Summer"], seasons.Chosen);

        // ChooseAsync presses the items that aren't pressed yet, and UnchooseAsync the ones that are.
        await seasons.ChooseAsync("Spring", "Fall");
        Assert.Equal(["Spring", "Summer", "Fall"], seasons.Chosen);

        await seasons.UnchooseAsync("Summer");
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Open in: Spring, Fall");
    }
}
