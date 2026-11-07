namespace Trailblazor.BrowserTests;

public class TabsOverflowTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-overflow");
        foreach (var list in await page.GetByRole(AriaRole.Tablist).AllAsync())
        {
            await Expect(list).ToHaveAttributeAsync("data-ready", "");
        }

        return page;
    }

    private static ILocator Tab(IPage page, string name) =>
        page.GetByRole(AriaRole.Tab, new() { Name = name, Exact = true });

    private static Task PressAsync(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true }).ClickAsync();

    // Waits for the tab to sit wholly inside its row, however the row got scrolled.
    private static async Task WaitUntilShownAsync(ILocator tab) => await tab.Page.WaitForFunctionAsync("""
        tab => {
            const own = tab.getBoundingClientRect();
            const row = tab.parentElement.getBoundingClientRect();
            return own.left >= row.left - 1 && own.right <= row.right + 1;
        }
        """, await tab.ElementHandleAsync(), new() { Timeout = 5000 });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowTheSelectedTab_WhenItStartsOutOfSight(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await Expect(Tab(page, "Wide 9")).ToHaveAttributeAsync("aria-selected", "true");
        await WaitUntilShownAsync(Tab(page, "Wide 9"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowTheSelectedTabAndLeaveThePageAlone_WhenSelectedFromCode(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await PressAsync(page, "Select the last");
        await Expect(Tab(page, "Wide 12")).ToHaveAttributeAsync("aria-selected", "true");
        await WaitUntilShownAsync(Tab(page, "Wide 12"));
        await PressAsync(page, "Select the first");
        await Expect(Tab(page, "Wide 1")).ToHaveAttributeAsync("aria-selected", "true");
        await WaitUntilShownAsync(Tab(page, "Wide 1"));

        Assert.Equal(0, await page.EvaluateAsync<double>("() => window.scrollY"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowAllOfATab_WhenAPartlyHiddenOneIsClicked(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var tab = Tab(page, "Wide 1");
        await tab.Locator("xpath=..").EvaluateAsync("""
            row => {
                row.scrollIntoView({ block: 'center' });
                row.scrollLeft = row.firstElementChild.getBoundingClientRect().width / 2;
            }
            """);
        var box = (await tab.BoundingBoxAsync())!;

        // The tab's end is the part still in sight.
        await page.Mouse.ClickAsync(box.X + box.Width - 5, box.Y + box.Height / 2);
        await Expect(tab).ToHaveAttributeAsync("aria-selected", "true");

        await WaitUntilShownAsync(tab);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowTheSelectedTab_WhenTheTextIsRightToLeft(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await PressAsync(page, "Select the last flipped");

        await Expect(Tab(page, "Flipped 12")).ToHaveAttributeAsync("aria-selected", "true");
        await WaitUntilShownAsync(Tab(page, "Flipped 12"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowTheSelectedTab_WhenTheTabsLoadLate(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await PressAsync(page, "Load the tabs");

        await Expect(Tab(page, "Late 9")).ToHaveAttributeAsync("aria-selected", "true");
        await WaitUntilShownAsync(Tab(page, "Late 9"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowTheSelectedTab_WhenItsRowIsShownLater(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await Tab(page, "Outer two").ClickAsync();

        await Expect(Tab(page, "Inner 9")).ToHaveAttributeAsync("aria-selected", "true");
        await WaitUntilShownAsync(Tab(page, "Inner 9"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowTheSelectedTab_WhenItMovesToItsPlaceInTheRow(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var tab = Tab(page, "Growing new");

        await PressAsync(page, "Add and select a first tab");

        await Expect(tab).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(tab.Locator("xpath=preceding-sibling::*[@role='tab']")).ToHaveCountAsync(0);
        await WaitUntilShownAsync(tab);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowATabAgain_WhenItIsSelectedAgainAfterNothingWasSelected(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var tab = Tab(page, "Keyed 12");
        await WaitUntilShownAsync(tab);

        await PressAsync(page, "Select no keyed tab");
        await Expect(tab).ToHaveAttributeAsync("aria-selected", "false");
        await tab.Locator("xpath=..").EvaluateAsync("row => row.scrollLeft = 0");
        await PressAsync(page, "Select the last keyed tab");

        await Expect(tab).ToHaveAttributeAsync("aria-selected", "true");
        await WaitUntilShownAsync(tab);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldReachTheFirstTab_WhenACenteredRowOverflows(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await WaitUntilShownAsync(Tab(page, "Centered 12"));

        await PressAsync(page, "Select the first centered");

        await Expect(Tab(page, "Centered 1")).ToHaveAttributeAsync("aria-selected", "true");
        await WaitUntilShownAsync(Tab(page, "Centered 1"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowATabsStart_WhenItIsWiderThanItsRow(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var tab = Tab(page, "A tab whose label is far wider than the row it sits in");

        await PressAsync(page, "Select the long tab");
        await Expect(tab).ToHaveAttributeAsync("aria-selected", "true");

        await page.WaitForFunctionAsync("""
            tab => Math.abs(tab.getBoundingClientRect().left - tab.parentElement.getBoundingClientRect().left) <= 1
            """, await tab.ElementHandleAsync(), new() { Timeout = 5000 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowATabsStart_WhenItIsWiderThanItsRowInRightToLeftText(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var tab = Tab(page, "A flipped tab whose label is far wider than its row");

        await PressAsync(page, "Select the long flipped tab");
        await Expect(tab).ToHaveAttributeAsync("aria-selected", "true");

        await page.WaitForFunctionAsync("""
            tab => Math.abs(tab.getBoundingClientRect().right - tab.parentElement.getBoundingClientRect().right) <= 1
            """, await tab.ElementHandleAsync(), new() { Timeout = 5000 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldLeaveAScrolledRowAlone_WhenATabIsAdded(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var row = page.GetByRole(AriaRole.Tablist, new() { Name = "Wide" });
        await WaitUntilShownAsync(Tab(page, "Wide 9"));
        await row.EvaluateAsync("row => row.scrollLeft = 0");

        await PressAsync(page, "Add a tab");
        await Expect(Tab(page, "Wide extra")).ToBeAttachedAsync();
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");

        Assert.Equal(0, await row.EvaluateAsync<double>("row => row.scrollLeft"));
    }
}
