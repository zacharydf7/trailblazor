namespace Trailblazor.BrowserTests;

// A tab the parent removes by any route, not only its close mark, hands focus to a neighbor if it held focus.
public class TabsRemoveTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldFocusTheNextTab_WhenTheParentRemovesTheFocusedOne(string browser)
    {
        var page = await OpenRemoveAsync(browser);
        await Tab(page, "Beta").FocusAsync();

        await page.Keyboard.PressAsync("r");

        await Expect(Tab(page, "Beta")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Gamma")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldPassOverTabsRemovedWithItAndDisabledOnes_WhenTheParentRemovesTheFocusedOne(
        string browser)
    {
        var page = await OpenRemoveAsync(browser);
        await Tab(page, "Beta").FocusAsync();

        await page.Keyboard.PressAsync("b");

        await Expect(Tab(page, "Gamma")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Echo")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldFocusTheEnabledTabBefore_WhenTheParentRemovesTheFocusedLastOne(string browser)
    {
        var page = await OpenRemoveAsync(browser);
        await Tab(page, "Echo").FocusAsync();

        await page.Keyboard.PressAsync("l");

        await Expect(Tab(page, "Echo")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Gamma")).ToBeFocusedAsync();
    }

    // A window losing focus sends focusout while the tab keeps focus.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldFocusTheNextTab_WhenTheParentRemovesTheFocusedOneAfterAFocusoutThatKeptFocus(
        string browser)
    {
        var page = await OpenRemoveAsync(browser);
        await Tab(page, "Beta").FocusAsync();
        await Tab(page, "Beta").DispatchEventAsync("focusout");

        await page.Keyboard.PressAsync("r");

        await Expect(Tab(page, "Beta")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Gamma")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldLeaveFocusAlone_WhenTheParentRemovesATabAfterFocusLeftItForAField(string browser)
    {
        var page = await OpenRemoveAsync(browser);
        await Tab(page, "Beta").FocusAsync();
        var notes = page.GetByLabel("Notes");
        await notes.FocusAsync();

        await page.Keyboard.PressAsync("r");

        await Expect(Tab(page, "Beta")).ToHaveCountAsync(0);
        await Expect(notes).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldLeaveFocusOnThePage_WhenTheParentRemovesATabAfterFocusLeftItForThePage(string browser)
    {
        var page = await OpenRemoveAsync(browser);
        await Tab(page, "Beta").FocusAsync();

        await page.GetByText("Remove Beta").ClickAsync();

        await Expect(Tab(page, "Beta")).ToHaveCountAsync(0);
        Assert.True(await page.EvaluateAsync<bool>("() => document.activeElement === document.body"));
    }

    private static ILocator Tab(IPage page, string name) =>
        page.GetByRole(AriaRole.Tab, new() { Name = name, Exact = true });

    private async Task<IPage> OpenRemoveAsync(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-remove");
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Trips" })).ToHaveAttributeAsync("data-ready", "");
        return page;
    }
}
