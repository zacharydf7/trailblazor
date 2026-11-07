using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class ToolbarTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Toolbar(IPage page, string name) =>
        page.GetByRole(AriaRole.Toolbar, new() { Name = name });

    private static ILocator Button(IPage page, string name) =>
        Toolbar(page, "Trail notes").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private async Task<IPage> OpenToolbarAsync(string browser, string path = "/examples/toolbar")
    {
        var page = await OpenAsync(browser, path);
        foreach (var toolbar in await page.Locator(".tb-toolbar").AllAsync())
        {
            await Expect(toolbar).ToHaveAttributeAsync("data-ready", "");
        }

        return page;
    }

    // Narrows the frame around the toolbar, which the toolbar then fits itself into.
    private static Task NarrowAsync(IPage page, string width) =>
        Toolbar(page, "Trail notes").Locator("xpath=..").EvaluateAsync($"frame => frame.style.inlineSize = '{width}'");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldShowItsNamedMenu_WhenItsButtonsDontFit(string browser)
    {
        var page = await OpenToolbarAsync(browser);

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "More map actions" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldMoveAcrossEveryControlWithTheArrows_WhenControlsAreNested(string browser)
    {
        var page = await OpenToolbarAsync(browser);
        await Button(page, "Newest").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Button(page, "Oldest")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Button(page, "Add a note")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(Button(page, "Delete note")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");

        await Expect(Button(page, "Newest")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldBeOneTabStop_WhenTabbedThrough(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar");
        var back = Toolbar(page, "Search").GetByRole(AriaRole.Button, new() { Name = "Back" });
        await back.FocusAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(Toolbar(page, "Tools").GetByRole(AriaRole.Button, new() { Name = "Pan" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(back).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldKeepOneTabStop_WhenAToggleGroupInsideItChangesFromOutside(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar");
        var search = Toolbar(page, "Search");
        await page.GetByRole(AriaRole.Button, new() { Name = "Show search modes" }).ClickAsync();
        await Expect(search.GetByRole(AriaRole.Group, new() { Name = "Search in" }))
            .ToHaveAttributeAsync("data-ready", "");

        await page.GetByRole(AriaRole.Button, new() { Name = "Search in notes" }).ClickAsync();

        await Expect(search.GetByRole(AriaRole.Button, new() { Name = "Notes" }))
            .ToHaveAttributeAsync("aria-pressed", "true");
        // A group inside the toolbar's popup keeps its own tab stop, so only the toolbar's own controls count.
        await Expect(search.Locator("[tabindex='0']:not([popover] *)")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldMoveButtonsIntoTheMoreMenuAndBack_WhenItNarrowsAndWidens(string browser)
    {
        var page = await OpenToolbarAsync(browser);
        var more = Button(page, "More");
        await Expect(more).ToBeHiddenAsync();

        await NarrowAsync(page, "20rem");
        await Expect(more).ToBeVisibleAsync();
        await Expect(Button(page, "Delete note")).ToBeHiddenAsync();
        await Expect(Button(page, "Add a note")).ToBeVisibleAsync();
        await more.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Add a note" })).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Delete note" }).ClickAsync();
        await Expect(page.GetByText("Last action: ")).ToHaveTextAsync("Last action: Delete note");
        await NarrowAsync(page, "100%");

        await Expect(more).ToBeHiddenAsync();
        await Expect(Button(page, "Delete note")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldLeaveTheKeysToTheBrowser_WhenAltCtrlOrCmdIsHeldOnItsMenuButton(string browser)
    {
        var page = await OpenToolbarAsync(browser);
        await NarrowAsync(page, "20rem");
        var more = Button(page, "More");
        await Expect(more).ToBeVisibleAsync();
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "false");
        await more.FocusAsync();

        foreach (var key in new[] { "Alt+ArrowDown", "Control+ArrowUp", "Meta+ArrowDown", "Alt+ArrowLeft", "Control+Home" })
        {
            await page.Keyboard.PressAsync(key);
            // A wrongly opened menu shows here at once, while focus would move into it only later.
            await Expect(page.Locator("[role=menu]:popover-open")).ToHaveCountAsync(0);
            await Expect(more).ToBeFocusedAsync();
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldSkipMovedButtonsAndReachTheMoreButton_WhenUsingTheArrows(string browser)
    {
        var page = await OpenToolbarAsync(browser);
        await NarrowAsync(page, "20rem");
        await Expect(Button(page, "More")).ToBeVisibleAsync();
        await Button(page, "Newest").FocusAsync();

        await page.Keyboard.PressAsync("End");

        await Expect(Button(page, "More")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Button(page, "More")).Not.ToBeFocusedAsync();
        Assert.True(await page.EvaluateAsync<bool>("() => document.activeElement.checkVisibility()"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldNotDescribeAnIconButtonByItsOwnName_WhenItsTooltipRepeatsIt(string browser)
    {
        var page = await OpenToolbarAsync(browser);

        await Expect(Button(page, "Add a note")).ToHaveAccessibleDescriptionAsync("");
        await Button(page, "Add a note").FocusAsync();
        await Expect(page.GetByRole(AriaRole.Tooltip)).ToHaveTextAsync("Add a note");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldLeaveTheArrowsToATextField_WhenItHasFocus(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar");
        var search = page.GetByRole(AriaRole.Textbox, new() { Name = "Search notes" });
        await search.FocusAsync();

        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(search).ToBeFocusedAsync();
        Assert.Equal(4, await search.EvaluateAsync<int>("input => input.selectionStart"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldKeepItsOwnKeys_WhenInAToolbarWithoutTheToolbarScript(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar");
        var plain = Toolbar(page, "Plain");
        await Expect(plain.GetByRole(AriaRole.Group)).ToHaveAttributeAsync("data-ready", "");
        await plain.GetByRole(AriaRole.Button, new() { Name = "Small" }).FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(plain.GetByRole(AriaRole.Button, new() { Name = "Large" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldKeepItsOwnKeys_WhenInAPopupInsideAToolbar(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar");
        var panel = await page.Tb().Popover("Units").OpenAsync();
        var units = panel.GetByRole(AriaRole.Group, new() { Name = "Distance units" });
        await Expect(units).ToHaveAttributeAsync("data-ready", "");
        await units.GetByRole(AriaRole.Button, new() { Name = "Miles" }).FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(units.GetByRole(AriaRole.Button, new() { Name = "Kilometers" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldMoveWithUpAndDown_WhenVertical(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar");
        var tools = Toolbar(page, "Tools");
        await Expect(tools).ToHaveAttributeAsync("aria-orientation", "vertical");
        await tools.GetByRole(AriaRole.Button, new() { Name = "Pan" }).FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(tools.GetByRole(AriaRole.Button, new() { Name = "Zoom" })).ToBeFocusedAsync();
    }
}
