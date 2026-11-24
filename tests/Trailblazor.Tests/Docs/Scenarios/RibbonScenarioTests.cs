using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class RibbonScenarioTests : BunitContext
{
    public RibbonScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailPlanner_ShouldRunACommandOnAnotherTab_WhenTheTabIsSelected()
    {
        var cut = Render<RibbonExample>();
        var planner = cut.Tb().Ribbon("Trail planner");
        Assert.Equal("Notes", planner.Selected);

        // SelectAsync shows another tab's commands, and ClickAsync presses one by its label.
        await planner.SelectAsync("Trails");
        await planner.ClickAsync("Add a trail");

        Assert.Equal("Trails", planner.Selected);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent.StartsWith("Last command: Add a trail."));
    }
}
