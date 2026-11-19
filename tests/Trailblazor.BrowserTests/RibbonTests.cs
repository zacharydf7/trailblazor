using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class RibbonTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Tab(IPage page, string title) =>
        page.GetByRole(AriaRole.Tab, new() { Name = title, Exact = true });

    private static ILocator Commands(IPage page, string tab) =>
        page.GetByRole(AriaRole.Toolbar, new() { Name = tab, Exact = true });

    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    // A tab's commands take the arrow keys once their script has attached.
    private async Task<IPage> OpenReadyAsync(string browser, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/examples/ribbon", options);
        await Expect(Commands(page, "Notes")).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldBeTwoTabStops_WhenTabbedThrough(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Tab(page, "Notes").FocusAsync();

        // The panel around the commands is no stop of its own.
        await page.Keyboard.PressAsync("Tab");
        await Expect(Button(page, "New note")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Button(page, "Find")).ToBeFocusedAsync();

        // The next stop is past the ribbon: the tab of the ribbon in the next section.
        await page.Keyboard.PressAsync("Tab");
        await Expect(Tab(page, "Photos")).ToBeFocusedAsync();

        // Coming back, the commands' stop is the control that had the focus.
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Button(page, "Find")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Tab(page, "Notes")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldMoveAcrossItsGroupsWithTheArrows_WhenACommandHasFocus(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Button(page, "Delete").FocusAsync();

        // Delete ends the first group, and the alert button starts the next.
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Button(page, "Set an alert")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Button(page, "Delete")).ToBeFocusedAsync();

        // The select in the second group is reached like a button, and the menu's button ends the row.
        await page.Keyboard.PressAsync("End");
        await Expect(Button(page, "File under")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(Button(page, "New note")).ToBeFocusedAsync();
        await Button(page, "Mute").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Remind me" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldShowAnotherTabsCommands_WhenTheArrowKeysMoveAlongItsTabs(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Tab(page, "Notes").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Tab(page, "Trails")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Commands(page, "Trails")).ToBeVisibleAsync();
        await Expect(Commands(page, "Notes")).ToBeHiddenAsync();
        // The new tab's commands are one stop too, and a button that is turned off is passed over.
        await Expect(Commands(page, "Trails")).ToHaveAttributeAsync("data-ready", "");
        await page.Keyboard.PressAsync("Tab");
        await Expect(Button(page, "Add a trail")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(Button(page, "Open a folder")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldRunCommandsAndToggle_WhenUsedThroughItsHelper(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var ribbon = page.Tb().Ribbon("Trail planner");
        Assert.Equal(["Notes", "Trails"], await ribbon.TabsAsync());
        Assert.Equal(["Write", "Alerts", "Progress"], await ribbon.GroupsAsync());

        await ribbon.ClickAsync("New note");
        await ribbon.ClickAsync("Mute");

        await Expect(page.GetByText("Last command: New note. Alerts are muted")).ToBeVisibleAsync();
        await Expect(Button(page, "Mute")).ToHaveAttributeAsync("aria-pressed", "true");

        await ribbon.SelectAsync("Trails");
        Assert.Equal("Trails", await ribbon.SelectedAsync());
        Assert.Equal(["Plan"], await ribbon.GroupsAsync());
        Assert.Equal("The ribbon button \"Close the trail\" is disabled.", (await Assert
            .ThrowsAsync<InvalidOperationException>(() => ribbon.ClickAsync("Close the trail"))).Message);
        Assert.Equal("The ribbon's selected tab has no button named \"New note\".", (await Assert
            .ThrowsAsync<InvalidOperationException>(() => ribbon.ClickAsync("New note"))).Message);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Group_ShouldStackItsSmallControlsBesideALargeButton_WhenItHoldsBoth(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var items = (await page.GetByRole(AriaRole.Group, new() { Name = "Write" }).Locator("[data-part=items]")
            .BoundingBoxAsync())!;
        var large = (await Button(page, "New note").BoundingBoxAsync())!;
        var small = new List<LocatorBoundingBoxResult>();
        foreach (var name in new[] { "Find", "Filter", "Delete" })
        {
            small.Add((await Button(page, name).BoundingBoxAsync())!);
        }

        // The large button is as tall as the three rows beside it, with its icon above its text.
        Assert.Equal(items.Height, large.Height, 1.0);
        var icon = (await Button(page, "New note").Locator("svg").BoundingBoxAsync())!;
        var text = (await Button(page, "New note").Locator("[data-part=label]").BoundingBoxAsync())!;
        Assert.True(icon.Y + icon.Height <= text.Y + 1, $"The icon ends at {icon.Y + icon.Height}, the text starts "
            + $"at {text.Y}.");
        Assert.All(small, button => Assert.Equal(small[0].X, button.X, 1.0));
        Assert.True(small[0].X >= large.X + large.Width - 1);
        Assert.True(small[0].Y < small[1].Y && small[1].Y < small[2].Y);
        // The group's name sits under its controls.
        var label = (await page.Locator("[data-part=group-label]").First.BoundingBoxAsync())!;
        Assert.True(label.Y >= items.Y + items.Height - 1);
        // Every button is a target of at least 24 pixels, the icon alone included.
        var alone = (await Button(page, "Mark as done").BoundingBoxAsync())!;
        Assert.All(small.Append(large).Append(alone), button =>
            Assert.True(button.Width >= 24 && button.Height >= 24, $"{button.Width} by {button.Height}"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldKeepTheLinesBetweenItsGroups_WhenForcedColorsAreOn(string browser)
    {
        var page = await OpenReadyAsync(browser, new() { ForcedColors = ForcedColors.Active });
        var second = page.GetByRole(AriaRole.Group, new() { Name = "Alerts" });

        // The line is drawn in the system's text color, whatever color the theme gave it.
        await Expect(second).ToHaveCSSAsync("border-inline-start-style", "solid");
        await Expect(second).ToHaveCSSAsync("border-inline-start-color", await SystemColorAsync(page, "CanvasText"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldScrollItsCommandsAndBringTheFocusedOneIntoView_WhenItIsTooNarrow(string browser)
    {
        var page = await OpenReadyAsync(browser, new() { ViewportSize = new() { Width = 390, Height = 800 } });
        var commands = Commands(page, "Notes");
        Assert.True(await commands.EvaluateAsync<bool>("row => row.scrollWidth > row.clientWidth"));
        // The page itself doesn't scroll sideways.
        Assert.False(await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth"));

        await Button(page, "New note").FocusAsync();
        await page.Keyboard.PressAsync("End");

        var last = Button(page, "File under");
        await Expect(last).ToBeFocusedAsync();
        await Expect(last).ToBeInViewportAsync(new() { Ratio = 1 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldSwitchTabsAndRefuseAHelpersClick_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-ribbon");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Tab(page, "Layers").ClickAsync();

        await Expect(Commands(page, "Layers")).ToBeVisibleAsync();
        await Expect(Commands(page, "View")).ToBeHiddenAsync();
        // The script that shows the panel leaves it out of the tab order, as the server's markup does.
        await Expect(page.Locator("[role=tabpanel]:not([hidden])")).Not.ToHaveAttributeAsync("tabindex", "0");
        await Tab(page, "Layers").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(Button(page, "Trails")).ToBeFocusedAsync();

        var still = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Ribbon("Map tools").ClickAsync("Trails"));
        Assert.Equal("On a static page the ribbon's buttons do nothing; render the page interactively to use them.",
            still.Message);
    }
}
