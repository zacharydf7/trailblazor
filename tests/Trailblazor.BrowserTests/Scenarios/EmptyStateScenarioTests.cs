using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class EmptyStateScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NoTrails_ShouldOpenTheTrailFinder_WhenItsActionIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/empty-state/basic");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "No trails saved yet" })).ToBeVisibleAsync();

        // The empty state has no helper of its own; its action is a TbButton, found by its name.
        await page.Tb().Button("Find a trail").ClickAsync();

        await Expect(page.Locator("output")).ToHaveTextAsync("Opened the trail finder.");
    }
}
