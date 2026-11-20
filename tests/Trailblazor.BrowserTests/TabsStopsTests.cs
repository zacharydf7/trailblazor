namespace Trailblazor.BrowserTests;

public class TabsStopsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldGoFromTheTabToThePanelsFirstControl_WhenThePanelStartsWithOne(string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Tab(page, "Gear").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(Field(page, "Pack")).ToBeFocusedAsync();
        await Expect(Panel(page, "Gear")).Not.ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldStopOnThePanel_WhenItStartsWithText(string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Tab(page, "Notes").ClickAsync();
        await Expect(Panel(page, "Notes")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(Panel(page, "Notes")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldGoToTheNestedSelectedTabAndThenItsPanel_WhenAPanelStartsWithNestedTabs(
        string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Tab(page, "Legs").ClickAsync();
        // The nested list's first tab isn't selected, so the arrow keys reach it and Tab doesn't.
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Legs" })).ToHaveAttributeAsync("data-ready", "");

        await page.Keyboard.PressAsync("Tab");
        await Expect(Tab(page, "South")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        // The nested panel starts with text, so it is a stop; the outer panel lost its stop only because it starts
        // with the nested tabs.
        await Expect(Panel(page, "South")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldTakeThePanelsStopAwayAndGiveItBack_WhenAControlTakesTheTextsPlaceAndGoesAgain(
        string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Tab(page, "Weather").ClickAsync();
        await Expect(Panel(page, "Weather")).ToHaveAttributeAsync("tabindex", "0");

        await page.GetByTestId("load").ClickAsync();
        await Expect(Field(page, "Forecast")).ToBeVisibleAsync();
        await Expect(Panel(page, "Weather")).Not.ToHaveAttributeAsync("tabindex", "0");
        await page.GetByTestId("load").ClickAsync();

        await Expect(Panel(page, "Weather")).ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepThePanelsStopAndItsFocus_WhenAControlArrivesWhileThePanelHasFocus(string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Tab(page, "Weather").ClickAsync();
        await Panel(page, "Weather").FocusAsync();

        // A click from script leaves focus on the panel.
        await page.GetByTestId("load").EvaluateAsync("button => button.click()");

        await Expect(Field(page, "Forecast")).ToBeVisibleAsync();
        await Expect(Panel(page, "Weather")).ToBeFocusedAsync();
        await Expect(Panel(page, "Weather")).ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldTakeThePanelsStopAway_WhenFocusLeavesAPanelThatKeptItsStopForIt(string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Tab(page, "Weather").ClickAsync();
        await Panel(page, "Weather").FocusAsync();
        await page.GetByTestId("load").EvaluateAsync("button => button.click()");
        await Expect(Field(page, "Forecast")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(Field(page, "Forecast")).ToBeFocusedAsync();
        await Expect(Panel(page, "Weather")).Not.ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldGiveThePanelItsStopBack_WhenItsFirstControlIsTurnedOff(string browser)
    {
        var page = await OpenStopsAsync(browser);
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Turn the pack field off or on" });
        await Expect(Panel(page, "Gear")).Not.ToHaveAttributeAsync("tabindex", "0");

        await toggle.ClickAsync();
        await Expect(Panel(page, "Gear")).ToHaveAttributeAsync("tabindex", "0");
        await toggle.ClickAsync();

        await Expect(Panel(page, "Gear")).Not.ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldGoToThePanelsFirstControlAgain_WhenAKeptPanelsTabIsSelectedAgain(string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Tab(page, "Notes").ClickAsync();
        await Expect(Panel(page, "Notes")).ToBeVisibleAsync();
        await Tab(page, "Gear").ClickAsync();
        await Expect(Panel(page, "Gear")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(Field(page, "Pack")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldGiveThePanelItsStopBack_WhenFocusablePanelsBecomesAlways(string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Expect(Panel(page, "Gear")).Not.ToHaveAttributeAsync("tabindex", "0");

        await page.GetByRole(AriaRole.Button, new() { Name = "Make every panel a stop" }).ClickAsync();

        await Expect(Panel(page, "Gear")).ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldGoFromTheTabToThePanelsFirstControl_WhenTheTabsWereSetUpWhileHidden(string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Hours", IncludeHidden = true }))
            .ToHaveAttributeAsync("data-ready", "");
        await Tab(page, "Tomorrow").ClickAsync();
        await Expect(Panel(page, "Morning")).Not.ToHaveAttributeAsync("tabindex", "0");
        await Tab(page, "Morning").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(Field(page, "Plan")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldLeaveThePanelNoStop_WhenFocusablePanelsBecomesNeverAndThePanelStartsWithText(
        string browser)
    {
        var page = await OpenStopsAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Make no panel a stop" }).ClickAsync();

        await Tab(page, "Notes").ClickAsync();

        await Expect(Panel(page, "Notes")).ToBeVisibleAsync();
        await Expect(Panel(page, "Notes")).Not.ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldDecideEachPanelsStopByWhatPeopleReadFirst_WhenPanelsStartInDifferentWays(
        string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Starts" })).ToHaveAttributeAsync("data-ready", "");
        // A legend and text an aria-labelledby points at name the control, and what is hidden, from everyone or from
        // screen readers, isn't read. An image with alt="" is decoration; one with alt text comes first. A menu's
        // button is a control though the menu is named by it, and the options before a list's selected one share
        // its tab stop. A list only script can focus holds text, and so does a wrapper with display: contents, which
        // has no box of its own.
        (string Tab, bool IsStop)[] panels =
        [
            ("Legend", false), ("Named", false), ("Decoration", false), ("Hidden", false), ("Spacer", false),
            ("Picture", true), ("Menu", false), ("Options", false), ("List", true), ("Wrapper", true),
        ];

        foreach (var (tab, isStop) in panels)
        {
            await Tab(page, tab).ClickAsync();
            await Expect(Panel(page, tab)).ToBeVisibleAsync();

            var stop = Expect(Panel(page, tab));
            await (isStop ? stop : stop.Not).ToHaveAttributeAsync("tabindex", "0");
        }
    }

    private async Task<IPage> OpenStaticStopsAsync(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-tabs-stops");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-tb-script", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldGoFromTheTabToThePanelsFirstControl_WhenThePageIsStaticAndThePanelStartsWithOne(
        string browser)
    {
        var page = await OpenStaticStopsAsync(browser);
        await page.GetByRole(AriaRole.Tab, new() { Name = "Gear" }).FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Add gear" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldGoFromTheTabToThePanelsFirstControl_WhenEnhancedNavigationBringsAStaticPagesTabs(
        string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-disclosure");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-tb-script", "");

        await page.GetByRole(AriaRole.Link, new() { Name = "Trip tabs" }).ClickAsync();
        // The startup script marks the root once it has handled the navigation.
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-tb-navigated", "");
        await page.GetByRole(AriaRole.Tab, new() { Name = "Gear" }).FocusAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Add gear" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldStopOnlyOnAPanelThatStartsWithText_WhenAStaticPagesTabsAreSwitched(string browser)
    {
        var page = await OpenStaticStopsAsync(browser);
        var gear = page.GetByRole(AriaRole.Tab, new() { Name = "Gear" });
        var route = page.GetByRole(AriaRole.Tab, new() { Name = "Route" });

        await route.ClickAsync();
        await route.FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Route" })).ToBeFocusedAsync();

        await gear.ClickAsync();
        await gear.FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Add gear" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldStopOnAPanelThatStartsWithAControl_WhenThePageIsStaticAndFocusablePanelsIsAlways(
        string browser)
    {
        var page = await OpenStaticStopsAsync(browser);
        await page.GetByRole(AriaRole.Tab, new() { Name = "Meals" }).FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Meals" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldPassOverAPanelThatStartsWithText_WhenThePageIsStaticAndFocusablePanelsIsNever(
        string browser)
    {
        var page = await OpenStaticStopsAsync(browser);
        await page.GetByRole(AriaRole.Tab, new() { Name = "Notes" }).FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Past the tabs" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldHaveDecidedThePanelsStop_WhenTheListIsMarkedReady(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-stops");
        // Reads the shown panel's stop as its list is marked ready, before a later frame could decide it.
        await page.AddInitScriptAsync("""
            new MutationObserver(records => {
                const list = records.map(record => record.target)
                    .find(target => target.getAttribute('aria-label') === 'Trip');
                if (list) {
                    const panel = list.parentElement.querySelector(':scope > [role=tabpanel]:not([hidden])');
                    window.stopWhenReady = panel.hasAttribute('tabindex') ? 'a stop' : 'no stop';
                }
            }).observe(document, { subtree: true, attributes: true, attributeFilter: ['data-ready'] });
            """);

        await page.ReloadAsync();

        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Trip" })).ToHaveAttributeAsync("data-ready", "");
        Assert.Equal("no stop", await page.EvaluateAsync<string>("() => window.stopWhenReady"));
    }

    // The script decides the shown panel's stop before it marks the list ready.
    private async Task<IPage> OpenStopsAsync(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-stops");
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Trip" })).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    private static ILocator Tab(IPage page, string name) =>
        page.GetByRole(AriaRole.Tab, new() { Name = name, Exact = true });

    private static ILocator Panel(IPage page, string name) =>
        page.GetByRole(AriaRole.Tabpanel, new() { Name = name, Exact = true });

    private static ILocator Field(IPage page, string name) =>
        page.GetByRole(AriaRole.Textbox, new() { Name = name, Exact = true });
}
