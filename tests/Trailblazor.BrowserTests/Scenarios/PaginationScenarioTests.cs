using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class PaginationScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Reports_ShouldGiveTheAppThePage_WhenAnotherPageIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination/pages");
        var reports = page.Tb().Pagination("Trail reports");
        Assert.Equal(1, await reports.PageAsync());

        // GoToPageAsync clicks its way there as a user would: the page's own button, or the nearest page shown on
        // the way to it. Page 7 isn't shown from page 1, so the app hears of pages 5 and 6 on the way.
        await reports.GoToPageAsync(7);

        Assert.Equal(7, await reports.PageAsync());
        await Expect(page.GetByText("Showing report page 7 of 20")).ToBeVisibleAsync();
    }
}
