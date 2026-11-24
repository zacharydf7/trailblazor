using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DockManagerScenarioTests : BunitContext
{
    public DockManagerScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task HikePlanner_ShouldShowTheChosenPanels_WhenATabIsSelectedAndAnotherClosed()
    {
        var cut = Render<DockManagerExample>();
        var planner = cut.Tb().Dock("Hike planner");
        // Showing is the panel each group shows, in order.
        Assert.Equal(["Map", "Notes", "Weather"], planner.Showing);

        // SelectAsync and CloseAsync act on a panel by its tab's title.
        await planner.SelectAsync("Trails");
        await planner.CloseAsync("Weather");

        Assert.Equal(["Trails", "Notes"], planner.Showing);
        Assert.Equal(["Trails", "Map", "Notes"], planner.Panels);
    }
}
