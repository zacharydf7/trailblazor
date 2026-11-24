using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DataGridScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Trails_ShouldSortPageAndReportWhatIsChosen_WhenAUserWorksThroughTheGrid(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid/sort-page-and-select");
        var trails = page.Tb().Grid("Trails");
        var chosen = page.GetByLabel("Chosen and planned");

        // The longest trail comes first, and the first page holds five of the seven.
        await trails.SortAsync("Length (km)", descending: true);
        Assert.Equal(5, await trails.RowCountAsync());
        Assert.Equal("Eagle Ridge traverse", (await trails.RowsAsync())[0][0]);

        // SelectAsync finds a row by the exact text of one of its cells.
        await trails.SelectAsync("Eagle Ridge traverse");
        await trails.SelectAsync("Summit Gate");
        await Expect(chosen).ToContainTextAsync("Eagle Ridge traverse");
        await Expect(chosen).ToContainTextAsync("Summit Gate");

        // The selection is kept from page to page.
        await trails.GoToPageAsync(2);
        Assert.Equal(2, await trails.RowCountAsync());
        await Expect(chosen).ToContainTextAsync("Summit Gate");
    }
}
