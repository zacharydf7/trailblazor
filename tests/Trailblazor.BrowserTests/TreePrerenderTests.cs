using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TreePrerenderTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldKeepItsPrerenderedChildrenAndLoadOnce_WhenItHasAPrerenderKey(string browser)
    {
        var (page, held) = await OpenPrerenderedAsync(browser, "persist=1");

        held.SetResult();

        // The one load is the prerendered one, and the children never gave way to the loading row.
        Assert.Equal("1", await CallsAsync(page, counted: 1));
        Assert.False(await page.EvaluateAsync<bool>("() => window.sawLoading"));
        await Expect(Row(page, "North ridge loop")).ToBeVisibleAsync();

        // A row opened after that loads as usual.
        await Row(page, "River valley").Locator("[data-part=toggle]").ClickAsync();
        await Expect(Row(page, "River valley spur")).ToBeVisibleAsync();
        Assert.Equal("2", await CallsAsync(page, counted: 2));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldShowItsPrerenderedChildrenWithoutLoading_WhenThePageTurnsInteractiveInWebAssembly(
        string browser)
    {
        var (page, held) = await OpenPrerenderedAsync(browser, "persist=1&mode=webassembly");

        held.SetResult();

        // The children crossed from the server in the page, so the loader in the browser was never called.
        Assert.Equal("0", await CallsAsync(page, counted: 1));
        Assert.False(await page.EvaluateAsync<bool>("() => window.sawLoading"));
        await Expect(Row(page, "North ridge spur")).ToBeVisibleAsync();
    }

    private static ILocator Row(IPage page, string name) =>
        page.GetByRole(AriaRole.Treeitem, new() { Name = name, Exact = true });

    private Task<(IPage Page, TaskCompletionSource Held)> OpenPrerenderedAsync(string browser, string query) =>
        OpenPrerenderedAsync(browser, "/tree-prerender", query, page => Row(page, "North ridge loop"));
}
