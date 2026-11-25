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
    public async Task Window_ShouldKeepARoomySizeInsideTheDock_WhenTheDockIsCompact(string browser)
    {
        var page = await OpenDockAsync(browser, "?compact=true");
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Weather");
        await dock.MoveWindowAsync("Weather", 25, 25);
        var row = page.Locator(".tb-dock > .tb-dock__group--floating");
        var grip = page.Locator(".tb-dock__window-grip > [data-part=trigger]");

        await NarrowAsync(page, "390px");

        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-compact", "");
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var (top, panel) = ((await row.BoundingBoxAsync())!, (await Panel(page, "Weather").BoundingBoxAsync())!);
        // Half of this dock would be under 200 pixels each way. The window is 18rem by 14rem instead, and moved back
        // so it doesn't run past the dock's end and bottom.
        Assert.Equal(288, top.Width, 1.0);
        Assert.Equal(224, panel.Y + panel.Height - top.Y, 1.0);
        Assert.Equal((top.X, top.Width), (panel.X, panel.Width));
        Assert.Equal(area.X + area.Width - 1, top.X + top.Width, 1.0);
        Assert.Equal(area.Y + area.Height - 1, panel.Y + panel.Height, 1.0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Move the Weather window" })).ToBeInViewportAsync();
        var corner = (await grip.BoundingBoxAsync())!;
        Assert.Equal(top.X + top.Width, corner.X + corner.Width, 1.0);
        Assert.Equal(panel.Y + panel.Height, corner.Y + corner.Height, 1.0);

        // A move and a resize start from the window that shows, and it can't get shorter than it is. The layout
        // keeps its own size on each side they leave alone.
        await dock.MoveWindowAsync("Weather", -10, 0);
        Assert.Equal((50, 50), ((await dock.WindowAsync("Weather")).Width, (await dock.WindowAsync("Weather")).Height));
        await dock.ResizeWindowAsync("Weather", 1, 0);
        Assert.Equal(50, (await dock.WindowAsync("Weather")).Height);
        var resized = (await row.BoundingBoxAsync())!;
        Assert.Equal(top.X - 38.8, resized.X, 1.0);
        Assert.Equal(288 + 3.88, resized.Width, 1.0);
        await grip.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Shorter" }))
            .ToHaveAttributeAsync("aria-disabled", "true");
        await page.Keyboard.PressAsync("Escape");

        await NarrowAsync(page, "48rem");

        // Wide again, the window takes the share of the dock it was given while compact.
        await Expect(page.Locator(".tb-dock")).Not.ToHaveAttributeAsync("data-compact", "");
        var wide = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        Assert.Equal((wide.Width - 2) * (await dock.WindowAsync("Weather")).Width / 100,
            (await row.BoundingBoxAsync())!.Width, 1.0);
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
