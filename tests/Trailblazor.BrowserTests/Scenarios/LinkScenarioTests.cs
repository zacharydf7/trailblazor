using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class LinkScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task BatchLink_ShouldShowTheBatchId_WhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/link/button");
        await Expect(page.GetByText("2041-B")).ToHaveCountAsync(0);

        // A link with no address runs the app's handler, and ClickAsync waits for it.
        await page.Tb().Link("Show the batch ID").ClickAsync();

        await Expect(page.GetByText("Batch 2041-B")).ToBeVisibleAsync();
    }
}
