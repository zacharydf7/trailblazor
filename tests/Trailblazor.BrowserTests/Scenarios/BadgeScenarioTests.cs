using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class BadgeScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Inbox_ShouldHideItsCount_WhenEverythingIsMarkedRead(string browser)
    {
        var page = await OpenAsync(browser, "/examples/badge/visible");
        var badge = page.Locator("[data-part=badge]");
        // The badge is hidden from screen readers, so the button's AriaLabel says the count.
        await Expect(badge).ToHaveTextAsync("5");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Inbox, 5 unread" })).ToBeVisibleAsync();

        await page.Tb().Button("Mark all read").ClickAsync();

        await Expect(badge).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Inbox", Exact = true })).ToBeVisibleAsync();
    }
}
