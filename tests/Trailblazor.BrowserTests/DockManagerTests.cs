using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockManagerTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldPutEachPanelAfterItsGroupsTabs_WhenAPanelChangesGroup(string browser)
    {
        var page = await OpenDockAsync(browser);
        Assert.Equal("group trails separator grip group notes separator grip group weather", await PartsAsync(page));

        await page.GetByRole(AriaRole.Button, new() { Name = "Move weather" }).ClickAsync();
        await page.WaitForFunctionAsync($"() => ({Parts})() === 'group trails weather separator grip group notes'");

        // Shift+Tab from the second group's tabs passes the separator and lands in the weather, now in the first.
        await Tab(page, "Notes").FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Separator(page, "Resize Weather")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Panel(page, "Weather").Locator(":focus")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldKeepAPanelsTextScrollAndFocus_WhenThePanelChangesGroup(string browser)
    {
        var page = await OpenDockAsync(browser);
        var city = page.FrameLocator("iframe[title=Forecast]").GetByLabel("City");
        await page.GetByLabel("Trail name").FillAsync("Ridge loop");
        await city.FillAsync("Leeds");
        await page.GetByTestId("days").EvaluateAsync("days => days.scrollTop = 100");
        var refresh = page.GetByRole(AriaRole.Button, new() { Name = "Refresh" });
        await refresh.FocusAsync();
        // Set after the focus, which scrolls the panel to its button.
        await Panel(page, "Weather").EvaluateAsync("panel => panel.scrollTop = 40");

        // A click from script leaves the focus where it is.
        await page.GetByRole(AriaRole.Button, new() { Name = "Move weather" }).EvaluateAsync("button => button.click()");
        await page.WaitForFunctionAsync($"() => ({Parts})() === 'group trails weather separator grip group notes'");

        await Expect(refresh).ToBeFocusedAsync();
        Assert.Equal(100, await page.GetByTestId("days").EvaluateAsync<int>("days => days.scrollTop"));
        Assert.Equal(40, await Panel(page, "Weather").EvaluateAsync<int>("panel => panel.scrollTop"));
        // The trails didn't move; their panel only went behind the weather.
        await Tab(page, "Trails").ClickAsync();
        await Expect(page.GetByLabel("Trail name")).ToHaveValueAsync("Ridge loop");
        // The weather's panel holds an iframe, so the other parts moved around it and what was typed in it is still
        // there, with or without moveBefore.
        await Tab(page, "Weather").ClickAsync();
        await Expect(city).ToHaveValueAsync("Leeds");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMoveBetweenAGroupsTabsAndShowEach_WhenTheArrowKeysAreUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        await Tab(page, "Trails").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Map")).ToBeFocusedAsync();
        await Expect(Tab(page, "Map")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Zoom in" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Add trail" })).ToBeHiddenAsync();

        // The keys wrap within the group and never leave it.
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(Tab(page, "Map")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();
        await Expect(Tab(page, "Trails")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldLeaveTheKeysToTabsInsideAPanel_WhenTheyAreUsedThere(string browser)
    {
        var page = await OpenDockAsync(browser);
        await Tab(page, "Map").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Layers" })).ToHaveAttributeAsync("data-ready", "");
        await Tab(page, "Roads").FocusAsync();

        // One press moves one tab: the dock doesn't take the inner tabs' keys for its own.
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Terrain")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Delete");
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Tab(page, "Satellite")).ToBeFocusedAsync();
        await Expect(Tab(page, "Map")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("\"closed\":[]");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldLeaveTheKeysToASplitterInsideAPanel_WhenTheyAreUsedThere(string browser)
    {
        var page = await OpenDockAsync(browser);
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        await Tab(page, "Map").ClickAsync();

        await page.Tb().Splitter("Resize Legend").ResizeAsync(45);

        Assert.Equal(45, await page.Tb().Splitter("Resize Legend").SizeAsync());
        // The dock's script took none of those keys for a separator of its own, and the dock heard only the click.
        Assert.Empty(errors);
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-commits", "1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldKeepAHiddenPanelOutOfReachAndAtItsScrollPosition_WhenAnotherTabShows(string browser)
    {
        var page = await OpenDockAsync(browser);
        await Panel(page, "Notes").EvaluateAsync("panel => panel.scrollTop = 200");

        await Tab(page, "Gear").ClickAsync();
        await Expect(page.GetByLabel("Note", new() { Exact = true })).ToBeHiddenAsync();
        // Tab goes from the tabs to the group's menu, the showing panel and its button, and then to the separator,
        // never into the notes.
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Options for Gear" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(Panel(page, "Gear")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Add gear" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(Separator(page, "Resize Gear")).ToBeFocusedAsync();

        await Tab(page, "Notes").ClickAsync();
        await Expect(page.GetByLabel("Note", new() { Exact = true })).ToBeVisibleAsync();
        Assert.Equal(200, await Panel(page, "Notes").EvaluateAsync<int>("panel => panel.scrollTop"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldCloseATabAndFocusItsNeighbor_WhenDeleteIsPressed(string browser)
    {
        var page = await OpenDockAsync(browser);
        await Tab(page, "Notes").FocusAsync();

        await page.Keyboard.PressAsync("Delete");
        await Expect(Tab(page, "Notes")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("\"closed\":[\"notes\"]");

        // With its last tab closed the group goes, and the next group's showing tab takes the focus.
        await page.Keyboard.PressAsync("Backspace");
        await Expect(Tab(page, "Gear")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Weather")).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.Tablist)).ToHaveCountAsync(2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldLeaveTheFocusInAField_WhenAnotherPanelsTabIsMiddleClicked(string browser)
    {
        var page = await OpenDockAsync(browser);
        var name = page.GetByLabel("Trail name");
        await name.FocusAsync();

        await Tab(page, "Gear").ClickAsync(new() { Button = MouseButton.Middle });
        await Expect(Tab(page, "Gear")).ToHaveCountAsync(0);
        await Expect(name).ToBeFocusedAsync();

        // The trails' own tab takes the field with it, so the focus goes to the tab beside it.
        await Tab(page, "Trails").ClickAsync(new() { Button = MouseButton.Middle });
        await Expect(Tab(page, "Trails")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Map")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldLeaveTheFocusInAField_WhenAnotherPanelsCloseMarkIsClicked(string browser)
    {
        var page = await OpenDockAsync(browser);
        var name = page.GetByLabel("Trail name");
        await name.FocusAsync();

        await Tab(page, "Gear").Locator("[data-part=close]").ClickAsync();
        await Expect(Tab(page, "Gear")).ToHaveCountAsync(0);
        await Expect(name).ToBeFocusedAsync();

        // The trails' own mark closes the panel the field is in, so the focus goes to the tab beside it.
        await Tab(page, "Trails").Locator("[data-part=close]").ClickAsync();
        await Expect(Tab(page, "Trails")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Map")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldTakeTheFocusItself_WhenItsLastPanelCloses(string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Only notes" }).ClickAsync();
        // The notes' tab is drawn anew in the one group left, so it is focused once that group is the only one.
        await Expect(page.Locator(".tb-dock__group")).ToHaveCountAsync(1);
        await Tab(page, "Notes").FocusAsync();

        await page.Keyboard.PressAsync("Delete");

        await Expect(page.GetByText("Nothing is open.")).ToBeVisibleAsync();
        await Expect(page.Locator(".tb-dock")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldKeepATabOpen_WhenItIsNotClosable(string browser)
    {
        var page = await OpenDockAsync(browser);
        await Tab(page, "Map").ClickAsync();

        await page.Keyboard.PressAsync("Delete");
        await Tab(page, "Map").ClickAsync(new() { Button = MouseButton.Middle });
        // The trails' close arrives after the map's would have.
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("Delete");

        await Expect(Tab(page, "Trails")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Map")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldCloseATab_WhenItIsMiddleClickedOrItsMarkIsClicked(string browser)
    {
        var page = await OpenDockAsync(browser);

        await Tab(page, "Gear").ClickAsync(new() { Button = MouseButton.Middle });
        await Expect(Tab(page, "Gear")).ToHaveCountAsync(0);

        await Tab(page, "Notes").Locator("[data-part=close]").ClickAsync();
        await Expect(Tab(page, "Notes")).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("\"closed\":[\"gear\",\"notes\"]");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldBringAClosedPanelBack_WhenTheAppOpensIt(string browser)
    {
        var page = await OpenDockAsync(browser);
        await Tab(page, "Notes").Locator("[data-part=close]").ClickAsync();
        await Expect(Tab(page, "Notes")).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Button, new() { Name = "Open notes" }).ClickAsync();

        await Expect(Tab(page, "Notes")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByLabel("Note", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldBringTheShowingTabIntoView_WhenItsRowOverflows(string browser)
    {
        var page = await OpenDockAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Crowd" }).ClickAsync();
        await Expect(Tab(page, "Weather")).ToHaveAttributeAsync("aria-selected", "true");

        await page.WaitForFunctionAsync(ShowingTabInView);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldShowItsShowingTab_WhenTheTabStartsOutOfSight(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=crowded");

        await page.WaitForFunctionAsync(ShowingTabInView);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldBringATabIntoView_WhenItJoinsAGroupAsItsShowingTab(string browser)
    {
        var page = await OpenDockAsync(browser);
        // With Gear shown once, its panel stays, so the change below adds nothing to the dock but Gear's tab.
        await Tab(page, "Gear").ClickAsync();
        await Tab(page, "Notes").ClickAsync();
        await Expect(Panel(page, "Notes")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Columns" }).ClickAsync();
        await Expect(Tab(page, "Gear")).ToHaveAttributeAsync("aria-selected", "true");

        await page.WaitForFunctionAsync(ShowingTabInView);
    }
}

public class DockManagerDriverTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldReadSelectAndCloseThePanelsByTheirTitles_WhenTheHelperIsUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock("Hike planner");

        Assert.Equal(["Trails", "Map", "Notes", "Gear", "Weather"], await dock.PanelsAsync());
        Assert.Equal(["Trails", "Notes", "Weather"], await dock.ShowingAsync());

        await dock.SelectAsync("Map");
        await dock.CloseAsync("Notes");

        Assert.Equal(["Trails", "Map", "Gear", "Weather"], await dock.PanelsAsync());
        Assert.Equal(["Map", "Gear", "Weather"], await dock.ShowingAsync());
        // The app had the layout before the helpers came back.
        Assert.Contains("\"active\":\"map\"", await page.GetByTestId("layout").TextContentAsync());
        Assert.Contains("\"closed\":[\"notes\"]", await page.GetByTestId("layout").TextContentAsync());
        Assert.Equal("The panel \"Map\" can't be closed.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.CloseAsync("Map"))).Message);
        await Expect(dock.PanelElement("Map").GetByRole(AriaRole.Button, new() { Name = "Zoom in" })).ToBeVisibleAsync();
        await Expect(dock.PanelElement("Trails")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMoveASeparatorWithItsKeysAndWaitForTheApp_WhenTheHelperIsUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();

        Assert.Equal(25, await dock.SizeAsync("Resize Trails"));
        await dock.ResizeAsync("Resize Trails", 47);
        await dock.ResizeAsync("Resize Notes", 68);
        await dock.ResizeAsync("Resize Notes", 68);

        Assert.Equal((47, 68), (await dock.SizeAsync("Resize Trails"), await dock.SizeAsync("Resize Notes")));
        // The two side-by-side parts shared a size of 4, and the stacked ones too.
        var layout = await page.GetByTestId("layout").TextContentAsync();
        Assert.Matches("\"size\":1\\.88", layout);
        Assert.Matches("\"size\":2\\.72", layout);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldFindOnlyASplittersSeparator_WhenADocksSeparatorHasItsName(string browser)
    {
        var page = await OpenDockAsync(browser);

        await page.Tb().Splitter("Resize Trails").ResizeAsync(40);

        Assert.Equal(40, await page.Tb().Splitter("Resize Trails").SizeAsync());
        Assert.Equal(25, await page.Tb().Dock().SizeAsync("Resize Trails"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMoveASeparatorByAPercentOfItsTwoSides_WhenTheyTakeOnlyPartOfTheDock(string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Columns" }).ClickAsync();
        var dock = page.Tb().Dock();
        // The first two columns take three quarters of the dock, one part to two.
        await Expect(Tab(page, "Gear")).ToHaveAttributeAsync("aria-selected", "true");
        Assert.Equal(33, await dock.SizeAsync("Resize Gear"));

        await dock.ResizeAsync("Resize Gear", 50);

        Assert.Equal(50, await dock.SizeAsync("Resize Gear"));
        // Seventeen steps of a percent of those three quarters, from a third of them.
        Assert.Equal((1.0 / 3 + 0.17) * 0.75 * 766,
            (await page.Locator(".tb-dock__group").First.BoundingBoxAsync())!.Width, 1.0);
        // The third column, on the far side of the next separator, keeps its quarter.
        Assert.Equal(0.25 * 766, (await page.Locator(".tb-dock__group").Nth(2).BoundingBoxAsync())!.Width, 1.0);
    }
}

public abstract class DockManagerPage(BrowserFixture fixture) : BrowserTest(fixture)
{
    // The dock's parts in the order the page holds them: each panel by its id, and each group and separator by name.
    protected const string Parts = "() => [...document.querySelector('.tb-dock').children]"
        + ".filter(part => part.dataset.part && part.dataset.part !== 'drop-mark')"
        + ".map(part => part.dataset.panel ?? part.dataset.part).join(' ')";

    // Whether the first group's row is too narrow for its tabs and still shows all of its showing tab.
    protected const string ShowingTabInView = """
        () => {
            const tab = document.querySelector('.tb-dock__tab[aria-selected="true"]').getBoundingClientRect();
            const row = document.querySelector('.tb-dock__tabs');
            const box = row.getBoundingClientRect();
            return row.scrollWidth > row.clientWidth && tab.left >= box.left - 1 && tab.right <= box.right + 1;
        }
        """;

    protected async Task<IPage> OpenDockAsync(string browser, string query = "",
        BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/dock-manager" + query, options);
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    protected static Task<string> PartsAsync(IPage page) => page.EvaluateAsync<string>(Parts);

    protected static ILocator Tab(IPage page, string name) =>
        page.GetByRole(AriaRole.Tab, new() { Name = name, Exact = true });

    protected static ILocator Panel(IPage page, string name) =>
        page.GetByRole(AriaRole.Tabpanel, new() { Name = name, Exact = true });

    // The fixture's splitter has a separator named like the dock's first one.
    protected static ILocator Separator(IPage page, string name) =>
        page.Locator(".tb-dock").GetByRole(AriaRole.Separator, new() { Name = name, Exact = true });
}
