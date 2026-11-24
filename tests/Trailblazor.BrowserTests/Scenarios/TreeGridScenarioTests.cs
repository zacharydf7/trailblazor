using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TreeGridScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Trails_ShouldShowAMatchUnderItsParkAndRegion_WhenFilteredByName(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-grid/nested-rows");
        var trails = page.Tb().Grid("Trails by region");
        Assert.Equal(["North ridge", "South valley"], (await trails.RowsAsync()).Select(row => row[0]));

        // A filter opens the rows a match is nested under, so it shows where the trail is.
        await trails.FilterAsync("Name", "Eagle");
        Assert.Equal(["North ridge", "Pine park", "Eagle Ridge"],
            (await trails.RowsAsync()).Select(row => row[0]));

        // ExpandAsync opens a row, found by the text of one of its cells.
        await trails.FilterAsync("Name", "");
        await trails.ExpandAsync("South valley");
        Assert.Equal(["North ridge", "South valley", "River park"],
            (await trails.RowsAsync()).Select(row => row[0]));
    }
}
