using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class VirtualListScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Reports_ShouldHoldEveryReport_WhenOnlySomeAreDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/virtual-list/items");
        var reports = page.Tb().VirtualList("Trail reports");

        // CountAsync is every item the list holds, drawn or not.
        Assert.Equal(5000, await reports.CountAsync());

        // ItemAsync scrolls the list to an item, as a user does, and waits until it is drawn.
        var last = await reports.ItemAsync(4999);
        await Expect(last).ToContainTextAsync("Report 5000: Trail 1");
        await Expect(last).ToBeInViewportAsync();
    }
}
