using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DescriptionListScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RemovedTrail_ShouldShowWhoRemovedItAndWhy_WhenRendered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/description-list/basic");

        var details = page.Tb().DescriptionList();

        Assert.Equal("Robin Hale", await details.ValueOfAsync("Removed by"));
        Assert.StartsWith("Merged into the Cedar Flats loop", await details.ValueOfAsync("Note"));
    }
}
