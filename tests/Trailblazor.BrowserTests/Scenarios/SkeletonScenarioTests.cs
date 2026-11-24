using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SkeletonScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Trip_ShouldShowAndBeUsable_WhenLoadingEnds(string browser)
    {
        var page = await OpenAsync(browser, "/examples/skeleton/wrapping-content");
        var trip = page.GetByRole(AriaRole.Region, new() { Name = "Trip" });
        var title = trip.GetByText("Ridge loop");
        // While it loads, the region is busy and its content is in the page but hidden.
        await Expect(trip).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(title).ToBeAttachedAsync();
        await Expect(title).ToBeHiddenAsync();

        // In an app, loading ends when the data comes; here the example's switch ends it.
        await page.Tb().Switch("Loading the trip").UncheckAsync();

        await Expect(trip).Not.ToHaveAttributeAsync("aria-busy", "true");
        await Expect(title).ToBeVisibleAsync();
    }
}
