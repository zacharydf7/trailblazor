using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridPrerenderTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldKeepItsPrerenderedRowsAndAskOnce_WhenItHasAPrerenderKey(string browser)
    {
        var (page, held) = await OpenPrerenderedAsync(browser, "persist=1");

        held.SetResult();

        // The one call is the prerendered one, and the rows never gave way to the loading row.
        Assert.Equal("1", await CallsAsync(page, counted: 1));
        Assert.False(await page.EvaluateAsync<bool>("() => window.sawLoading"));
        await Expect(page.GetByRole(AriaRole.Cell, new() { Name = "Trail 1", Exact = true })).ToBeVisibleAsync();

        // A request that differs from the prerendered one loads as usual.
        await page.Tb().Grid("Trails").GoToPageAsync(2);
        await Expect(page.GetByRole(AriaRole.Cell, new() { Name = "Trail 6", Exact = true })).ToBeVisibleAsync();
        Assert.Equal("2", await CallsAsync(page, counted: 2));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldAskForItsTotalsWithTheRowsInView_WhenAColumnHasAnAggregate(string browser)
    {
        var (page, held) = await OpenPrerenderedAsync(browser, "persist=1&totals=1");

        held.SetResult();

        // The prerendered pass asks before it knows the columns, so the totals take a second call.
        await Expect(page.Locator("[data-part=totals]")).ToContainTextAsync("117");
        Assert.Equal("2", await CallsAsync(page, counted: 1));
        Assert.False(await page.EvaluateAsync<bool>("() => window.sawLoading"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldShowItsPrerenderedRowsWithoutAsking_WhenThePageTurnsInteractiveInWebAssembly(
        string browser)
    {
        var (page, held) = await OpenPrerenderedAsync(browser, "persist=1&mode=webassembly");

        held.SetResult();

        // The rows crossed from the server in the page, so the provider in the browser was never called.
        Assert.Equal("0", await CallsAsync(page, counted: 1));
        Assert.False(await page.EvaluateAsync<bool>("() => window.sawLoading"));
        await Expect(page.GetByRole(AriaRole.Cell, new() { Name = "Trail 1", Exact = true })).ToBeVisibleAsync();
    }

    private Task<(IPage Page, TaskCompletionSource Held)> OpenPrerenderedAsync(string browser, string query) =>
        OpenPrerenderedAsync(browser, "/grid-prerender", query,
            page => page.GetByRole(AriaRole.Cell, new() { Name = "Trail 1", Exact = true }));
}
