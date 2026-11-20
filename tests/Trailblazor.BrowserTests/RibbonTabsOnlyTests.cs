using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class RibbonTabsOnlyTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Ribbon(IPage page) =>
        page.Locator("[data-part=ribbon]:has([role=tablist]:is([aria-label=Planner]))");

    private static ILocator Toggle(IPage page) =>
        Ribbon(page).GetByRole(AriaRole.Button, new() { Name = "Ribbon commands", Exact = true });

    private static ILocator Tab(IPage page, string name) => page.GetByRole(AriaRole.Tab, new() { Name = name });

    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/ribbon-tabs-only");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    private async Task<IPage> OpenCollapsedAsync(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Toggle(page).ClickAsync();
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-collapsed", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldHideItsCommandsAndShowThemAgain_WhenItsCollapseButtonIsPressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Expect(Toggle(page)).ToHaveAttributeAsync("aria-expanded", "true");

        await Toggle(page).ClickAsync();

        await Expect(Button(page, "Add")).ToBeHiddenAsync();
        await Expect(Toggle(page)).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByTestId("collapsed")).ToHaveTextAsync("collapsed");
        await Toggle(page).ClickAsync();
        await Expect(Button(page, "Add")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("collapsed")).ToHaveTextAsync("showing");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldCollapseAndMoveFocusToTheSelectedTab_WhenCtrlF1IsPressedInItsCommands(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Button(page, "Add").FocusAsync();

        await page.Keyboard.PressAsync("Control+F1");

        await Expect(Button(page, "Add")).ToBeHiddenAsync();
        await Expect(Tab(page, "Home")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Control+F1");
        await Expect(Button(page, "Add")).ToBeVisibleAsync();
        await Expect(Tab(page, "Home")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldCollapseAndShowItsCommandsAgain_WhenATabIsDoubleClicked(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await Tab(page, "Home").DblClickAsync();
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-collapsed", "");
        await Tab(page, "Home").DblClickAsync();

        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-collapsed", "");
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-peek", "");
        await Expect(Button(page, "Add")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldKeepItsCommandsShowing_WhenCtrlF1OrADoubleClickReachesOneThatIsNotCollapsible(
        string browser)
    {
        var page = await OpenAsync(browser, "/ribbon-collapse");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-ready", "");
        await Button(page, "Find").FocusAsync();

        await page.Keyboard.PressAsync("Control+F1");
        await Tab(page, "Home").DblClickAsync();

        // A command still runs, which it couldn't from a ribbon that had collapsed in the meantime.
        await Button(page, "Find").ClickAsync();
        await Expect(page.GetByTestId("last")).ToHaveTextAsync("Find");
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-collapsed", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CollapsedRibbon_ShouldShowATabsCommandsOverThePage_WhenTheTabIsChosen(string browser)
    {
        var page = await OpenCollapsedAsync(browser);
        var under = page.GetByTestId("under");
        var before = (await under.BoundingBoxAsync())!;

        await Tab(page, "Review").ClickAsync();

        await Expect(Button(page, "Spelling")).ToBeVisibleAsync();
        var commands = (await page.GetByRole(AriaRole.Toolbar, new() { Name = "Review" }).BoundingBoxAsync())!;
        var after = (await under.BoundingBoxAsync())!;
        var tabs = (await page.GetByRole(AriaRole.Tablist).BoundingBoxAsync())!;
        // The page stays where it was, and the commands cover the start of it, under the tabs.
        Assert.Equal(before.Y, after.Y);
        Assert.True(commands.Y + commands.Height > after.Y, $"The commands end at {commands.Y + commands.Height}.");
        Assert.True(commands.Y >= tabs.Y + tabs.Height, $"The commands start at {commands.Y}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CollapsedRibbon_ShouldRunTheCommandAndHideTheCommands_WhenACommandIsChosen(string browser)
    {
        var page = await OpenCollapsedAsync(browser);
        await Tab(page, "Home").ClickAsync();

        await Button(page, "Add").ClickAsync();

        await Expect(page.GetByTestId("last")).ToHaveTextAsync("Add");
        await Expect(Button(page, "Add")).ToBeHiddenAsync();
        await Expect(Tab(page, "Home")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CollapsedRibbon_ShouldKeepItsCommandsShowing_WhenAFieldInThemIsTypedIn(string browser)
    {
        var page = await OpenCollapsedAsync(browser);
        await Tab(page, "Home").ClickAsync();
        var title = page.GetByLabel("Title");

        await title.ClickAsync();
        await title.PressSequentiallyAsync("Ridge");

        await Expect(title).ToHaveValueAsync("Ridge");
        await Expect(title).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CollapsedRibbon_ShouldKeepItsCommandsShowing_WhenAMenuInThemOpensAndEscapeClosesIt(string browser)
    {
        var page = await OpenCollapsedAsync(browser);
        await Tab(page, "Home").ClickAsync();
        var item = page.GetByRole(AriaRole.Menuitem, new() { Name = "As a PDF" });

        await Button(page, "Export").ClickAsync();
        await Expect(item).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(item).ToBeHiddenAsync();
        await Expect(Button(page, "Export")).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(Button(page, "Export")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CollapsedRibbon_ShouldHideItsCommandsAndLeaveItsDialogOpen_WhenEscapeIsPressedInThem(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Button(page, "Open the dialog").ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Quick tools" });
        var ribbon = dialog.Locator("[data-part=ribbon]");
        await Expect(ribbon).ToHaveAttributeAsync("data-ready", "");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Ribbon commands" }).ClickAsync();
        // Enter on a tab shows its commands only once the ribbon has collapsed.
        await Expect(ribbon).ToHaveAttributeAsync("data-collapsed", "");
        await Tab(page, "Tools").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(Button(page, "Ruler")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(Button(page, "Ruler")).ToBeHiddenAsync();
        // A dialog that had also handled Escape would report closing only after its close animation, about a
        // quarter of a second, so wait longer than that before checking it's still open.
        await page.WaitForTimeoutAsync(500);
        await Expect(page.GetByTestId("dialog")).ToHaveTextAsync("open");
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByTestId("dialog")).ToHaveTextAsync("closed");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CollapsedRibbon_ShouldKeepItsCommandsShowing_WhenAControlInThemUsesEscapeItself(string browser)
    {
        var page = await OpenCollapsedAsync(browser);
        await Tab(page, "Home").ClickAsync();
        var code = page.GetByLabel("Code");
        await code.FocusAsync();

        await page.Keyboard.PressAsync("Escape");
        await code.PressSequentiallyAsync("N4");

        // Typing only reaches a field that still shows.
        await Expect(code).ToHaveValueAsync("N4");
        await Expect(code).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CollapsedRibbon_ShouldShowTheCommandsAndHideThemAgain_WhenEnterThenEscapeArePressed(string browser)
    {
        var page = await OpenCollapsedAsync(browser);
        await Tab(page, "Home").FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        // The commands' one tab stop comes back a frame or two after they show.
        await Expect(Button(page, "Add")).ToHaveAttributeAsync("tabindex", "0");
        // Tab goes to the collapse button, which ends the tabs' row, and then into the commands.
        await page.Keyboard.PressAsync("Tab");
        await Expect(Toggle(page)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(Button(page, "Add")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(Button(page, "Add")).ToBeHiddenAsync();
        await Expect(Tab(page, "Home")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CollapsedRibbon_ShouldHideItsCommands_WhenFocusOrAPressGoesElsewhere(string browser)
    {
        var page = await OpenCollapsedAsync(browser);

        await Tab(page, "Home").ClickAsync();
        await Expect(Button(page, "Add")).ToBeVisibleAsync();
        await Button(page, "Before").FocusAsync();
        await Expect(Button(page, "Add")).ToBeHiddenAsync();

        await Tab(page, "Home").ClickAsync();
        await Expect(Button(page, "Add")).ToBeVisibleAsync();
        await page.GetByTestId("far").ClickAsync();
        await Expect(Button(page, "Add")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldShowItsCommandsInPlace_WhenTheAppSetsCollapsedBackWhileATabsCommandsShow(
        string browser)
    {
        var page = await OpenCollapsedAsync(browser);
        var under = page.GetByTestId("under");
        var collapsed = (await under.BoundingBoxAsync())!;
        await Tab(page, "Home").ClickAsync();
        await Expect(Button(page, "Add")).ToBeVisibleAsync();

        // Not a press, which would hide the commands as a press elsewhere does.
        await Button(page, "Set from code").EvaluateAsync("button => button.click()");

        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-collapsed", "");
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-peek", "");
        await Expect(Button(page, "Add")).ToBeVisibleAsync();
        Assert.True((await under.BoundingBoxAsync())!.Y > collapsed.Y, "The commands should push the page down again.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldChooseTheTabOfACollapsedRibbonToClickAButton_WhenTheRibbonIsCollapsed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var ribbon = page.Tb().Ribbon("Planner");

        await ribbon.CollapseAsync();
        Assert.True(await ribbon.IsCollapsedAsync());
        await ribbon.ClickAsync("Add");
        await Expect(page.GetByTestId("last")).ToHaveTextAsync("Add");
        await ribbon.ExpandAsync();

        Assert.False(await ribbon.IsCollapsedAsync());
        await Expect(Button(page, "Add")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReachAButton_WhenTheRibbonStartsCollapsedAndThePageHasJustOpened(string browser)
    {
        var (page, held) = await OpenHeldAsync(browser, "/ribbon-tabs-only?start=true");

        // The prerendered ribbon shows its commands, and collapses once the page is interactive.
        var clicking = page.Tb().Ribbon("Planner").ClickAsync("Add");
        await page.WaitForTimeoutAsync(300);
        held.SetResult();
        await clicking;

        await Expect(page.GetByTestId("last")).ToHaveTextAsync("Add");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldRefuseToCollapseOrExpand_WhenTheRibbonHasNoCollapseButtonOrThePageIsStatic(
        string browser)
    {
        var plain = await OpenAsync(browser, "/ribbon-collapse");
        var @static = await OpenStaticAsync(browser, "/static-ribbon");
        var ribbon = @static.Tb().Ribbon("Map tools");

        Assert.Equal("The ribbon has no collapse button; set Collapsible to give it one.", (await
            Assert.ThrowsAsync<InvalidOperationException>(plain.Tb().Ribbon("Planner").ExpandAsync)).Message);
        Assert.False(await ribbon.IsCollapsedAsync());
        Assert.Equal("On a static page the ribbon can't collapse; render the page interactively to collapse it.",
            (await Assert.ThrowsAsync<InvalidOperationException>(ribbon.CollapseAsync)).Message);
    }
}
