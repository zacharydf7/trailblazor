namespace Trailblazor.BrowserTests;

public class VirtualListTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Reports(IPage page) => page.GetByRole(AriaRole.Feed, new() { Name = "Trail reports" });

    private static ILocator Report(IPage page, int number) =>
        Reports(page).GetByRole(AriaRole.Article, new() { Name = $"Report {number}: Trail {number % 40 + 1}" });

    // The lists' keys work once their script has attached, which data-ready marks.
    private async Task<IPage> OpenListsAsync(string browser)
    {
        var page = await OpenAsync(browser, "/examples/virtual-list");
        await Expect(page.Locator("[role=feed]:not([data-ready])")).ToHaveCountAsync(0);
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualList_ShouldMoveBetweenItemsOfAnyHeight_WhenPageDownAndPageUpArePressed(string browser)
    {
        var page = await OpenListsAsync(browser);
        await Report(page, 1).FocusAsync();

        for (var i = 0; i < 12; i++)
        {
            await page.Keyboard.PressAsync("PageDown");
        }

        await Expect(Report(page, 13)).ToBeFocusedAsync();
        await Expect(Report(page, 13)).ToBeInViewportAsync();
        await Expect(Report(page, 13)).ToHaveAttributeAsync("aria-posinset", "13");
        await page.Keyboard.PressAsync("PageUp");
        await Expect(Report(page, 12)).ToBeFocusedAsync();
        await Expect(Reports(page).Locator("[tabindex='0']")).ToHaveCountAsync(1);
        await Expect(Report(page, 12)).ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualList_ShouldLeaveTheFeed_WhenCtrlEndOrCtrlHomeIsPressed(string browser)
    {
        var page = await OpenListsAsync(browser);
        var goTo = page.GetByRole(AriaRole.Button, new() { Name = "Go to sighting 751" });
        var sightings = page.GetByRole(AriaRole.Feed, new() { Name = "Wildlife sightings" });
        await Report(page, 3).FocusAsync();

        await page.Keyboard.PressAsync("Control+End");
        await Expect(goTo).ToBeFocusedAsync();
        await sightings.GetByRole(AriaRole.Article).First.FocusAsync();
        await page.Keyboard.PressAsync("Control+Home");

        await Expect(goTo).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ScrollToAsync_ShouldDrawAndFocusAFarItem_WhenCalled(string browser)
    {
        var page = await OpenListsAsync(browser);
        var sightings = page.GetByRole(AriaRole.Feed, new() { Name = "Wildlife sightings" });

        // The list scrolls to an item once the provider has given the total.
        await Expect(sightings).ToHaveAttributeAsync("data-count", "2000");

        await page.GetByRole(AriaRole.Button, new() { Name = "Go to sighting 751" }).ClickAsync();

        var target = sightings.GetByRole(AriaRole.Article).Filter(new() { HasText = "Sighting 751:" });
        await Expect(target).ToBeFocusedAsync();
        await Expect(target).ToBeInViewportAsync();
        await Expect(sightings).ToHaveAttributeAsync("aria-busy", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualList_ShouldKeepTheKeysWorking_WhenTheFocusedItemScrollsOutOfTheList(string browser)
    {
        var page = await OpenListsAsync(browser);
        await Report(page, 2).FocusAsync();

        await Reports(page).EvaluateAsync("feed => feed.scrollTop = 40000");
        await Expect(Report(page, 2)).ToHaveCountAsync(0);
        await Expect(Reports(page)).ToBeFocusedAsync();
        await Reports(page).EvaluateAsync("feed => feed.scrollTop = 0");
        await page.Keyboard.PressAsync("PageDown");

        await Expect(Report(page, 3)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualList_ShouldLeaveTheKeysAndTabStopsOfItsItemsControls_WhenTheyHaveFocus(string browser)
    {
        var page = await OpenAsync(browser, "/virtual-list");
        var posts = page.GetByRole(AriaRole.Feed, new() { Name = "Posts" });
        await Expect(posts).ToHaveAttributeAsync("data-ready", "");
        var reply = posts.GetByRole(AriaRole.Textbox, new() { Name = "Reply to Post 2" });
        await reply.FillAsync("Great walk");

        await page.Keyboard.PressAsync("Control+Home");
        await page.Keyboard.PressAsync("PageDown");
        await Expect(reply).ToBeFocusedAsync();
        await posts.GetByRole(AriaRole.Group, new() { Name = "Reaction to Post 2" })
            .GetByRole(AriaRole.Button, new() { Name = "Like", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("PageDown");

        await Expect(posts.GetByRole(AriaRole.Article, new() { Name = "Post 3" })).ToBeFocusedAsync();
        await posts.GetByRole(AriaRole.Checkbox, new() { Name = "Mark Post 3 read" }).FocusAsync();
        await page.Keyboard.PressAsync("PageDown");
        await Expect(posts.GetByRole(AriaRole.Article, new() { Name = "Post 4" })).ToBeFocusedAsync();
        var likeStops = await posts.GetByRole(AriaRole.Group, new() { Name = "Reaction to Post 1", Exact = true })
            .Locator("[tabindex='0']").CountAsync();
        Assert.Equal(1, likeStops);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualList_ShouldShowEveryItem_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-virtual-list");

        var reports = page.GetByRole(AriaRole.Feed, new() { Name = "Trail reports" });
        await Expect(reports.GetByRole(AriaRole.Article)).ToHaveCountAsync(500);
        await Expect(reports.GetByRole(AriaRole.Article).Last).ToHaveAttributeAsync("aria-setsize", "500");
    }
}
