using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DockManagerScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task HikePlanner_ShouldShowTheChosenPanels_WhenATabIsSelectedAndAnotherClosed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dock-manager/layout");
        var planner = page.Tb().Dock("Hike planner");
        // ShowingAsync reads the panel each group shows, in order.
        Assert.Equal(["Map", "Notes", "Weather"], await planner.ShowingAsync());

        // SelectAsync and CloseAsync act on a panel by its tab's title, and wait until the layout has changed.
        await planner.SelectAsync("Trails");
        await planner.CloseAsync("Weather");

        Assert.Equal(["Trails", "Notes"], await planner.ShowingAsync());
        Assert.Equal(["Trails", "Map", "Notes"], await planner.PanelsAsync());
    }
}
