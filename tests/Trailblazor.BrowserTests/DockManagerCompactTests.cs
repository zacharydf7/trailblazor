using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// The fixture's dock is 48rem wide, and with ?compact=true it is compact below 640 pixels.
public class DockManagerCompactTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    private const string AllTabs = "els => els.map(tab => tab.textContent.trim())";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldShowItsDockedPanelsAsTheTabsOfOneGroup_WhenItIsNarrowerThanItsCompactWidth(
        string browser)
    {
        var page = await OpenDockAsync(browser, "?compact=true");
        var dock = page.Tb().Dock();
        var rows = page.Locator(".tb-dock > .tb-dock__group");
        await Expect(rows).ToHaveCountAsync(3);
        Assert.False(await dock.IsCompactAsync());

        await NarrowAsync(page, "24rem");

        await Expect(rows).ToHaveCountAsync(1);
        Assert.True(await dock.IsCompactAsync());
        Assert.Equal(["Trails", "Map", "Notes", "Gear", "Weather"], await dock.PanelsAsync());
        Assert.Equal(["Trails"], await dock.ShowingAsync());
        await Expect(page.Locator(".tb-dock > :is(.tb-dock__separator, .tb-dock__grip)")).ToHaveCountAsync(0);
        // The one group and its panel fill the dock.
        var (area, panel) = ((await page.Locator(".tb-dock").BoundingBoxAsync())!,
            (await Panel(page, "Trails").BoundingBoxAsync())!);
        Assert.Equal(area.Width - 2, panel.Width, 1.0);
        Assert.Equal(area.Y + area.Height - 1, panel.Y + panel.Height, 1.0);

        // A panel from another group shows in the same place, and stays its group's showing tab once there is room.
        await dock.SelectAsync("Gear");
        Assert.Equal(["Gear"], await dock.ShowingAsync());
        await NarrowAsync(page, "48rem");

        await Expect(rows).ToHaveCountAsync(3);
        Assert.False(await dock.IsCompactAsync());
        Assert.Equal(["Trails", "Gear", "Weather"], await dock.ShowingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldReachThePanelsOfEveryGroupWithTheArrowKeys_WhenTheDockIsCompact(string browser)
    {
        var page = await OpenDockAsync(browser, "?compact=true");
        await NarrowAsync(page, "24rem");
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-compact", "");

        await Tab(page, "Map").FocusAsync();
        await ChangeAsync(page, () => page.Keyboard.PressAsync("ArrowRight"));

        await Expect(Tab(page, "Notes")).ToBeFocusedAsync();
        await Expect(Panel(page, "Notes")).ToBeVisibleAsync();
        await ChangeAsync(page, () => page.Keyboard.PressAsync("End"));
        await Expect(Tab(page, "Weather")).ToBeFocusedAsync();
        await Expect(Panel(page, "Weather")).ToBeVisibleAsync();
        await Expect(Panel(page, "Notes")).ToBeHiddenAsync();
        await ChangeAsync(page, () => page.Keyboard.PressAsync("Home"));
        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();
        await Expect(Panel(page, "Trails")).ToBeVisibleAsync();
        // Left wraps from the first tab to the last one, three groups on.
        await ChangeAsync(page, () => page.Keyboard.PressAsync("ArrowLeft"));
        await Expect(Tab(page, "Weather")).ToBeFocusedAsync();
        await Expect(Panel(page, "Weather")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldKeepTheFocusAndItsPanelInFront_WhenItTurnsCompactAndBackWhileATabHoldsTheFocus(
        string browser)
    {
        var page = await OpenDockAsync(browser, "?compact=true");
        await Tab(page, "Weather").FocusAsync();

        await NarrowAsync(page, "24rem");

        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-compact", "");
        await Expect(Tab(page, "Weather")).ToBeFocusedAsync();
        await Expect(Panel(page, "Weather")).ToBeVisibleAsync();

        await NarrowAsync(page, "48rem");

        await Expect(page.Locator(".tb-dock")).Not.ToHaveAttributeAsync("data-compact", "");
        await Expect(Tab(page, "Weather")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldKeepAPanelInFront_WhenItTurnsCompactWhileTheFocusIsInsideThePanel(string browser)
    {
        var page = await OpenDockAsync(browser, "?compact=true");
        var note = page.GetByLabel("Note", new() { Exact = true });
        await note.FocusAsync();

        await NarrowAsync(page, "24rem");

        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-compact", "");
        await Expect(Panel(page, "Notes")).ToBeVisibleAsync();
        await Expect(note).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TabRow_ShouldFadeTheSideThatHasTabsPastIt_WhenItsTabsDontFit(string browser)
    {
        // Four tabs in a group too narrow for them, scrolled to the last one, which shows.
        var page = await OpenDockAsync(browser, "?layout=crowded");
        var row = page.Locator(".tb-dock__tabs").First;

        await Expect(row).ToHaveAttributeAsync("data-more-left", "");
        await Expect(row).Not.ToHaveAttributeAsync("data-more-right", "");
        Assert.Contains("gradient", await row.EvaluateAsync<string>("row => getComputedStyle(row).maskImage"));

        await Tab(page, "Weather").FocusAsync();
        await ChangeAsync(page, () => page.Keyboard.PressAsync("Home"));

        await Expect(row).ToHaveAttributeAsync("data-more-right", "");
        await Expect(row).Not.ToHaveAttributeAsync("data-more-left", "");

        // A row whose tabs fit has no side to fade.
        var roomy = page.Locator(".tb-dock__tabs").Nth(1);
        await Expect(roomy).Not.ToHaveAttributeAsync("data-more-left", "");
        await Expect(roomy).Not.ToHaveAttributeAsync("data-more-right", "");
        Assert.Equal("none", await roomy.EvaluateAsync<string>("row => getComputedStyle(row).maskImage"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grip_ShouldSitAboveItsLine_WhenTheLineRunsAlongTheRowOfTabsBelowIt(string browser)
    {
        var page = await OpenDockAsync(browser);
        // The line between the notes and the weather below them, whose row of tabs starts at the line.
        var line = (await Separator(page, "Resize Notes").BoundingBoxAsync())!;
        var grip = (await page.Locator(".tb-dock__grip--vertical > [data-part=trigger]").BoundingBoxAsync())!;
        var row = (await Tab(page, "Weather").BoundingBoxAsync())!;

        Assert.InRange(grip.Y + grip.Height, line.Y, line.Y + line.Height + 1);
        Assert.True(grip.Y + grip.Height <= row.Y + 1, $"The grip ends at {grip.Y + grip.Height}, the row is at {row.Y}.");
    }
}
