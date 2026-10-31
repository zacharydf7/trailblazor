using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class TabsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
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
}
