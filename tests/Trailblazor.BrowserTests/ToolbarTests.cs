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

    private static ILocator Tool(IPage page, string toolbar, string name) =>
        Toolbar(page, toolbar).GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldReachASelectWithTheArrowsAsOneTabStop_WhenItHoldsOne(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        var select = Toolbar(page, "Tools").GetByRole(AriaRole.Combobox, new() { Name = "Size" });
        await Tool(page, "Tools", "Back").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(select).ToBeFocusedAsync();
        await Tool(page, "Tools", "Back").FocusAsync();
        await Expect(select).ToHaveAttributeAsync("tabindex", "-1");
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.Locator("[role=toolbar][aria-label='Tools'] :focus")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldKeepItsOwnKeys_WhenInAToolbar(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        var select = Toolbar(page, "Tools").GetByRole(AriaRole.Combobox, new() { Name = "Size" });
        await Expect(select).ToHaveAttributeAsync("data-ready", "");
        await select.FocusAsync();

        await page.Keyboard.PressAsync("End");

        await Expect(select).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(select).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldSkipATurnedOffSelect_WhenTheArrowsMove(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        await Tool(page, "With off select", "First").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Tool(page, "With off select", "Last")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldReachButtonsAndLinksKeptFocusable_WhenTheyreOff(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        await Tool(page, "Tools", "Save").FocusAsync();
        var link = Toolbar(page, "Tools").GetByRole(AriaRole.Link, new() { Name = "Off link" });

        // Left passes over the plain disabled button.
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(link).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tool(page, "Tools", "Off button")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.Locator("[role=toolbar][aria-label='Tools'] :focus")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldSkipLinksThatAreOff_WhenOneStartsOffAndOneTurnsOff(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        await page.GetByRole(AriaRole.Button, new() { Name = "Turn link off" }).ClickAsync();
        await Expect(Toolbar(page, "With links").GetByRole(AriaRole.Link, new() { Name = "Turning link" }))
            .ToHaveAttributeAsync("aria-disabled", "true");
        await Tool(page, "With links", "Start").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Toolbar(page, "With links").GetByRole(AriaRole.Combobox, new() { Name = "Turning size" }))
            .ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldKeepOneTabStop_WhenALinkBecomesFocusableWhileOff(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        var link = Toolbar(page, "With links").GetByRole(AriaRole.Link, new() { Name = "Kept link" });

        await page.GetByRole(AriaRole.Button, new() { Name = "Keep link focusable" }).ClickAsync();
        await Expect(link).ToHaveAttributeAsync("data-kept-focusable", "");

        await Expect(Toolbar(page, "With links").Locator("[tabindex='0']")).ToHaveCountAsync(1);
        await Expect(link).ToHaveAttributeAsync("tabindex", "-1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldKeepItsTabStop_WhenSomethingChangesWhileItsHidden(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        var toolbar = Toolbar(page, "Stacked");
        var select = toolbar.GetByRole(AriaRole.Combobox, new() { Name = "Stacked pick" });
        await Expect(select).ToHaveAttributeAsync("tabindex", "0");

        await toolbar.EvaluateAsync("""
            toolbar => {
                toolbar.style.display = 'none';
                toolbar.querySelector('button').setAttribute('aria-pressed', 'true');
                return new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)));
            }
            """);
        // A hidden toolbar has no role to find it by.
        await page.Locator("[aria-label='Stacked']").EvaluateAsync("toolbar => toolbar.style.display = ''");

        await Expect(select).ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldKeepFocusOnASelect_WhenItTurnsOffWhileFocused(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        var select = Toolbar(page, "With links").GetByRole(AriaRole.Combobox, new() { Name = "Turning size" });
        await select.FocusAsync();

        // A click from a script leaves focus where it is, as a save finishing in the background would.
        await page.GetByRole(AriaRole.Button, new() { Name = "Turn size off" }).EvaluateAsync("button => button.click()");
        await Expect(select).ToHaveAttributeAsync("aria-disabled", "true");
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");

        await Expect(select).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(select).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldKeepOneTabStop_WhenTheLinkHoldingItTurnsOff(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        var link = Toolbar(page, "With links").GetByRole(AriaRole.Link, new() { Name = "Turning link" });
        await link.FocusAsync();
        await Expect(link).ToHaveAttributeAsync("tabindex", "0");

        await page.GetByRole(AriaRole.Button, new() { Name = "Turn link off" }).ClickAsync();
        await Expect(link).ToHaveAttributeAsync("aria-disabled", "true");

        await Expect(Toolbar(page, "With links").Locator("[tabindex='0']")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldKeepOneTabStop_WhenTheLinkHoldingItTurnsOffWhileHidden(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        foreach (var (name, linkName, turnOff) in new[]
            { ("With links", "Turning link", "Turn link off"), ("Stacked", "Stacked link", "Turn stacked link off") })
        {
            var link = Toolbar(page, name).GetByRole(AriaRole.Link, new() { Name = linkName });
            await link.FocusAsync();
            await Expect(link).ToHaveAttributeAsync("tabindex", "0");
            var toolbar = page.Locator($"[aria-label='{name}']");

            await toolbar.EvaluateAsync("toolbar => toolbar.style.display = 'none'");
            await page.GetByRole(AriaRole.Button, new() { Name = turnOff }).ClickAsync();
            await Expect(toolbar.Locator("a", new() { HasText = linkName })).ToHaveAttributeAsync("aria-disabled", "true");
            await toolbar.EvaluateAsync("toolbar => toolbar.style.display = ''");

            await Expect(link).Not.ToHaveAttributeAsync("tabindex", "0");
            await Expect(Toolbar(page, name).Locator("[tabindex='0']")).ToHaveCountAsync(1);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldKeepTheTabStopOnTheLastFocusedControl_WhenHiddenAndShownAgain(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        foreach (var (name, control) in new[] { ("Tools", "Next"), ("Stacked", "Stacked button") })
        {
            var last = Tool(page, name, control);
            await last.FocusAsync();
            var toolbar = page.Locator($"[aria-label='{name}']");

            // A few frames each way let the toolbar refit, and sync, while hidden and once shown.
            foreach (var display in new[] { "none", "" })
            {
                await toolbar.EvaluateAsync($$"""
                    toolbar => {
                        toolbar.style.display = '{{display}}';
                        return new Promise(done => requestAnimationFrame(() => requestAnimationFrame(
                            () => requestAnimationFrame(done))));
                    }
                    """);
            }

            await Expect(last).ToHaveAttributeAsync("tabindex", "0");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldKeepOneTabStop_WhenABusyLinkTurnsBackOn(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        var link = Toolbar(page, "Busy").GetByRole(AriaRole.Link, new() { Name = "Busy link" });

        await page.GetByRole(AriaRole.Button, new() { Name = "Turn busy link on" }).ClickAsync();
        await Expect(link).ToHaveAttributeAsync("href", "toolbar");

        await Expect(link).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Tool(page, "Busy", "Busy start")).ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldKeepItsKeysAndTabStop_WhenTheFocusedButtonTurnsLoading(string browser)
    {
        var page = await OpenToolbarAsync(browser, "/toolbar-keys");
        var save = Tool(page, "Tools", "Save");
        await save.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(save).ToHaveAttributeAsync("aria-busy", "true");

        await Expect(save).ToHaveAttributeAsync("tabindex", "0");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tool(page, "Tools", "Next")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(save).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldMoveFocusToItsMenu_WhenTheFocusedButtonMovesIntoIt(string browser)
    {
        var page = await OpenToolbarAsync(browser);
        await Button(page, "Delete note").FocusAsync();

        await NarrowAsync(page, "20rem");

        await Expect(Button(page, "More")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldMoveFocusToItsLastControl_WhenItsFocusedMenuGoesAway(string browser)
    {
        var page = await OpenToolbarAsync(browser);
        await NarrowAsync(page, "20rem");
        await Button(page, "More").FocusAsync();

        await NarrowAsync(page, "100%");

        await Expect(Button(page, "Delete note")).ToBeFocusedAsync();
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
