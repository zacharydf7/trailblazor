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
    public async Task Interactive_ShouldKeepTheMark_WhenTheFirstPageStreamsItsContent(string browser)
    {
        // Blazor starts the session once the stream ends, so the mark renders after the streamed content.
        var page = await OpenStaticAsync(browser, "/per-page-streamed");
        await Expect(page.GetByText("Streamed trails")).ToBeVisibleAsync();
        page.SetDefaultTimeout(5000);

        await page.Tb().InteractiveAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Interactive_ShouldPassOverTheSessionsMark_WhenItFirstRendersAfterANavigation(string browser)
    {
        var (page, session) = await OpenWithTheSessionHeldAsync(browser, "/static-attempted-plain");
        await page.EvaluateAsync("""
            () => new Promise(loaded => {
                Blazor.addEventListener('enhancedload', loaded, { once: true });
                Blazor.navigateTo('/static-attempted-plain?again=1');
            })
            """);

        session.SetResult();
        await page.Locator("[data-tb-session][data-tb-interactive]").WaitForAsync(
            new() { State = WaitForSelectorState.Attached });
        page.SetDefaultTimeout(2000);

        // TbRoot's mark speaks for the first page only, and this page has no mark of its own.
        await Assert.ThrowsAsync<TimeoutException>(() => page.Tb().InteractiveAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Interactive_ShouldTakeTheRootsMark_WhenRoutesAreInteractiveAndTheSessionStartsAfterANavigation(
        string browser)
    {
        var (page, session) = await OpenWithTheSessionHeldAsync(browser, "/examples/button");
        await page.EvaluateAsync("""
            () => new Promise(loaded => {
                Blazor.addEventListener('enhancedload', loaded, { once: true });
                Blazor.navigateTo('/examples/link');
            })
            """);

        session.SetResult();
        page.SetDefaultTimeout(10000);

        // The whole page runs in the session, so TbRoot's mark speaks for it.
        await page.Tb().InteractiveAsync();
    }

    [Theory]
    [MemberData(nameof(BothOrders))]
    public async Task Interactive_ShouldKeepTheMark_WhenAnEnhancedLoadKeepsTheAddress(string browser, bool lateSession)
    {
        var (page, session) = await OpenWithTheSessionHeldAsync(browser, "/static-attempted-plain");
        if (!lateSession)
        {
            session.SetResult();
            await page.Tb().InteractiveAsync();
        }

        await page.EvaluateAsync("""
            () => new Promise(loaded => {
                Blazor.addEventListener('enhancedload', loaded, { once: true });
                Blazor.navigateTo(location.pathname + location.search);
            })
            """);
        session.TrySetResult();
        page.SetDefaultTimeout(10000);

        // The same page is still the first page, which TbRoot's mark speaks for.
        await page.Tb().InteractiveAsync();
    }

    public static TheoryData<string, bool> BothOrders()
    {
        var data = new TheoryData<string, bool>();
        foreach (var browser in AllBrowsers.Names)
        {
            data.Add(browser, false);
            data.Add(browser, true);
        }

        return data;
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
