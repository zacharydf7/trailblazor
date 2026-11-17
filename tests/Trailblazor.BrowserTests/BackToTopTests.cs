using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class BackToTopTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // A long article whose page scrolls as a whole.
    private const string Page = "/back-to-top-page";

    private static Task ScrollToAsync(IPage page, int top) => page.EvaluateAsync("top => window.scrollTo(0, top)", top);

    private static async Task<TbBackToTopLocator> ReadyAsync(IPage page)
    {
        var top = page.Tb().BackToTop();
        await Expect(top.BackToTop).ToHaveAttributeAsync("data-ready", "");
        return top;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task BackToTop_ShouldShowOnlyOnceThePageHasScrolledDown_WhenScrolled(string browser)
    {
        var page = await OpenAsync(browser, Page);
        var top = await ReadyAsync(page);
        await Expect(top.Button).ToBeHiddenAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(top.Button).Not.ToBeFocusedAsync();

        await ScrollToAsync(page, 2000);
        await Expect(top.Button).ToBeVisibleAsync();
        await ScrollToAsync(page, 0);

        await Expect(top.Button).ToBeHiddenAsync();
        Assert.False(await top.IsShownAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task BackToTop_ShouldScrollUpAndMoveFocusThere_WhenPressedWithTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, Page);
        var top = await ReadyAsync(page);
        await ScrollToAsync(page, 2000);
        await Expect(top.Button).ToBeVisibleAsync();

        await top.Button.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await page.WaitForFunctionAsync("() => window.scrollY === 0");
        await Expect(page.Locator("#page-title")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task BackToTop_ShouldJumpToTheTop_WhenReducedMotionIsPreferred(string browser)
    {
        var page = await OpenAsync(browser, Page, new() { ReducedMotion = ReducedMotion.Reduce });
        var top = await ReadyAsync(page);
        await ScrollToAsync(page, 2000);

        await top.ClickAsync();

        Assert.Equal(0, await page.EvaluateAsync<double>("() => window.scrollY"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task BackToTop_ShouldFollowAndScrollTheContainer_WhenGivenOne(string browser)
    {
        var page = await OpenAsync(browser, "/examples/back-to-top");
        var top = await ReadyAsync(page);
        var log = page.Locator("#trail-log");
        await Expect(top.Button).ToBeHiddenAsync();

        await log.EvaluateAsync("log => log.scrollTo(0, 2000)");
        await top.ClickAsync();

        await page.WaitForFunctionAsync("() => document.querySelector('#trail-log').scrollTop === 0");
        await Expect(page.Locator("#trail-log-title")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task BackToTop_ShouldFollowAContainerThatRendersLater_WhenGivenOne(string browser)
    {
        var page = await OpenAsync(browser, "/back-to-top-late");
        var top = await ReadyAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Load the log" }).ClickAsync();
        await page.Locator("#late-log").EvaluateAsync("log => log.scrollTo(0, 2000)");

        await Expect(top.Button).ToBeVisibleAsync();
    }
}
