using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class RibbonScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailPlanner_ShouldRunACommandOnAnotherTab_WhenTheTabIsSelected(string browser)
    {
        var page = await OpenAsync(browser, "/examples/ribbon/basic");
        var planner = page.Tb().Ribbon("Trail planner");
        Assert.Equal("Notes", await planner.SelectedAsync());

        // SelectAsync shows another tab's commands, and ClickAsync presses one by its label.
        await planner.SelectAsync("Trails");
        await planner.ClickAsync("Add a trail");

        Assert.Equal("Trails", await planner.SelectedAsync());
        await Expect(page.GetByText("Last command: Add a trail.")).ToBeVisibleAsync();
    }
}
