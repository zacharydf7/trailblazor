using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TextScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Headings_ShouldBeTheTextGivenAHeadingElement_WhenRead(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text/elements");

        // Text has no helper of its own. Its look doesn't make it a heading; its As does.
        var heading = new PageGetByRoleOptions { Name = "A small heading that's still a heading", Level = 3 };
        await Expect(page.GetByRole(AriaRole.Heading, heading)).ToBeVisibleAsync();
        await Expect(page.GetByText("Large text that isn't a heading")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Large text that isn't a heading" }))
            .ToHaveCountAsync(0);
    }
}
