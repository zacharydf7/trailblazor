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

    // The page shows the count it had when it last drew, so the count is read once this click's draw has landed.
    private static async Task<string> CallsAsync(IPage page, int counted)
    {
        await page.Tb().Button("Count").ClickAsync();
        await Expect(page.GetByTestId("counted")).ToHaveTextAsync($"{counted}");
        return (await page.GetByTestId("calls").TextContentAsync())!;
    }

    // Opens the grid's page under a new run id, so the call count is this test's alone, holds it before it turns
    // interactive, and watches for the loading row from then on.
    private async Task<(IPage Page, TaskCompletionSource Held)> OpenPrerenderedAsync(string browser, string query)
    {
        var (page, held) = await OpenHeldAsync(browser, "/grid-prerender");
        await page.GotoAsync(new Uri(new Uri(page.Url), $"/grid-prerender?run={Guid.NewGuid():N}&{query}").ToString(),
            new() { WaitUntil = WaitUntilState.Commit });
        await Expect(page.GetByRole(AriaRole.Cell, new() { Name = "Trail 1", Exact = true })).ToBeVisibleAsync();
        await page.EvaluateAsync("""
            () => {
                window.sawLoading = false;
                new MutationObserver(() => window.sawLoading ||= document.querySelector('[data-part=loading]') !== null)
                    .observe(document.documentElement, { childList: true, subtree: true });
            }
            """);
        return (page, held);
    }
}
