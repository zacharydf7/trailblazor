using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TreeViewScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Trails_ShouldShowOnlyMatchesAndOpenATrail_WhenFilteredAndActivated(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view/filter-and-select");
        var trails = page.Tb().Tree("Trails by region");

        // The filter keeps the rows that match and the regions they sit in.
        await page.Tb().TextField("Find a trail").EnterAsync("cedar");
        Assert.Equal(["River valley", "Cedar Flats"], await trails.TextsAsync());

        // A row is found by the texts along its path. ActivateAsync does what a double click or Enter does.
        await trails.ActivateAsync("River valley", "Cedar Flats");
        await Expect(page.GetByText("Opened Cedar Flats.")).ToBeVisibleAsync();
    }
}
