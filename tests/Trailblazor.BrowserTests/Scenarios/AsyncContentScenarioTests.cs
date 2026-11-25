using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AsyncContentScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailReports_ShouldShow_WhenRetriedAfterTheFirstLoadFails(string browser)
    {
        var page = await OpenAsync(browser, "/examples/async-content/failure");
        var reports = page.Tb().AsyncContent("trail reports");
        await reports.WaitUntilLoadedAsync();
        Assert.Equal(AsyncContentState.Failed, await reports.StateAsync());

        // Retry returns once the load it starts has finished.
        await reports.RetryAsync();

        Assert.Equal(AsyncContentState.Content, await reports.StateAsync());
        await Expect(page.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
    }
}
