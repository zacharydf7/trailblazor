using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class BackToTopScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailLog_ShouldGoBackToItsTop_WhenTheButtonIsPressedAfterScrolling(string browser)
    {
        var page = await OpenAsync(browser, "/examples/back-to-top/basic");

        // The button shows once the log has scrolled past its Threshold of 200 pixels.
        await page.GetByText("Day 60:").ScrollIntoViewIfNeededAsync();
        // ClickAsync waits until the button shows, then presses it, which scrolls the log up and focuses its title.
        await page.Tb().BackToTop().ClickAsync();

        await Expect(page.GetByText("Day 1:")).ToBeInViewportAsync();
        await Expect(page.GetByText("Trail log", new() { Exact = true })).ToBeFocusedAsync();
    }
}
