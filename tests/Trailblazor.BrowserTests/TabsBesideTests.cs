namespace Trailblazor.BrowserTests;

public class TabsBesideTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Tabs(IPage page, string name) =>
        page.GetByRole(AriaRole.Tablist, new() { Name = name }).Locator("xpath=..");

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator) =>
        (await locator.BoundingBoxAsync())!;

    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-beside");
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Report" })).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldPutStartBeforeAndEndAfterTheTabsInOneRow_WhenGivenBoth(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var tabs = Tabs(page, "Report");

        var whole = await BoxAsync(tabs);
        var start = await BoxAsync(tabs.GetByRole(AriaRole.Button, new() { Name = "Menu" }));
        var list = await BoxAsync(tabs.GetByRole(AriaRole.Tablist));
        var end = await BoxAsync(tabs.GetByRole(AriaRole.Button, new() { Name = "Share" }));
        var panel = await BoxAsync(tabs.GetByRole(AriaRole.Tabpanel));

        Assert.True(start.X + start.Width <= list.X + 0.5, "Start isn't before the tabs.");
        Assert.True(end.X >= list.X + list.Width - 0.5, "End isn't after the tabs.");
        Assert.InRange(start.Y + start.Height / 2, list.Y, list.Y + list.Height);
        Assert.InRange(end.Y + end.Height / 2, list.Y, list.Y + list.Height);
        // The panel takes the whole width below the row.
        Assert.True(panel.Y >= list.Y + list.Height - 0.5, "The panel isn't under the row.");
        Assert.Equal(whole.Width, panel.Width, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepStartAndEndInViewAtTheRowsEnds_WhenTheListScrollsBetweenThem(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var tabs = Tabs(page, "Narrow");
        var list = tabs.GetByRole(AriaRole.Tablist);
        await Expect(tabs.Locator(".tb-tabs__scroll--end")).ToBeVisibleAsync();

        var whole = await BoxAsync(tabs);
        var start = await BoxAsync(tabs.GetByRole(AriaRole.Button, new() { Name = "Menu" }));
        var row = await BoxAsync(list);
        var add = await BoxAsync(tabs.Locator("[data-part=add]"));
        var end = await BoxAsync(tabs.GetByRole(AriaRole.Button, new() { Name = "Share" }));

        Assert.True(await list.EvaluateAsync<bool>("list => list.scrollWidth > list.clientWidth"), "The list fits.");
        Assert.True(start.X >= whole.X - 0.5 && start.X + start.Width <= row.X + 0.5, "Start isn't before the list.");
        Assert.True(add.X >= row.X + row.Width - 0.5, "The add button isn't after the list.");
        Assert.True(end.X >= add.X + add.Width - 0.5, "End isn't after the add button.");
        Assert.True(end.X + end.Width <= whole.X + whole.Width + 0.5, "End is cut off.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldPutStartAboveAndEndUnderTheTabsBesideThePanel_WhenVertical(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var tabs = Tabs(page, "Column");

        var start = await BoxAsync(tabs.GetByRole(AriaRole.Button, new() { Name = "Menu" }));
        var list = await BoxAsync(tabs.GetByRole(AriaRole.Tablist));
        var add = await BoxAsync(tabs.Locator("[data-part=add]"));
        var end = await BoxAsync(tabs.GetByRole(AriaRole.Button, new() { Name = "Share" }));
        var panel = await BoxAsync(tabs.GetByRole(AriaRole.Tabpanel));

        Assert.True(start.Y + start.Height <= list.Y + 0.5, "Start isn't above the tabs.");
        Assert.True(add.Y >= list.Y + list.Height - 0.5, "The add button isn't under the tabs.");
        Assert.True(end.Y >= list.Y + list.Height - 0.5, "End isn't under the tabs.");
        Assert.True(panel.X >= list.X + list.Width - 0.5, "The panel isn't beside the tabs.");
        Assert.Equal(start.Y, panel.Y, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldRunTheListsLineDownThePanel_WhenAColumnHasStartContentAndNothingUnderIt(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var tabs = Tabs(page, "Stacked");

        var start = await BoxAsync(tabs.GetByRole(AriaRole.Button, new() { Name = "Menu" }));
        var list = await BoxAsync(tabs.GetByRole(AriaRole.Tablist));
        var panel = await BoxAsync(tabs.GetByRole(AriaRole.Tabpanel));

        Assert.True(start.Y + start.Height <= list.Y + 0.5, "Start isn't above the tabs.");
        Assert.True(panel.Height > 150, "The panel should be taller than its tabs.");
        Assert.Equal(panel.Y + panel.Height, list.Y + list.Height, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldDrawNoLineUnderTheStartContent_WhenTheTabsArePills(string browser)
    {
        var page = await OpenReadyAsync(browser);

        var line = await Tabs(page, "Pills").Locator(":scope > .tb-tabs__start")
            .EvaluateAsync<string>("cell => getComputedStyle(cell).borderBottomWidth");
        var plain = await Tabs(page, "Report").Locator(":scope > .tb-tabs__start")
            .EvaluateAsync<string>("cell => getComputedStyle(cell).borderBottomWidth");

        Assert.Equal(("0px", "1px"), (line, plain));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepStartInTheRowAndGiveThePanelTheRestOfTheHeight_WhenFillIsSetToo(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var tabs = Tabs(page, "Filling");

        var whole = await BoxAsync(tabs);
        var start = await BoxAsync(tabs.GetByRole(AriaRole.Button, new() { Name = "Menu" }));
        var list = await BoxAsync(tabs.GetByRole(AriaRole.Tablist));
        var panel = await BoxAsync(tabs.GetByRole(AriaRole.Tabpanel));

        Assert.Equal(240, whole.Height, 1.0);
        Assert.InRange(start.Y + start.Height / 2, list.Y, list.Y + list.Height);
        Assert.Equal(whole.Y + whole.Height, panel.Y + panel.Height, 1.0);
        Assert.Equal(list.Y + list.Height, panel.Y, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldGoThroughStartTheTabsEndAndThenThePanel_WhenTabIsPressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var tabs = Tabs(page, "Report");
        await tabs.GetByRole(AriaRole.Button, new() { Name = "Menu" }).FocusAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(tabs.GetByRole(AriaRole.Tab, new() { Name = "Summary" })).ToBeFocusedAsync();
        // The arrow keys stay with the tabs. Each one selects a tab, so its panel is waited for before the next key.
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(tabs.GetByRole(AriaRole.Tab, new() { Name = "Details" })).ToBeFocusedAsync();
        await Expect(tabs.GetByText("Details panel")).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(tabs.GetByRole(AriaRole.Tab, new() { Name = "Summary" })).ToBeFocusedAsync();
        await Expect(tabs.GetByRole(AriaRole.Button, new() { Name = "In the panel" })).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(tabs.GetByRole(AriaRole.Button, new() { Name = "Share" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(tabs.GetByRole(AriaRole.Button, new() { Name = "In the panel" })).ToBeFocusedAsync();
    }
}
