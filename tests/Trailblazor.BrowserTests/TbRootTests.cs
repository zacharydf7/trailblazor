using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TbRootTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TbRoot_ShouldShowWhatThePageOpens_WhenOnlyThePageIsInteractive(string browser)
    {
        var page = await OpenStaticAsync(browser, "/per-page");
        // Only the buttons are interactive, so the layout's main never gets the attribute.
        await page.Locator("main [data-interactive]").WaitForAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Toast" }).ClickAsync();
        await Expect(page.Locator("[data-part=toast]")).ToContainTextAsync("Saved");
        await page.GetByRole(AriaRole.Button, new() { Name = "Announce" }).ClickAsync();
        await Expect(page.Locator("[data-part=announcer] [role=alert]")).ToHaveTextAsync("Connection lost");
        await page.GetByRole(AriaRole.Button, new() { Name = "Dialog" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Toasting" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Interactive_ShouldClearTheMark_WhenEnhancedNavigationReachesAStaticPage(string browser)
    {
        var page = await OpenStaticAsync(browser, "/per-page");
        await page.Tb().InteractiveAsync();

        await page.EvaluateAsync("() => Blazor.navigateTo('/static-attempted-plain')");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Send" })).ToBeVisibleAsync();

        await Expect(page.Locator("[data-tb-interactive]")).ToHaveCountAsync(0);
    }
}
