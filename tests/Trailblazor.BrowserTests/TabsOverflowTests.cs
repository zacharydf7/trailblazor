namespace Trailblazor.BrowserTests;

public class TabsOverflowTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldFadeTheSideWithMoreTabs_WhenTheRowScrollsBesideOtherContent(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");
        // Beside Start and End content, and on its own, the row is too narrow for its tabs at this width.
        foreach (var name in new[] { "Trail report", "Months" })
        {
            var list = page.GetByRole(AriaRole.Tablist, new() { Name = name, Exact = true });
            await Expect(list).ToHaveAttributeAsync("data-ready", "");
            // WebKit tells a row of its new size only once the row is in view.
            await list.ScrollIntoViewIfNeededAsync();
            // A scrollbar that takes room shows that the row scrolls, so the row is given none, as on a phone.
            await list.EvaluateAsync("""
                list => {
                    list.parentElement.style.inlineSize = '18rem';
                    list.style.scrollbarWidth = 'none';
                    list.scrollLeft = 0;
                }
                """);

            await Expect(list).ToHaveAttributeAsync("data-more-right", "");
            await Expect(list).Not.ToHaveAttributeAsync("data-more-left", "");
            Assert.StartsWith("linear-gradient(to left",
                await list.EvaluateAsync<string>("list => getComputedStyle(list).maskImage"));

            await list.EvaluateAsync("list => list.scrollLeft = list.scrollWidth");
            await Expect(list).ToHaveAttributeAsync("data-more-left", "");
            await Expect(list).Not.ToHaveAttributeAsync("data-more-right", "");
        }

        // A tab the row brings into view rests clear of the fade, which is 2rem wide, and the row's line stays whole.
        var months = page.GetByRole(AriaRole.Tablist, new() { Name = "Months", Exact = true });
        await months.EvaluateAsync("list => list.scrollLeft = 0");
        await Expect(months).Not.ToHaveAttributeAsync("data-more-left", "");
        var at = await months.EvaluateAsync<int>("""
            list => [...list.children].findIndex(
                tab => tab.getBoundingClientRect().right > list.getBoundingClientRect().right)
            """);
        var cut = months.GetByRole(AriaRole.Tab).Nth(at);
        // The arrow keys reach the first tab the row cuts off, which a click would scroll to by itself.
        await months.GetByRole(AriaRole.Tab).First.ClickAsync();
        for (var step = 0; step < at; step++)
        {
            await page.Keyboard.PressAsync("ArrowRight");
        }

        await Expect(cut).ToHaveAttributeAsync("aria-selected", "true");
        var (tab, row) = ((await cut.BoundingBoxAsync())!, (await months.BoundingBoxAsync())!);
        // Firefox brings a focused tab further in than the 2rem the others stop at.
        Assert.True(row.X + row.Width - tab.X - tab.Width >= 31.5, "The tab rests under the fade.");
        await Expect(months).ToHaveCSSAsync("mask-size", "auto, 100% 1px");
    }

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
