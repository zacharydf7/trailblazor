namespace Trailblazor.BrowserTests;

public class ToggleGroupTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Group(IPage page, string name) => page.GetByRole(AriaRole.Group, new() { Name = name });

    private static ILocator Item(IPage page, string group, string name) =>
        Group(page, group).GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private async Task<IPage> OpenGroupsAsync(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toggle-group");
        foreach (var name in new[] { "Map view", "Show on map", "Distance units" })
        {
            await Expect(Group(page, name)).ToHaveAttributeAsync("data-ready", "");
        }

        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldBeOneTabStopLandingOnThePressedItem_WhenTabbedThrough(string browser)
    {
        var page = await OpenGroupsAsync(browser);
        await Item(page, "Map view", "Trails").FocusAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(Item(page, "Show on map", "Water")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(Item(page, "Map view", "Trails")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldMoveFocusAndPressWithTheKeys_WhenTheArrowsHomeEndAndSpaceAreUsed(string browser)
    {
        var page = await OpenGroupsAsync(browser);
        await Item(page, "Map view", "Trails").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item(page, "Map view", "Satellite")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(Item(page, "Map view", "Terrain")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item(page, "Map view", "Trails")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("Space");

        await Expect(Item(page, "Map view", "Terrain")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Item(page, "Map view", "Trails")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(page.GetByText("View: ")).ToHaveTextAsync("View: Terrain");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiToggleGroup_ShouldSkipDisabledItemsAndPressSeveral_WhenUsedWithTheKeys(string browser)
    {
        var page = await OpenGroupsAsync(browser);
        await Item(page, "Show on map", "Water").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item(page, "Show on map", "Alerts")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Home");
        await page.Keyboard.PressAsync("Space");

        await Expect(page.GetByText("Showing: ")).ToHaveTextAsync("Showing: Water, Alerts, Campsites");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldMoveWithUpAndDown_WhenVertical(string browser)
    {
        var page = await OpenGroupsAsync(browser);
        await Item(page, "Distance units", "Miles").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item(page, "Distance units", "Miles")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(Item(page, "Distance units", "Kilometers")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldSwapLeftAndRight_WhenTheGroupIsRightToLeft(string browser)
    {
        var page = await OpenGroupsAsync(browser);
        await Group(page, "Map view").EvaluateAsync("group => group.dir = 'rtl'");
        await Item(page, "Map view", "Trails").FocusAsync();

        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(Item(page, "Map view", "Satellite")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldClearThePressedItem_WhenItIsPressedAgainInAClearableGroup(string browser)
    {
        var page = await OpenGroupsAsync(browser);
        var miles = Item(page, "Distance units", "Miles");

        await miles.ClickAsync();
        await Expect(page.GetByText("Units: ")).ToHaveTextAsync("Units: Miles");
        await miles.ClickAsync();

        await Expect(page.GetByText("Units: ")).ToHaveTextAsync("Units: none");
        await Expect(miles).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldKeepTheTabStopOnTheLastFocusedItem_WhenFocusLeavesAndComesBack(string browser)
    {
        var page = await OpenGroupsAsync(browser);
        await Item(page, "Map view", "Trails").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");

        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(Item(page, "Map view", "Satellite")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldFillThePressedItemWithTheHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toggle-group", new() { ForcedColors = ForcedColors.Active });

        await Expect(Item(page, "Map view", "Trails"))
            .ToHaveCSSAsync("background-color", await SystemColorAsync(page, "Highlight"));
    }
}
