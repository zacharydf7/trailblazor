using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TabsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldTakeTheIndicatorSizeFromAroundThem_WhenThePageSetsIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");
        var selected = page.GetByRole(AriaRole.Tab, new() { Name = "Summary" });

        await page.EvaluateAsync(
            "() => document.querySelector('main').style.setProperty('--tb-tabs-indicator-size', '5px')");

        await Expect(selected).ToHaveCSSAsync("box-shadow", new Regex("-5px"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldSelectTheTab_WhenGivenItsNameAndTheTabsName(string browser)
    {
        // Not waiting for the page to be interactive, which the helper waits for itself.
        var page = await OpenStaticAsync(browser, "/examples/tabs");

        await page.Tb().Tabs("Trail settings").SelectAsync("Alerts");

        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Alerts" }))
            .ToHaveTextAsync("Tell me about closures and fire danger.");
    }

    private static ILocator Tab(IPage page, string name) =>
        page.GetByRole(AriaRole.Tab, new() { Name = name, Exact = true });

    // The arrow keys work once the tabs script has attached, which is when data-ready appears.
    private static async Task WaitForKeysAsync(IPage page, string list) =>
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = list })).ToHaveAttributeAsync("data-ready", "");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldUnderlineOnlyTheSelectedTabWithoutMovingAnything_WhenColorsAreForced(string browser)
    {
        var plain = await OpenAsync(browser, "/examples/tabs");
        var sections = new PageGetByRoleOptions { Name = "Report sections" };
        var height = (await plain.GetByRole(AriaRole.Tablist, sections).BoundingBoxAsync())!.Height;
        var page = await OpenAsync(browser, "/examples/tabs", new() { ForcedColors = ForcedColors.Active });

        var underlines = await page.GetByRole(AriaRole.Tablist, sections).EvaluateAsync<string[]>("""
            list => {
                const probe = document.body.appendChild(document.createElement('div'));
                probe.style.cssText = 'forced-color-adjust: none; color: Highlight';
                const highlight = getComputedStyle(probe).color;
                return [...list.querySelectorAll('[role=tab]')].map(tab => {
                    const style = getComputedStyle(tab);
                    return style.borderBottomWidth === '0px' ? 'none'
                        : style.borderBottomColor === highlight ? 'highlight' : style.borderBottomColor;
                });
            }
            """);

        Assert.Equal(["highlight", "none", "none"], underlines);
        Assert.Equal(height, (await page.GetByRole(AriaRole.Tablist, sections).BoundingBoxAsync())!.Height,
            tolerance: 0.5);
        // The list scrolls sideways, so it also clips what sticks out below it; the underline must stay inside.
        Assert.True(await page.GetByRole(AriaRole.Tablist, sections).EvaluateAsync<bool>(
            "list => list.scrollHeight <= list.clientHeight"), "The underline is clipped below the tab list.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldCloseTheFocusedTabAndFocusTheNext_WhenDeleteOrBackspaceIsPressedAlone(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");
        await WaitForKeysAsync(page, "Open trails");
        var list = page.GetByRole(AriaRole.Tablist, new() { Name = "Open trails" });
        await list.GetByRole(AriaRole.Tab, new() { Name = "Ridge loop", Exact = true }).FocusAsync();

        // Had Ctrl+Delete closed Ridge loop, Delete would then close Lake shore.
        await page.Keyboard.PressAsync("Control+Delete");
        await page.Keyboard.PressAsync("Delete");

        await Expect(list.GetByRole(AriaRole.Tab)).ToHaveTextAsync(["All trails", "Lake shore", "Cedar flats"]);
        await Expect(list.GetByRole(AriaRole.Tab, new() { Name = "Lake shore", Exact = true })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Backspace");

        await Expect(list.GetByRole(AriaRole.Tab)).ToHaveTextAsync(["All trails", "Cedar flats"]);
        await Expect(list.GetByRole(AriaRole.Tab, new() { Name = "Cedar flats", Exact = true })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldCloseOneTab_WhenDeleteIsHeld(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");
        await WaitForKeysAsync(page, "Open trails");
        var list = page.GetByRole(AriaRole.Tablist, new() { Name = "Open trails" });
        await list.GetByRole(AriaRole.Tab, new() { Name = "Ridge loop", Exact = true }).FocusAsync();

        await page.Keyboard.DownAsync("Delete");
        await Expect(list.GetByRole(AriaRole.Tab, new() { Name = "Lake shore", Exact = true })).ToBeFocusedAsync();
        // A second press while the key is still down is a repeat.
        await page.Keyboard.DownAsync("Delete");
        await page.Keyboard.UpAsync("Delete");
        await page.WaitForTimeoutAsync(500);

        await Expect(list.GetByRole(AriaRole.Tab)).ToHaveTextAsync(["All trails", "Lake shore", "Cedar flats"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldCloseATabWithoutSelectingIt_WhenItsCloseMarkIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");
        var list = page.GetByRole(AriaRole.Tablist, new() { Name = "Open trails" });
        await WaitForKeysAsync(page, "Open trails");
        var mark = (await list.Locator("[data-part=close]").First.BoundingBoxAsync())!;
        var plain = (await list.GetByRole(AriaRole.Tab, new() { Name = "All trails" }).BoundingBoxAsync())!;
        var closable = (await list.GetByRole(AriaRole.Tab, new() { Name = "Ridge loop" }).BoundingBoxAsync())!;
        Assert.Equal((24, 24), (mark.Width, mark.Height));
        Assert.Equal(plain.Height, closable.Height, tolerance: 0.5);

        await page.Tb().Tabs("Open trails").CloseAsync("Cedar flats");

        await Expect(list.GetByRole(AriaRole.Tab)).ToHaveTextAsync(["All trails", "Ridge loop", "Lake shore"]);
        await Expect(list.GetByRole(AriaRole.Tab, new() { Name = "Ridge loop", Exact = true }))
            .ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepTheAddButtonInViewAtTheRowsEnd_WhenTheListScrolls(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-add");
        var list = page.GetByRole(AriaRole.Tablist, new() { Name = "Narrow" });
        var tabs = list.Locator("xpath=..");
        var add = tabs.Locator(":scope > .tb-tabs__actions > [data-part=add]");

        var row = (await list.BoundingBoxAsync())!;
        var button = (await add.BoundingBoxAsync())!;
        var whole = (await tabs.BoundingBoxAsync())!;
        Assert.True(await list.EvaluateAsync<bool>("list => list.scrollWidth > list.clientWidth"), "The list fits.");
        Assert.True(button.X >= row.X + row.Width - 1, "The add button isn't after the list.");
        Assert.True(button.X + button.Width <= whole.X + whole.Width + 0.5, "The add button is cut off.");
        Assert.InRange(button.Y + button.Height / 2, row.Y, row.Y + row.Height);
        await page.Tb().Tabs("Narrow").AddAsync();
        await Expect(page.GetByTestId("added")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldDrawNoLineUnderTheAddButton_WhenTheTabsArePills(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-add");
        const string borders = "cell => [getComputedStyle(cell).borderBottomWidth, "
            + "getComputedStyle(cell).borderInlineEndWidth]";

        foreach (var name in new[] { "Pills", "Pill column" })
        {
            var actions = page.GetByRole(AriaRole.Tablist, new() { Name = name, Exact = true }).Locator("xpath=..")
                .Locator(":scope > .tb-tabs__actions");
            Assert.Equal(["0px", "0px"], await actions.EvaluateAsync<string[]>(borders));
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldPutTheAddButtonUnderTheTabs_WhenTheListIsVertical(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-add");
        var list = page.GetByRole(AriaRole.Tablist, new() { Name = "Column", Exact = true });
        var tabs = list.Locator("xpath=..");

        var column = (await list.BoundingBoxAsync())!;
        var button = (await tabs.Locator(":scope > .tb-tabs__actions > [data-part=add]").BoundingBoxAsync())!;
        var panel = (await tabs.GetByRole(AriaRole.Tabpanel).BoundingBoxAsync())!;
        Assert.True(button.Y >= column.Y + column.Height - 1, "The add button isn't under the tabs.");
        Assert.True(button.X < column.X + column.Width, "The add button isn't in the tabs' column.");
        Assert.Equal(column.X + column.Width, panel.X, tolerance: 1);
        Assert.Equal(column.Y, panel.Y, tolerance: 1);

        // With no panel beside the column, the button still sits under the tabs.
        var bare = page.GetByRole(AriaRole.Tablist, new() { Name = "Bare column" });
        var bareColumn = (await bare.BoundingBoxAsync())!;
        var bareButton = (await bare.Locator("xpath=..").Locator(":scope > .tb-tabs__actions > [data-part=add]")
            .BoundingBoxAsync())!;
        Assert.True(bareButton.Y >= bareColumn.Y + bareColumn.Height - 1,
            "The add button isn't under the bare column.");
        Assert.True(bareButton.X < bareColumn.X + bareColumn.Width, "The add button isn't in the bare column.");
    }

    // A row's scroll buttons, by which end of the row they scroll toward.
    private static (ILocator Start, ILocator End) Scrollers(ILocator list) =>
        (list.Locator("xpath=..").Locator(":scope > .tb-tabs__scroll > [data-part=scroll-start]"),
            list.Locator("xpath=..").Locator(":scope > .tb-tabs__scroll > [data-part=scroll-end]"));

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldScrollTheRowAndTurnEachButtonOffAtItsEnd_WhenItsScrollButtonsAreClicked(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-scroll");
        foreach (var name in new[] { "Long", "Flipped" })
        {
            var list = page.GetByRole(AriaRole.Tablist, new() { Name = name });
            var (start, end) = Scrollers(list);
            await Expect(start).ToBeDisabledAsync();
            await Expect(end).ToBeEnabledAsync();
            await list.GetByRole(AriaRole.Tab).First.FocusAsync();

            await end.ClickAsync();
            await Expect(start).ToBeEnabledAsync();
            // Right-to-left rows scroll to negative positions.
            Assert.True(await list.EvaluateAsync<double>("list => Math.abs(list.scrollLeft)") > 0,
                $"{name} didn't scroll.");
            await Expect(list.GetByRole(AriaRole.Tab).First).ToBeFocusedAsync();
            // Forced, since the button can turn off between the check and the click as a smooth scroll ends.
            for (var click = 0; click < 10 && await end.IsEnabledAsync(); click++)
            {
                await end.ClickAsync(new() { Force = true });
                await page.WaitForTimeoutAsync(300);
            }

            await Expect(end).ToBeDisabledAsync();
            Assert.True(await list.EvaluateAsync<bool>(
                "list => Math.abs(list.scrollLeft) >= list.scrollWidth - list.clientWidth - 1"),
                $"{name} isn't at its end.");
            await start.ClickAsync();
            await Expect(end).ToBeEnabledAsync();
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldHideTheScrollButtons_WhenTheRowFits(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-scroll");
        await Expect(Scrollers(page.GetByRole(AriaRole.Tablist, new() { Name = "Long" })).End).ToBeVisibleAsync();

        var (start, end) = Scrollers(page.GetByRole(AriaRole.Tablist, new() { Name = "Short" }));

        await Expect(start).ToBeHiddenAsync();
        await Expect(end).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowTheScrollButtons_WhenTurnedOnLaterOrTheRowStartsToOverflow(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-scroll");
        await WaitForKeysAsync(page, "Late buttons");
        await WaitForKeysAsync(page, "Growing");
        var late = Scrollers(page.GetByRole(AriaRole.Tablist, new() { Name = "Late buttons" }));
        var growing = Scrollers(page.GetByRole(AriaRole.Tablist, new() { Name = "Growing" }));
        await Expect(growing.End).ToBeHiddenAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Show scroll buttons" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Grow the label" }).ClickAsync();

        foreach (var (start, end) in new[] { late, growing })
        {
            await Expect(end).ToBeVisibleAsync();
            await Expect(end).ToBeEnabledAsync();
            await Expect(start).ToBeDisabledAsync();
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldClickTheAddButton_WhenScrollButtonsShowBeforeIt(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-scroll");
        await Expect(Scrollers(page.GetByRole(AriaRole.Tablist, new() { Name = "Adding" })).End).ToBeVisibleAsync();

        await page.Tb().Tabs("Adding").AddAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepTheScrollButtonsAtTheRowsEndsBeforeTheAddButton_WhenBothShow(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-scroll");
        var list = page.GetByRole(AriaRole.Tablist, new() { Name = "Adding" });
        var (start, end) = Scrollers(list);
        await Expect(end).ToBeVisibleAsync();
        var add = list.Locator("xpath=..").Locator(":scope > .tb-tabs__actions > [data-part=add]");

        var order = new[] { start, list, end, add };
        var boxes = new List<LocatorBoundingBoxResult>();
        foreach (var part in order)
        {
            boxes.Add((await part.BoundingBoxAsync())!);
        }

        for (var index = 1; index < boxes.Count; index++)
        {
            Assert.True(boxes[index].X >= boxes[index - 1].X + boxes[index - 1].Width - 1,
                $"Part {index} is out of order.");
            Assert.InRange(boxes[index].Y + boxes[index].Height / 2, boxes[1].Y, boxes[1].Y + boxes[1].Height);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldLeaveTheKeysToTheBrowser_WhenAltCtrlOrCmdIsHeld(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Product");
        await Tab(page, "Overview").FocusAsync();

        foreach (var key in new[] { "Alt+ArrowRight", "Control+ArrowRight", "Meta+ArrowRight", "Control+End" })
        {
            await page.Keyboard.PressAsync(key);
            await Expect(Tab(page, "Overview")).ToBeFocusedAsync();
        }

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Reviews")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldMoveFocusAndSelectSkippingDisabledTabs_WhenArrowKeysArePressed(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Product");
        await Tab(page, "Overview").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Reviews")).ToBeFocusedAsync();
        await Expect(Tab(page, "Reviews")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Reviews" })).ToHaveTextAsync("Reviews panel");
        await Expect(page.GetByTestId("section")).ToHaveTextAsync("2");

        await page.Keyboard.PressAsync("End");
        await Expect(Tab(page, "Pricing")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Overview")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tab(page, "Pricing")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(Tab(page, "Overview")).ToBeFocusedAsync();
        await Expect(page.GetByTestId("section")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldMoveForwardWithTheLeftArrow_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Product");
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        await Tab(page, "Overview").FocusAsync();

        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(Tab(page, "Reviews")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldOnlyMoveFocusUntilEnter_WhenActivationIsManual(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Account");
        await Tab(page, "Profile").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Security")).ToBeFocusedAsync();
        await Expect(Tab(page, "Profile")).ToHaveAttributeAsync("aria-selected", "true");

        await page.Keyboard.PressAsync("Enter");
        await Expect(Tab(page, "Security")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Security" })).ToHaveTextAsync("Security panel");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepAPanelsState_WhenKeepAliveIsSetAndTheUserSwitchesAway(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await page.GetByLabel("Bio").FillAsync("likes hiking");

        await Tab(page, "Security").ClickAsync();
        await Expect(page.GetByLabel("Bio")).ToBeHiddenAsync();
        await Tab(page, "Profile").ClickAsync();

        await Expect(page.GetByLabel("Bio")).ToHaveValueAsync("likes hiking");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldMoveFocusIntoThePanel_WhenTabIsPressedOnTheSelectedTab(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await Tab(page, "Overview").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Overview" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldListALateTabInMarkupOrderAndKeepTheSelection_WhenItRendersAfterOthers(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Reports");
        await Tab(page, "Monthly").ClickAsync();
        await Expect(page.GetByTestId("report")).ToHaveTextAsync("1");

        await page.GetByRole(AriaRole.Button, new() { Name = "Add weekly" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Reports" }).GetByRole(AriaRole.Tab))
            .ToHaveTextAsync(["Daily", "Weekly", "Monthly"]);
        await Expect(page.GetByTestId("report")).ToHaveTextAsync("2");
        await Expect(Tab(page, "Monthly")).ToHaveAttributeAsync("aria-selected", "true");
        await Tab(page, "Daily").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Weekly")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepTheSelectedTab_WhenTabsAreSortedAfterTheParentOpenedANewLastTab(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Regions");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open south" }).ClickAsync();
        await Expect(Tab(page, "South")).ToHaveAttributeAsync("aria-selected", "true");

        await page.GetByRole(AriaRole.Button, new() { Name = "Sort regions" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Regions" }).GetByRole(AriaRole.Tab))
            .ToHaveTextAsync(["North", "South", "West"]);
        await Expect(page.GetByTestId("region")).ToHaveTextAsync("1");
        await Expect(Tab(page, "South")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepFocusOnTheSameTab_WhenALateTabRendersBeforeIt(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Plans");
        await Tab(page, "Basic").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Plans" }).GetByRole(AriaRole.Tab))
            .ToHaveTextAsync(["Basic", "Plus", "Pro"]);
        await Expect(Tab(page, "Pro")).ToBeFocusedAsync();
        await Expect(Tab(page, "Pro")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldSelectTheTabTheParentChose_WhenItAddsTheTabAndSetsTheIndexTogether(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Reports");

        await page.GetByRole(AriaRole.Button, new() { Name = "Open weekly" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Reports" }).GetByRole(AriaRole.Tab))
            .ToHaveTextAsync(["Daily", "Weekly", "Monthly"]);
        await Expect(Tab(page, "Weekly")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Weekly" })).ToHaveTextAsync("Weekly panel");
        await Expect(page.GetByTestId("report")).ToHaveTextAsync("1");
        // Kept alive, Monthly would stay in the page had it been selected while the order was on its way.
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Monthly", IncludeHidden = true }))
            .ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldMoveWithUpAndDownButNotLeftAndRight_WhenVertical(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");
        await WaitForKeysAsync(page, "Trail settings");
        await Tab(page, "Route").FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Tab(page, "Alerts")).ToBeFocusedAsync();
        await Expect(Tab(page, "Alerts")).ToHaveAttributeAsync("aria-selected", "true");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(Tab(page, "Route")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(Tab(page, "Filters")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Tab(page, "Filters")).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Filters" }))
            .ToHaveTextAsync("Dogs allowed, no bikes.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldStandBesideTheirPanel_WhenVertical(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");

        var list = (await page.GetByRole(AriaRole.Tablist, new() { Name = "Trail settings" }).BoundingBoxAsync())!;
        var panel = (await page.GetByRole(AriaRole.Tabpanel, new() { Name = "Route" }).BoundingBoxAsync())!;

        Assert.True(panel.X >= list.X + list.Width - 0.5, $"The panel starts at {panel.X}, the list ends at "
            + $"{list.X + list.Width}.");
        Assert.True(panel.Y < list.Y + list.Height, "The panel should sit beside the list, not below it.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldMarkTheSelectedTabOnItsInnerEdge_WhenVerticalAndColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs", new() { ForcedColors = ForcedColors.Active });

        var list = page.GetByRole(AriaRole.Tablist, new() { Name = "Trail settings" });

        var lines = await list.EvaluateAsync<string[]>("""
            list => {
                const probe = document.body.appendChild(document.createElement('div'));
                probe.style.cssText = 'forced-color-adjust: none; color: Highlight';
                const highlight = getComputedStyle(probe).color;
                return [...list.querySelectorAll('[role=tab]')].map(tab => {
                    const style = getComputedStyle(tab);
                    return style.borderBottomWidth !== '0px' ? 'bottom'
                        : style.borderRightWidth === '0px' ? 'none'
                        : style.borderRightColor === highlight ? 'highlight' : style.borderRightColor;
                });
            }
            """);

        Assert.Equal(["highlight", "none", "none"], lines);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepTheTabWithTheKey_WhenATabAppearsBeforeIt(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Trips");
        await Tab(page, "Multi-day").ClickAsync();
        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("multi");

        await page.GetByRole(AriaRole.Button, new() { Name = "Add overnight" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Trips" }).GetByRole(AriaRole.Tab))
            .ToHaveTextAsync(["Day hikes", "Overnight", "Multi-day"]);
        await Expect(Tab(page, "Multi-day")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Multi-day" }))
            .ToHaveTextAsync("Multi-day panel");
        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("multi");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldSelectTheParentsTab_WhenItAddsTheTabAndSetsItsKeyTogether(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Trips");

        await page.GetByRole(AriaRole.Button, new() { Name = "Open overnight" }).ClickAsync();

        await Expect(Tab(page, "Overnight")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Overnight" }))
            .ToHaveTextAsync("Overnight panel");
        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("overnight");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldNameEachTabByItsHeader_WhenHeadersHoldAnIconOrACount(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");

        await Expect(Tab(page, "Details 3")).ToBeVisibleAsync();
        await Expect(Tab(page, "Route")).ToBeVisibleAsync();
        await Expect(Tab(page, "Route").Locator("[data-part=icon]")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldMarkTheSelectedTabWithAFillAndNoLine_WhenTheVariantIsSoft(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");
        var layers = page.GetByRole(AriaRole.Tablist, new() { Name = "Map layers" });
        var selected = layers.GetByRole(AriaRole.Tab, new() { Name = "Terrain" });
        var other = layers.GetByRole(AriaRole.Tab, new() { Name = "Satellite" });

        await Expect(selected).Not.ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(selected).ToHaveCSSAsync("box-shadow", NoLine);
        await Expect(other).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldFillTheSelectedTabWithNoLine_WhenAVerticalListIsSoft(string browser)
    {
        var page = await OpenAsync(browser, "/tab-variants");
        var selected = page.GetByRole(AriaRole.Tab, new() { Name = "Packs" });

        await Expect(selected).Not.ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(selected).ToHaveCSSAsync("box-shadow", NoLine);
    }

    // A tab with no line has no shadow, or the see-through one it keeps for the line to fade in.
    private static readonly Regex NoLine = new(@"^(none|rgba\(0, 0, 0, 0\).*)$");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldCheckEveryPanelsFieldsAndNameTheTabsToFix_WhenRenderAllPanelsIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");
        var plan = page.GetByRole(AriaRole.Tablist, new() { Name = "Trip plan" });
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save trip" });

        await page.GetByLabel("Leader's name").FillAsync("Ada");
        await save.ClickAsync();
        await plan.GetByRole(AriaRole.Tab, new() { Name = "Itinerary (needs attention)" }).ClickAsync();
        await Expect(page.GetByText("Saved")).ToHaveCountAsync(0);
        var trail = page.GetByRole(AriaRole.Tabpanel, new() { Name = "Itinerary" }).GetByRole(AriaRole.Textbox);
        await trail.FillAsync("Pine Hollow");
        // Committing the trail clears its message and moves Save, so clicking Save before it's committed can miss.
        await trail.PressAsync("Tab");
        await Expect(plan.GetByRole(AriaRole.Tab, new() { Name = "Itinerary", Exact = true })).ToBeVisibleAsync();
        await save.ClickAsync();

        await Expect(page.GetByText("Saved Ada's trip on Pine Hollow.")).ToBeVisibleAsync();
        await Expect(plan.GetByRole(AriaRole.Tab, new() { Name = "Leader", Exact = true })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldRaiseItsOnClickForClicksButNotArrowKeys_WhenItsHeaderIsClickedEvenWhileSelected(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");
        var tabs = page.GetByRole(AriaRole.Tablist, new() { Name = "Trail map" });
        var map = tabs.GetByRole(AriaRole.Tab, new() { Name = "Map" });
        await Expect(tabs).ToHaveAttributeAsync("data-ready", "");

        await map.ClickAsync();
        await map.ClickAsync();
        await map.PressAsync("Enter");
        await map.PressAsync("ArrowLeft");
        await Expect(tabs.GetByRole(AriaRole.Tab, new() { Name = "Overview" })).ToHaveAttributeAsync("aria-selected",
            "true");
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(map).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByText("Map refreshed 3 times.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldSwitchByClickOrArrowKey_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-disclosure");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var details = page.GetByRole(AriaRole.Tab, new() { Name = "Details" });
        var notes = page.GetByRole(AriaRole.Tab, new() { Name = "Notes" });

        await details.ClickAsync();

        await Expect(details).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByText("Every order placed.")).ToBeVisibleAsync();
        await Expect(page.GetByText("Totals for the quarter.")).ToBeHiddenAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(notes).ToBeFocusedAsync();
        await Expect(notes).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByText("Refunds were up in March.")).ToBeVisibleAsync();
        await Expect(page.GetByText("Every order placed.")).ToBeHiddenAsync();
        await Expect(page.Locator("[role=tabpanel]", new() { HasText = "Last year's reports." }))
            .ToHaveAttributeAsync("hidden", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldTabFromThePanelOutOfTheTabs_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-disclosure");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var details = page.GetByRole(AriaRole.Tab, new() { Name = "Details" });
        var past = page.GetByRole(AriaRole.Button, new() { Name = "Past the tabs" });
        await page.GetByRole(AriaRole.Tab, new() { Name = "Summary" }).FocusAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Summary" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(past).ToBeFocusedAsync();

        await details.ClickAsync();
        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Tab");
        await Expect(past).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(details).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldSelectTheTabHoldingTheText_WhenTheBrowserFindsItOnAStaticPage(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-disclosure#:~:text=Refunds");

        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" }))
            .ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByText("Refunds were up in March.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TabList_ShouldKeepItsHeight_WhenAFixedHeightLeavesThePanelTooLittleRoom(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-fill");

        // A list or tab squeezed shorter than its content clips it, which shows as content taller than the box.
        foreach (var tabs in new[] { "Flex column", "With an add button", "With scroll buttons" })
        {
            var clipped = await page.GetByRole(AriaRole.Tablist, new() { Name = tabs }).EvaluateAsync<string[]>("""
                list => [list, list.querySelector('[role=tab]')]
                    .filter(box => box.scrollHeight > box.clientHeight)
                    .map(box => `${box.getAttribute('role')} ${box.clientHeight}px of ${box.scrollHeight}px`)
                """);

            Assert.True(clipped.Length == 0, $"{tabs}: {string.Join(", ", clipped)}.");
        }
    }
}
