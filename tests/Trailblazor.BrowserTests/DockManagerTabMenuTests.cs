using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockManagerTabMenuTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldOpenATabsMenuAtThePointerAndCloseThatTab_WhenATabThatIsNotShowingIsRightClicked(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        var gear = (await Tab(page, "Gear").BoundingBoxAsync())!;
        var field = page.GetByLabel("Trail name");
        await field.FocusAsync();

        await Tab(page, "Gear").ClickAsync(new() { Button = MouseButton.Right });

        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Tab options" });
        await Expect(menu).ToBeVisibleAsync();
        var box = (await menu.BoundingBoxAsync())!;
        Assert.Equal(gear.X + gear.Width / 2, box.X, 3.0);
        Assert.Equal(gear.Y + gear.Height / 2, box.Y, 3.0);
        // The right-click showed nothing else: Notes is still the showing tab of Gear's group.
        Assert.Equal(["Trails", "Notes", "Weather"], await dock.ShowingAsync());

        await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Close" }).ClickAsync();

        await Expect(Tab(page, "Gear")).ToHaveCountAsync(0);
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], await dock.PanelsAsync());
        Assert.Equal(["Trails", "Notes", "Weather"], await dock.ShowingAsync());
        // The right press didn't take the focus from the field, so it is back there once the menu has closed.
        await Expect(field).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMoveATabFromItsOwnMenuAndWaitForTheApp_WhenTheHelperIsUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();

        // The map isn't showing and can't close, so its menu has the moves and no Close.
        await dock.ChooseFromTabMenuAsync("Map", "Move to group", "Group 3: Weather");

        Assert.Equal(["Trails", "Notes", "Gear", "Weather", "Map"], await dock.PanelsAsync());
        Assert.Contains("[\"weather\",\"map\"]", await page.GetByTestId("layout").TextContentAsync());
        await Expect(Tab(page, "Map")).ToBeFocusedAsync();
        await page.Tb().Announcements.ExpectAnnouncedAsync("Map moved to panel group 3.");
        // The helper opens the menu on the tab itself: Gear isn't showing, and it's Gear that closes.
        await dock.ChooseFromTabMenuAsync("Gear", "Close");
        Assert.Equal(["Trails", "Notes", "Weather", "Map"], await dock.PanelsAsync());
        await Tab(page, "Map").ClickAsync(new() { Button = MouseButton.Right });
        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Tab options" });
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Move tab back" })).ToBeVisibleAsync();
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Close" })).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldOpenTheFocusedTabsMenuAndGiveTheFocusBack_WhenShiftF10OrTheMenuKeyIsPressed(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Tab options" });
        await Tab(page, "Notes").FocusAsync();

        await page.Keyboard.PressAsync("Shift+F10");
        await Expect(menu).ToBeVisibleAsync();
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Move tab back" })).ToBeFocusedAsync();
        // From the keyboard the menu opens under the tab, at the side where its text starts.
        var (tab, list) = ((await Tab(page, "Notes").BoundingBoxAsync())!, (await menu.BoundingBoxAsync())!);
        Assert.Equal(tab.X, list.X, 3.0);
        Assert.Equal(tab.Y + tab.Height, list.Y, 3.0);
        await page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(Tab(page, "Notes")).ToBeFocusedAsync();

        // Notes is first in its row, so the move back is disabled and the next item down moves it forward.
        await page.Keyboard.PressAsync("ContextMenu");
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Move tab back" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Move tab forward" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        // The menu gives the focus back at once, so the move is waited for by the dock's count of finished actions.
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-commits", "1");
        Assert.Equal(["Trails", "Map", "Gear", "Notes", "Weather"], await page.Tb().Dock().PanelsAsync());
        await Expect(Tab(page, "Notes")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldOpenTheMenuOfTheShowingPanel_WhenTheRestOfATabRowIsRightClicked(string browser)
    {
        var page = await OpenDockAsync(browser);
        var row = page.Locator(".tb-dock__tabs").Nth(2);
        var box = (await row.BoundingBoxAsync())!;
        var field = page.GetByLabel("Trail name");
        await field.FocusAsync();

        // The weather's row has room after its one tab.
        await row.ClickAsync(new() { Button = MouseButton.Right, Position = new() { X = box.Width - 12, Y = 12 } });
        await page.GetByRole(AriaRole.Menu, new() { Name = "Tab options" })
            .GetByRole(AriaRole.Menuitem, new() { Name = "Close" }).ClickAsync();

        await Expect(Tab(page, "Weather")).ToHaveCountAsync(0);
        Assert.Equal(["Trails", "Map", "Notes", "Gear"], await page.Tb().Dock().PanelsAsync());
        // A right press on the row's empty part doesn't take the focus from the field either.
        await Expect(field).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldOpenATabsMenuAndPutTheTabDown_WhenAFingerRestsOnItLongerThanItTakesToPickItUp(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");

        // A finger that rests on a tab picks it up, and one that keeps resting opens the tab's menu instead.
        var pickedUp = await Tab(page, "Gear").EvaluateAsync<bool>(RestOnTab);

        Assert.True(pickedUp);
        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Tab options" });
        await Expect(menu).ToBeVisibleAsync();
        await Expect(Tab(page, "Gear")).Not.ToHaveAttributeAsync("data-dragging", "");
        await LiftAsync(page);
        // A press outside the menu closes it.
        await page.GetByTestId("layout").ClickAsync();
        await Expect(menu).ToBeHiddenAsync();

        // The second time, the menu has its items already and opens without asking for them, and still puts the tab
        // down.
        Assert.True(await Tab(page, "Gear").EvaluateAsync<bool>(RestOnTab));
        await Expect(menu).ToBeVisibleAsync();
        await Expect(Tab(page, "Gear")).Not.ToHaveAttributeAsync("data-dragging", "");
        await Expect(page.Locator(".tb-dock")).Not.ToHaveAttributeAsync("data-moving", "");
        await LiftAsync(page);
        await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Close" }).ClickAsync();
        await Expect(Tab(page, "Gear")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldLeaveTheFocusOrHandItToTheShowingTab_WhenATabThatIsNotShowingIsClosedFromItsMenu(
        string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=crowded");
        var field = page.GetByLabel("Trail name");
        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Tab options" });
        await Tab(page, "Trails").ClickAsync();

        // The menu opens without a press that could move the focus, so the focus goes back to the field it was in.
        await field.FocusAsync();
        await Tab(page, "Notes").DispatchEventAsync("contextmenu", new { bubbles = true, clientX = 40, clientY = 40 });
        await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Close" }).ClickAsync();
        await Expect(Tab(page, "Notes")).ToHaveCountAsync(0);
        await Expect(field).ToBeFocusedAsync();

        // A tab that isn't showing holds the focus here, and the row's showing tab takes it, not the tab beside it.
        // Focusing the tab scrolls its row, and a scroll drops a menu that is still opening, so the row settles first.
        await Tab(page, "Weather").FocusAsync();
        await SettleAsync(page);
        await Tab(page, "Weather").DispatchEventAsync("contextmenu", new { bubbles = true, clientX = 40, clientY = 40 });
        await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Close" }).ClickAsync();
        await Expect(Tab(page, "Weather")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();
    }

    // In WebAssembly the dock hears of the choice before the menu has given the focus back to the tab.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldFocusTheNextTab_WhenAFocusedTabIsClosedFromItsMenuInWebAssembly(string browser)
    {
        var page = await OpenDockAsync(browser, "?mode=webassembly");
        await Expect(page.Locator("main")).ToHaveAttributeAsync("data-renderer", "WebAssembly");
        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Tab options" });
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");
        await Tab(page, "Notes").FocusAsync();
        await SettleAsync(page);

        await page.Keyboard.PressAsync("Shift+F10");
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Move tab back" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Close" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(Tab(page, "Notes")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
        await Expect(menu).ToBeHiddenAsync();

        // The weather's group goes with its only tab, and its menu with it, before the menu can say it has closed.
        // A close after that still finds the focus where it is: on Gear, whose only neighbor is the map's group.
        await Tab(page, "Weather").FocusAsync();
        await SettleAsync(page);
        await page.Keyboard.PressAsync("Shift+F10");
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Move tab back" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Close" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(Tab(page, "Weather")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
        await Tab(page, "Gear").Locator("[data-part=close]").ClickAsync();

        await Expect(Tab(page, "Gear")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldAskAfreshWhoHoldsTheFocus_WhenATabsMenuClosedWithNothingChosen(string browser)
    {
        var page = await OpenDockAsync(browser);
        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Tab options" });
        var field = page.GetByLabel("Trail name");
        await field.FocusAsync();

        // The menu opens with the focus in the trails' field and closes with nothing chosen, which gives the focus
        // back to the field.
        await Tab(page, "Gear").ClickAsync(new() { Button = MouseButton.Right });
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Move tab back" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(field).ToBeFocusedAsync();

        // The focus is in the notes when their tab closes, so the next tab takes it.
        await page.GetByLabel("Note", new() { Exact = true }).FocusAsync();
        await Tab(page, "Notes").Locator("[data-part=close]").ClickAsync();

        await Expect(Tab(page, "Notes")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
    }

    // A finger comes down on the middle of a tab and stays; says whether the tab was picked up 400 ms later.
    private const string RestOnTab = """
        async tab => {
            const box = tab.getBoundingClientRect();
            tab.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerId: 7, pointerType: 'touch',
                isPrimary: true, button: 0, clientX: box.left + box.width / 2, clientY: box.top + box.height / 2 }));
            await new Promise(resolve => setTimeout(resolve, 400));
            return tab.hasAttribute('data-dragging');
        }
        """;

    // Two frames, by which a scroll that a focus started has been heard.
    private static Task SettleAsync(IPage page) =>
        page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");

    private static Task LiftAsync(IPage page) => page.EvaluateAsync("""
        () => document.dispatchEvent(new PointerEvent('pointerup', {
            bubbles: true, pointerId: 7, pointerType: 'touch', isPrimary: true }))
        """);
}
