using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class RibbonKeyTipsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Ribbon(IPage page, string name = "Journal") =>
        page.Locator($"[data-part=ribbon]:has([role=tablist]:is([aria-label={name}]))");

    private static ILocator Tab(IPage page, string name) => page.GetByRole(AriaRole.Tab, new() { Name = name });

    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static ILocator Notes(IPage page) => page.GetByRole(AriaRole.Textbox, new() { Name = "Notes" });

    // The key each showing badge names, read from the controls the script has marked.
    private static async Task<string[]> ShownAsync(IPage page) => await page.Locator("[data-key-tip-shown]")
        .EvaluateAllAsync<string[]>("controls => controls.map(control => control.getAttribute('aria-keyshortcuts'))");

    private static Task<string> BadgeAsync(ILocator control) =>
        control.EvaluateAsync<string>("control => getComputedStyle(control, '::after').content");

    private async Task<IPage> OpenReadyAsync(string browser, string query = "",
        BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/ribbon-key-tips" + query, options);
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-ready", "");
        await Expect(page.GetByRole(AriaRole.Toolbar, new() { Name = "Home" })).ToHaveAttributeAsync("data-ready", "");
        await Notes(page).FocusAsync();
        return page;
    }

    // Alt and then a tab's key, with the tab's commands showing their key tips before the test goes on.
    private async Task<IPage> OpenAtCommandsAsync(string browser, string query = "")
    {
        var page = await OpenReadyAsync(browser, query);
        await page.Keyboard.PressAsync("Alt");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");
        await page.Keyboard.PressAsync("h");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "commands");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldShowOnTheTabsAndBesideThemAndFocusTheSelectedTab_WhenAltIsPressedAlone(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        Assert.Equal("none", await BadgeAsync(Tab(page, "Home")));
        // What the selected tab says of its key at the moment it takes focus, which is when a screen reader reads it.
        await Tab(page, "Home").EvaluateAsync("""
            tab => tab.addEventListener('focus', () => { tab.dataset.heard = tab.getAttribute('aria-keyshortcuts'); })
            """);

        await page.Keyboard.PressAsync("Alt");

        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");
        Assert.Equal(["Q", "H", "V"], await ShownAsync(page));
        Assert.Contains("\"H\"", await BadgeAsync(Tab(page, "Home")));
        Assert.Equal("none", await BadgeAsync(Tab(page, "Help")));
        await Expect(Tab(page, "Home")).ToBeFocusedAsync();
        await Expect(Tab(page, "Home")).ToHaveAttributeAsync("data-heard", "H");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldDrawEachBadgeOverTheLowerEdgeOfItsControl_WhenTheyShow(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var list = page.GetByRole(AriaRole.Tablist, new() { Name = "Journal" });
        var height = (await list.BoundingBoxAsync())!.Height;

        await page.Keyboard.PressAsync("Alt");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");

        var badge = await Tab(page, "Home").EvaluateAsync<string[]>("""
            tab => {
                const badge = getComputedStyle(tab, '::after');
                return [badge.position, badge.bottom, badge.pointerEvents, String(parseFloat(badge.width) > 0)];
            }
            """);
        Assert.Equal(["absolute", "0px", "none", "true"], badge);
        // Inside the tab's own box, the badge neither grows the row nor makes it scroll.
        Assert.Equal(height, (await list.BoundingBoxAsync())!.Height);
        Assert.True(await list.EvaluateAsync<bool>("list => list.scrollHeight <= list.clientHeight"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldDrawTheirBadgesInTheSystemsHighlightColors_WhenForcedColorsAreOn(string browser)
    {
        var page = await OpenReadyAsync(browser, options: new() { ForcedColors = ForcedColors.Active });

        await page.Keyboard.PressAsync("Alt");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");

        var colors = await Tab(page, "Home").EvaluateAsync<string[]>("""
            tab => {
                const badge = getComputedStyle(tab, '::after');
                return [badge.backgroundColor, badge.color];
            }
            """);
        var system = new[] { await SystemColorAsync(page, "Highlight"), await SystemColorAsync(page, "HighlightText") };
        Assert.Equal(system, colors);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldSelectTheTabAndShowItsCommandsKeys_WhenATabsKeyIsPressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.Keyboard.PressAsync("Alt");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");

        // Shift on its way down is no key of its own, and the letter counts in either case.
        await page.Keyboard.PressAsync("Shift+V");

        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "commands");
        await Expect(Tab(page, "View")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.Locator("[data-key-tip-shown]")).ToHaveCountAsync(1);
        Assert.Equal(["B"], await ShownAsync(page));
        await Expect(Tab(page, "View")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldRunTheCommandAndGiveFocusBack_WhenACommandsKeyIsPressed(string browser)
    {
        var page = await OpenAtCommandsAsync(browser);
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=N]")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("n");

        await Expect(page.GetByTestId("last")).ToHaveTextAsync("New entry");
        await Expect(Notes(page)).ToBeFocusedAsync();
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "commands");
        await Expect(page.Locator("[data-key-tip-shown]")).ToHaveCountAsync(0);
        await Expect(Button(page, "New entry")).Not.ToHaveAttributeAsync("aria-keyshortcuts", "N");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldRunTheCommandOfTheNewTab_WhenItsKeyIsTypedBeforeTheTabHasSwitched(string browser)
    {
        var page = await OpenReadyAsync(browser);
        // The View tab's commands stay on the page, hidden, once they have been shown.
        await page.Tb().Ribbon("Journal").SelectAsync("View");
        await page.Tb().Ribbon("Journal").SelectAsync("Home");
        await Notes(page).FocusAsync();
        await page.Keyboard.PressAsync("Alt");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");

        // Both keys arrive in one task, before the tab can switch. B is Bold on the tab that still shows.
        await page.EvaluateAsync("""
            () => {
                for (const key of ['v', 'b']) {
                    document.activeElement.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
                }
            }
            """);

        await Expect(page.GetByTestId("last")).ToHaveTextAsync("Zoom in");
        await Expect(page.GetByTestId("bold")).ToHaveTextAsync("plain");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldStepBackToTheTabsAndThenOut_WhenEscapeIsPressedTwice(string browser)
    {
        var page = await OpenAtCommandsAsync(browser);

        await page.Keyboard.PressAsync("Escape");

        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");
        Assert.Equal(["Q", "H", "V"], await ShownAsync(page));
        await Expect(Tab(page, "Home")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "tabs");
        await Expect(Notes(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldHideAndGiveFocusBack_WhenAltIsPressedAgain(string browser)
    {
        var page = await OpenAtCommandsAsync(browser);

        await page.Keyboard.PressAsync("Alt");

        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "commands");
        await Expect(page.Locator("[data-key-tip-shown]")).ToHaveCountAsync(0);
        await Expect(Notes(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldHideAndLeaveTheArrowKeyToTheTabs_WhenAnArrowKeyIsPressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.Keyboard.PressAsync("Alt");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "tabs");
        await Expect(Tab(page, "View")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldHide_WhenThePageIsPressedSomewhere(string browser)
    {
        var page = await OpenAtCommandsAsync(browser);

        await page.GetByTestId("bold").ClickAsync();

        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "commands");
        await Expect(page.Locator("[data-key-tip-shown]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldStayHidden_WhenAltIsLetGoAfterAnotherKey(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.Keyboard.DownAsync("Alt");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.UpAsync("Alt");
        await page.Keyboard.PressAsync("Shift+Alt");

        // The next Alt on its own shows them, so the earlier ones had not.
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "tabs");
        await page.Keyboard.PressAsync("Alt");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldLeaveOutADisabledControl_WhenTheCommandsShow(string browser)
    {
        var page = await OpenAtCommandsAsync(browser);
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=N]")).ToBeVisibleAsync();

        // Neither does the button of a group that hasn't collapsed.
        Assert.Equal(["B", "N", "E", "T", "M", "P", "D"], await ShownAsync(page));
        await page.Keyboard.PressAsync("l");

        // The key does nothing, and the key tips stay for the next one.
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "commands");
        await page.Keyboard.PressAsync("n");
        await Expect(page.GetByTestId("last")).ToHaveTextAsync("New entry");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldPressAToggle_WhenItsKeyIsPressed(string browser)
    {
        var page = await OpenAtCommandsAsync(browser);
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=B]")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("b");

        await Expect(page.GetByTestId("bold")).ToHaveTextAsync("bold");
        await Expect(Button(page, "Bold")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldOpenTheMenuAndLeaveItTheKeys_WhenAMenuButtonsKeyIsPressed(string browser)
    {
        var page = await OpenAtCommandsAsync(browser);
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=E]")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("e");

        var item = page.GetByRole(AriaRole.Menuitem, new() { Name = "As a PDF" });
        await Expect(item).ToBeVisibleAsync();
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "commands");
        await Expect(item).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("last")).ToHaveTextAsync("As a PDF");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldPressAButtonThatOpensSomethingAndLeaveFocusOnIt_WhenItsKeyIsPressed(string browser)
    {
        var page = await OpenAtCommandsAsync(browser);
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=D]")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("d");

        await Expect(Button(page, "Details")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Button(page, "Details")).ToBeFocusedAsync();
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "commands");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldMoveFocusIntoAField_WhenTheKeyOfTheElementAroundItIsPressed(string browser)
    {
        var page = await OpenAtCommandsAsync(browser);
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=T]")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("t");

        var title = page.GetByRole(AriaRole.Textbox, new() { Name = "Title" });
        await Expect(title).ToBeFocusedAsync();
        // The key itself isn't typed, and the next ones are.
        await page.Keyboard.TypeAsync("nb");
        await Expect(title).ToHaveValueAsync("nb");
        await Expect(page.GetByTestId("bold")).ToHaveTextAsync("plain");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldOpenACollapsedGroupAndThenRunItsCommand_WhenTheirKeysArePressed(string browser)
    {
        var page = await OpenAtCommandsAsync(browser, "?narrow=true");
        var group = page.Locator("[data-key-tip-shown][data-key-tip=G]");
        await Expect(group).ToBeVisibleAsync();
        Assert.DoesNotContain("N", await ShownAsync(page));

        await page.Keyboard.PressAsync("g");

        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=N]")).ToBeVisibleAsync();
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "commands");
        await page.Keyboard.PressAsync("n");
        await Expect(page.GetByTestId("last")).ToHaveTextAsync("New entry");
        await Expect(Button(page, "New entry")).ToBeHiddenAsync();
        await Expect(Notes(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldShowACollapsedRibbonsCommandsAndHideThemAgain_WhenATabsKeyThenACommandsArePressed(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.Tb().Ribbon("Journal").CollapseAsync();
        await Notes(page).FocusAsync();
        await page.Keyboard.PressAsync("Alt");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");

        await page.Keyboard.PressAsync("h");

        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=N]")).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("n");
        await Expect(page.GetByTestId("last")).ToHaveTextAsync("New entry");
        await Expect(Button(page, "New entry")).ToBeHiddenAsync();
        await Expect(Notes(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldHideACollapsedRibbonsCommandsAndStepBackToTheTabs_WhenEscapeIsPressed(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.Tb().Ribbon("Journal").CollapseAsync();
        await page.Keyboard.PressAsync("Alt");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");
        await page.Keyboard.PressAsync("h");
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=N]")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(Button(page, "New entry")).ToBeHiddenAsync();
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");
        await Expect(Tab(page, "Home")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldRunAButtonBesideTheTabsAndKeepItsOwnShortcut_WhenItsKeyIsPressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.Keyboard.PressAsync("Alt");
        await Expect(Button(page, "Quick save")).ToHaveAttributeAsync("aria-keyshortcuts", "Q");

        await page.Keyboard.PressAsync("q");

        await Expect(page.GetByTestId("last")).ToHaveTextAsync("Quick save");
        await Expect(Button(page, "Quick save")).ToHaveAttributeAsync("aria-keyshortcuts", "Control+S");
        await Expect(Notes(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldGoToTheRibbonInTheDialogAndLeaveItOpen_WhenADialogIsOpen(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Button(page, "Open the dialog").ClickAsync();
        await Expect(Ribbon(page, "Stamps")).ToHaveAttributeAsync("data-ready", "");
        // The dialog takes focus a moment after it opens, which would end key tips shown before then.
        await page.WaitForFunctionAsync(
            "() => document.querySelector('dialog[open]')?.contains(document.activeElement)");

        await page.Keyboard.PressAsync("Alt");

        await Expect(Ribbon(page, "Stamps")).ToHaveAttributeAsync("data-key-tips", "tabs");
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "tabs");
        await page.Keyboard.PressAsync("Escape");
        await Expect(Ribbon(page, "Stamps")).Not.ToHaveAttributeAsync("data-key-tips", "tabs");
        // The dialog would tell the app it closed only once its closing had played.
        await page.WaitForTimeoutAsync(500);
        await Expect(page.GetByTestId("dialog")).ToHaveTextAsync("open");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldGoToTheRibbonWithFocus_WhenThePageHasTwo(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Expect(Ribbon(page, "Atlas")).ToHaveAttributeAsync("data-ready", "");
        await Tab(page, "Maps").FocusAsync();

        await page.Keyboard.PressAsync("Alt");

        await Expect(Ribbon(page, "Atlas")).ToHaveAttributeAsync("data-key-tips", "tabs");
        Assert.Equal(["A"], await ShownAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldGoToTheFirstRibbonOnThePage_WhenALaterOneWasThereBeforeIt(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Button(page, "Add a ribbon first").ClickAsync();
        await Expect(Ribbon(page, "Early")).ToHaveAttributeAsync("data-ready", "");
        await Notes(page).FocusAsync();

        await page.Keyboard.PressAsync("Alt");

        await Expect(Ribbon(page, "Early")).ToHaveAttributeAsync("data-key-tips", "tabs");
        Assert.Equal(["D"], await ShownAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldPassOverARibbon_WhenItIsInert(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Expect(Ribbon(page, "Atlas")).ToHaveAttributeAsync("data-ready", "");
        // A modal dialog that isn't in the top layer makes the page around it inert in this way.
        await page.GetByTestId("journal").EvaluateAsync("box => { box.inert = true; }");

        await page.Keyboard.PressAsync("Alt");

        await Expect(Ribbon(page, "Atlas")).ToHaveAttributeAsync("data-key-tips", "tabs");
        Assert.Equal(["A"], await ShownAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldLeaveEscapeToAnOpenGroupFirst_WhenACollapsedGroupIsOpen(string browser)
    {
        var page = await OpenAtCommandsAsync(browser, "?narrow=true");
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=G]")).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("g");
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=N]")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        // The group closed and took the focus, and the key tips are still at the tab's commands.
        await Expect(Button(page, "New entry")).ToBeHiddenAsync();
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=G]")).ToBeFocusedAsync();
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "commands");
        await page.Keyboard.PressAsync("Escape");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");
        await Expect(Tab(page, "Home")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldHideAndLeaveTheKeyAlone_WhenItIsPressedWithCtrlOrAlt(string browser)
    {
        var page = await OpenAtCommandsAsync(browser);
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=B]")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Control+b");

        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "commands");
        await Expect(page.GetByTestId("bold")).ToHaveTextAsync("plain");
        await Notes(page).FocusAsync();
        await page.Keyboard.PressAsync("Alt");
        await page.Keyboard.PressAsync("h");
        await Expect(page.Locator("[data-key-tip-shown][data-key-tip=B]")).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Alt+b");
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "commands");
        await Expect(page.GetByTestId("bold")).ToHaveTextAsync("plain");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTips_ShouldHide_WhenFocusOrTheWindowsFocusGoesElsewhere(string browser)
    {
        var page = await OpenAtCommandsAsync(browser);

        await Button(page, "Open the dialog").FocusAsync();

        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "commands");
        await page.Keyboard.PressAsync("Alt");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");
        await page.EvaluateAsync("() => { window.dispatchEvent(new Event('blur')); }");
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "tabs");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Alt_ShouldBeLeftToTheBrowser_WhenNoRibbonOnThePageHasKeyTips(string browser)
    {
        var page = await OpenAsync(browser, "/ribbon-tabs-only");
        await Expect(page.Locator("[data-part=ribbon]").First).ToHaveAttributeAsync("data-ready", "");
        const string watch = """
            () => {
                window.altTaken = [];
                for (const name of ['keydown', 'keyup']) {
                    window.addEventListener(name, event => window.altTaken.push(event.defaultPrevented));
                }
            }
            """;
        await page.EvaluateAsync(watch);

        await page.Keyboard.PressAsync("Alt");

        var left = await page.EvaluateAsync<bool[]>("() => window.altTaken");
        Assert.Equal([false, false], left);
        var tipped = await OpenReadyAsync(browser);
        await tipped.EvaluateAsync(watch);
        await tipped.Keyboard.PressAsync("Alt");
        var taken = await tipped.EvaluateAsync<bool[]>("() => window.altTaken");
        Assert.Equal([true, true], taken);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldPressAltAndEachKeyAndWaitForTheHandler_WhenGivenATabsKeyAndACommands(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.Tb().Ribbon("Journal").PressKeyTipsAsync("V", "B");

        Assert.Equal("Zoom in", await page.GetByTestId("last").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task KeyTipsExample_ShouldReachACommandThroughItsCollapsedGroup_WhenShownInItsNarrowBox(string browser)
    {
        var page = await OpenAsync(browser, "/examples/ribbon");
        var ribbon = page.Tb().Ribbon("River guide");

        await ribbon.PressKeyTipsAsync("R", "G", "T");

        await Expect(page.GetByText("Last command: Throw rope.")).ToBeVisibleAsync();
        await ribbon.PressKeyTipsAsync("R", "N");
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Name of the rapid" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldStartOverAndStopAfterACommand_WhenCalledAgainWithKeyTipsStillShowing(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var ribbon = page.Tb().Ribbon("Journal");
        await ribbon.PressKeyTipsAsync("H");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "commands");

        await ribbon.PressKeyTipsAsync("V", "B");

        Assert.Equal("Zoom in", await page.GetByTestId("last").TextContentAsync());
        var spent = await Assert.ThrowsAsync<InvalidOperationException>(() => ribbon.PressKeyTipsAsync("H", "N", "B"));
        Assert.Equal("Key tips end once a command is chosen, so \"B\" has nothing to reach.", spent.Message);
        Assert.Equal("New entry", await page.GetByTestId("last").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReachItsOwnRibbon_WhenAltWouldGoToAnotherOne(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Expect(Ribbon(page, "Atlas")).ToHaveAttributeAsync("data-ready", "");

        await page.Tb().Ribbon("Atlas").PressKeyTipsAsync("A", "L");

        Assert.Equal("Legend", await page.GetByTestId("last").TextContentAsync());
        await Expect(page.Locator("[data-part=ribbon][data-key-tips]")).ToHaveCountAsync(0);
        // The command gave focus back to the Atlas tab the helper had moved it to.
        await Expect(Tab(page, "Maps")).ToBeFocusedAsync();
        // Key tips left showing on the other ribbon are put away first.
        await Notes(page).FocusAsync();
        await page.Keyboard.PressAsync("Alt");
        await Expect(Ribbon(page)).ToHaveAttributeAsync("data-key-tips", "tabs");
        await page.Tb().Ribbon("Atlas").PressKeyTipsAsync("A");
        await Expect(Ribbon(page, "Atlas")).ToHaveAttributeAsync("data-key-tips", "commands");
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "tabs");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReachTheSelectedTabsCommands_WhenTheRibbonIsInsideATabsPanel(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Button(page, "Open the dialog").ClickAsync();
        var ribbon = page.Tb().Ribbon("Stamps");

        await ribbon.SelectAsync("Paper");

        Assert.Equal(["Shape"], await ribbon.GroupsAsync());
        await ribbon.ClickAsync("Fold");
        Assert.Equal("Fold", await page.GetByTestId("last").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldRefuseKeyTips_WhenGivenMoreThanOneKeyOrOnAStaticPage(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var wrong = await Assert.ThrowsAsync<ArgumentException>(
            () => page.Tb().Ribbon("Journal").PressKeyTipsAsync("H", "NB"));
        Assert.StartsWith("A key tip is one letter or digit, and \"NB\" isn't.", wrong.Message);
        await Expect(Ribbon(page)).Not.ToHaveAttributeAsync("data-key-tips", "tabs");

        var still = await OpenStaticAsync(browser, "/static-ribbon");
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(
            () => still.Tb().Ribbon("Map tools").PressKeyTipsAsync("H"));
        Assert.Equal("On a static page the ribbon has no key tips; render the page interactively to use them.",
            refused.Message);
    }
}
