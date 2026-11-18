using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockManagerMoveTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMoveAPanelToAnotherGroupAndKeepTheFocusOnItsTab_WhenItsGroupsMenuIsUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();

        // The menu is a stop of its own after the group's tabs, so the keyboard reaches it.
        await Tab(page, "Trails").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Options for Trails" })).ToBeFocusedAsync();
        await page.Tb().Menu("Options for Trails").ChooseAsync("Move to group", "Group 2: Notes");

        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();
        Assert.Equal(["Map", "Notes", "Gear", "Trails", "Weather"], await dock.PanelsAsync());
        Assert.Equal(["Map", "Trails", "Weather"], await dock.ShowingAsync());
        await page.Tb().Announcements.ExpectAnnouncedAsync("Trails moved to panel group 2.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMoveATabAlongItsRow_WhenForwardOrBackIsChosen(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();

        await page.Tb().Menu("Options for Trails").ChooseAsync("Move tab forward");
        Assert.Equal(["Map", "Trails", "Notes", "Gear", "Weather"], await dock.PanelsAsync());
        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();

        await page.Tb().Menu("Options for Trails").ChooseAsync("Move tab back");
        Assert.Equal(["Trails", "Map", "Notes", "Gear", "Weather"], await dock.PanelsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldGiveAPanelANewGroup_WhenASideOfItsGroupOrOfTheDockIsChosen(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;

        // Under its own group: Notes keeps the top half of the box the two shared.
        await page.Tb().Menu("Options for Notes").ChooseAsync("New group beside this one", "Below");
        await Expect(page.GetByRole(AriaRole.Tablist)).ToHaveCountAsync(4);
        var notes = (await page.Locator(".tb-dock__group").Nth(2).BoundingBoxAsync())!;
        Assert.Equal(dock.Y + 1 + 0.375 * 382, notes.Y, 1.0);
        Assert.Equal(["Trails", "Gear", "Notes", "Weather"], await page.Tb().Dock().ShowingAsync());

        // Along the dock's bottom edge: a quarter of the height, across the whole width.
        await page.Tb().Menu("Options for Weather").ChooseAsync("New group at the dock's edge", "At the bottom");
        await Expect(page.GetByRole(AriaRole.Tablist)).ToHaveCountAsync(4);
        var weather = (await page.Locator(".tb-dock__group").Nth(3).BoundingBoxAsync())!;
        Assert.Equal(766, weather.Width, 1.0);
        Assert.Equal(dock.Y + 1 + 0.75 * 382, weather.Y, 1.0);
        await Expect(Tab(page, "Weather")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldCloseAPanel_WhenCloseIsChosenFromItsGroupsMenu(string browser)
    {
        var page = await OpenDockAsync(browser);

        await page.Tb().Menu("Options for Notes").ChooseAsync("Close");

        await Expect(Tab(page, "Notes")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
        // The map can't close, so its menu has no such item.
        await Tab(page, "Map").ClickAsync();
        await page.Tb().Menu("Options for Map").OpenAsync();
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Move tab back" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Close" })).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldResizeInSteps_WhenASeparatorsGripMenuIsUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        // The splitter under the dock has a grip with the same name.
        var dock = page.Locator(".tb-dock");
        var grip = dock.GetByRole(AriaRole.Button, new() { Name = "Size of Trails" });
        // The grip shows once the pointer is on its separator.
        await Expect(grip).ToHaveCSSAsync("opacity", "0");
        var box = (await Separator(page, "Resize Trails").BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 4);
        await Expect(grip).ToHaveCSSAsync("opacity", "1");

        await dock.Tb().Menu("Size of Trails").ChooseAsync("Make Trails larger");
        await Expect(Separator(page, "Resize Trails")).ToHaveAttributeAsync("aria-valuenow", "35");
        await dock.Tb().Menu("Size of Notes").ChooseAsync("Make Notes smaller");

        await Expect(Separator(page, "Resize Notes")).ToHaveAttributeAsync("aria-valuenow", "65");
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("\"size\":1.4");

        // The steps stop where the keys do: 64 pixels is a little over 8% of the width.
        for (var step = 0; step < 4; step++)
        {
            await dock.Tb().Menu("Size of Trails").ChooseAsync("Make Trails smaller");
        }

        await Expect(Separator(page, "Resize Trails")).ToHaveAttributeAsync("aria-valuenow", "8");
        // The grip and a group's menu button are targets of at least 24 pixels.
        var targets = new[] { grip, page.GetByRole(AriaRole.Button, new() { Name = "Options for Trails" }) };
        foreach (var target in targets)
        {
            var size = (await target.BoundingBoxAsync())!;
            Assert.True(size.Width >= 24 && size.Height >= 24, $"{size.Width} by {size.Height}");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldKeepItsLayout_WhenThePageIsReloadedWithAPersistenceKey(string browser)
    {
        var page = await OpenDockAsync(browser, "?key=hike");
        await page.Tb().Dock().ResizeAsync("Resize Trails", 40);
        await page.Tb().Dock().CloseAsync("Gear");
        await page.WaitForFunctionAsync("() => localStorage.getItem('tb-dock:hike')?.includes('\"closed\":[\"gear\"]')");

        await page.ReloadAsync();
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");

        await Expect(Separator(page, "Resize Trails")).ToHaveAttributeAsync("aria-valuenow", "40");
        await Expect(Tab(page, "Gear")).ToHaveCountAsync(0);
        // The app heard the saved layout, and its own layout takes the saved one's place when it sets one.
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("\"closed\":[\"gear\"]");
        await page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).ClickAsync();
        await Expect(Tab(page, "Gear")).ToHaveCountAsync(1);
        await page.WaitForFunctionAsync("() => localStorage.getItem('tb-dock:hike')?.includes('\"closed\":[]')");
    }
}

public class DockManagerMoveDriverTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMoveAPanelWithItsGroupsMenuAndWaitForTheApp_WhenAMoveHelperIsUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();

        // The map isn't showing, so the helper selects it before it moves it.
        await dock.MoveTabAsync("Map", -1);
        Assert.Equal(["Map", "Trails", "Notes", "Gear", "Weather"], await dock.PanelsAsync());
        await dock.MoveTabAsync("Map", 1);
        await dock.MoveToGroupAsync("Map", 3);
        Assert.Equal(["Trails", "Notes", "Gear", "Weather", "Map"], await dock.PanelsAsync());
        // The app had the layout before the helper came back.
        Assert.Contains("[\"weather\",\"map\"]", await page.GetByTestId("layout").TextContentAsync());
        await dock.MoveToNewGroupAsync("Gear", DockPosition.Start);
        await dock.MoveToEdgeAsync("Trails", DockPosition.Bottom);

        Assert.Equal(["Gear", "Notes", "Weather", "Map", "Trails"], await dock.PanelsAsync());
        Assert.Equal(["Gear", "Notes", "Map", "Trails"], await dock.ShowingAsync());
        await Expect(page.GetByRole(AriaRole.Menu)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldSayWhyAPanelCantMove_WhenItsMenuHasNoSuchItem(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();

        Assert.Equal("The tab of \"Trails\" is at the start of its row.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.MoveTabAsync("Trails", -1))).Message);
        Assert.Equal("The dock has no other group 1 to move \"Trails\" to.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.MoveToGroupAsync("Trails", 1))).Message);
        Assert.Equal("The panel \"Weather\" is alone in its group, so it has no group to split.",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => dock.MoveToNewGroupAsync("Weather", DockPosition.Top))).Message);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => dock.MoveToEdgeAsync("Trails", DockPosition.Before));
        // Nothing moved, and no menu was left open.
        Assert.Equal(["Trails", "Map", "Notes", "Gear", "Weather"], await dock.PanelsAsync());
        await Expect(page.GetByRole(AriaRole.Menu)).ToHaveCountAsync(0);
    }
}

// Dragging uses pointer capture, which Firefox can leak into parallel pages, so it runs with the other drag tests.
[Collection(nameof(PointerDrags))]
public class DockManagerGripDragTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldResizeWithoutOpeningTheMenu_WhenASeparatorsGripIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser);
        var box = (await Separator(page, "Resize Trails").BoundingBoxAsync())!;
        // The grip sits on the middle of the line.
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + 76.6f, y, new() { Steps = 5 });
        // The grip goes with its line while the drag runs, before the app has rendered the new sizes.
        var line = (await Separator(page, "Resize Trails").BoundingBoxAsync())!;
        var grip = (await page.Locator(".tb-dock").GetByRole(AriaRole.Button, new() { Name = "Size of Trails" })
            .BoundingBoxAsync())!;
        Assert.Equal(x + 76.6, line.X + line.Width / 2, 1.5);
        Assert.Equal(line.X + line.Width / 2, grip.X + grip.Width / 2, 1.0);
        await page.Mouse.UpAsync();

        await Expect(Separator(page, "Resize Trails")).ToHaveAttributeAsync("aria-valuenow", "35");
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-report", "1");
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Make Trails larger" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldOpenTheGripsMenuWithoutResizing_WhenAClickOnItWandersAFewPixels(string browser)
    {
        var page = await OpenDockAsync(browser);
        var box = (await Separator(page, "Resize Trails").BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + 3, y, new() { Steps = 3 });
        await page.Mouse.UpAsync();

        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Make Trails larger" })).ToBeVisibleAsync();
        await Expect(Separator(page, "Resize Trails")).ToHaveAttributeAsync("aria-valuenow", "25");
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-report", "0");
    }
}
