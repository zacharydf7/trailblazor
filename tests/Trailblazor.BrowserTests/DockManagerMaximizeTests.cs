using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockManagerMaximizeTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldFillWithATabsGroupAndThenShowEveryGroupAgain_WhenATabIsDoubleClicked(string browser)
    {
        var page = await OpenDockAsync(browser);
        var filling = page.Locator(".tb-dock__group[data-maximized]");
        await page.GetByLabel("Trail name").FillAsync("Ridge loop");
        var before = (await Panel(page, "Notes").BoundingBoxAsync())!;

        // Gear isn't showing: the first click shows it and the second has its group fill the dock.
        await Tab(page, "Gear").DblClickAsync();

        await Expect(filling).ToHaveCountAsync(1);
        await Expect(Tab(page, "Gear")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var (row, panel) = ((await filling.BoundingBoxAsync())!, (await Panel(page, "Gear").BoundingBoxAsync())!);
        // The dock's border is a pixel wide.
        Assert.Equal(dock.X + 1, row.X, 0.5);
        Assert.Equal(dock.Y + 1, row.Y, 0.5);
        Assert.Equal(dock.Width - 2, row.Width, 0.5);
        Assert.Equal(dock.Width - 2, panel.Width, 0.5);
        Assert.Equal(dock.Y + dock.Height - 1, panel.Y + panel.Height, 0.5);
        // The other groups, their panels and the separators are out of sight and out of reach.
        await Expect(Tab(page, "Trails")).ToBeHiddenAsync();
        await Expect(page.Locator("[role=tabpanel][data-panel=trails]")).ToHaveAttributeAsync("inert", "");
        await Expect(page.Locator(".tb-dock__separator, .tb-dock__grip")).ToHaveCountAsync(0);
        Assert.Equal(["Gear"], await page.Tb().Dock().ShowingAsync());
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Restore" })).ToBeVisibleAsync();
        // The double click selected no text.
        Assert.Equal("", await page.EvaluateAsync<string>("() => getSelection().toString()"));

        // A double click on any tab of the group shows every group again, where it was.
        await Tab(page, "Notes").DblClickAsync();

        await Expect(filling).ToHaveCountAsync(0);
        await Expect(Tab(page, "Trails")).ToBeVisibleAsync();
        await Expect(page.Locator(".tb-dock__separator")).ToHaveCountAsync(2);
        var after = (await Panel(page, "Notes").BoundingBoxAsync())!;
        Assert.Equal(before.X, after.X, 0.5);
        Assert.Equal(before.Y, after.Y, 0.5);
        Assert.Equal(before.Width, after.Width, 0.5);
        Assert.Equal(before.Height, after.Height, 0.5);
        // What a covered panel held is still there.
        await Expect(page.GetByLabel("Trail name")).ToHaveValueAsync("Ridge loop");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldLeaveItsGroupsAsTheyAre_WhenTheTwoClicksAreNotOnOneTab(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();

        // A click on one tab and a double click that ends on another, as when the first click closes a tab and the
        // next one slides under the pointer.
        await dock.SelectAsync("Map");
        await Tab(page, "Trails").DispatchEventAsync("dblclick");
        // The dock handles events in order, so had the double click filled the dock, it would have done so before
        // this selection shows.
        await dock.SelectAsync("Trails");

        await Expect(page.Locator(".tb-dock__group[data-maximized]")).ToHaveCountAsync(0);
        Assert.Equal(["Trails", "Notes", "Weather"], await dock.ShowingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldFillWithAGroupAndShowEveryGroupAgain_WhenTheHelpersAreUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        var city = page.FrameLocator("iframe[title=Forecast]").GetByLabel("City");
        await city.FillAsync("Zermatt");

        await dock.MaximizeAsync("Gear");

        Assert.Equal(["Gear"], await dock.ShowingAsync());
        Assert.Equal(["Trails", "Map", "Notes", "Gear", "Weather"], await dock.PanelsAsync());
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("\"active\":\"gear\",\"maximized\":true");
        // A helper that clicks says why it can't, where the click itself would wait until it timed out.
        var behind = "The panel \"Trails\" is behind the group that fills the dock; restore the groups first.";
        Assert.Equal(behind, (await Assert.ThrowsAsync<InvalidOperationException>(
            () => dock.SelectAsync("Trails"))).Message);
        Assert.Equal(behind, (await Assert.ThrowsAsync<InvalidOperationException>(
            () => dock.ChooseFromTabMenuAsync("Trails", "Close"))).Message);
        Assert.Equal("The group of \"Gear\" is the dock's only one, or fills the dock already.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.MaximizeAsync("Gear"))).Message);
        // The Restore button is the stop between the group's tabs and its menu button.
        await Tab(page, "Gear").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Restore" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Options for Gear" })).ToBeFocusedAsync();

        await dock.RestoreAsync();

        Assert.Equal(["Trails", "Gear", "Weather"], await dock.ShowingAsync());
        await Expect(page.GetByTestId("layout")).Not.ToContainTextAsync("maximized");
        // The button went with the click, and the focus it held is on the group's showing tab.
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
        // The covered panel's iframe kept what it had loaded.
        await Expect(city).ToHaveValueAsync("Zermatt");
        Assert.Equal("No group fills the dock.",
            (await Assert.ThrowsAsync<InvalidOperationException>(dock.RestoreAsync)).Message);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMoveTheFocusToTheTab_WhenTheGroupThatFillsTheDockCoversWhatHeldIt(string browser)
    {
        var page = await OpenDockAsync(browser);
        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Tab options" });
        await page.GetByLabel("Trail name").FocusAsync();

        // The right press leaves the focus in the trails' field, which the notes' group then covers.
        await page.Tb().Dock().ChooseFromTabMenuAsync("Notes", "Maximize");

        await Expect(Tab(page, "Notes")).ToBeFocusedAsync();
        await Expect(page.Locator(".tb-dock__group[data-maximized]")).ToHaveCountAsync(1);

        // From the keyboard, the tab's menu shows every group again and gives the focus back to the tab.
        await page.Keyboard.PressAsync("Shift+F10");
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Move tab back" })).ToBeFocusedAsync();
        await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Restore" }).ClickAsync();

        await Expect(page.Locator(".tb-dock__group[data-maximized]")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Notes")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldKeepTheFocusInTheGroup_WhenItFillsTheDockFromItsMenuInWebAssembly(string browser)
    {
        var page = await OpenDockAsync(browser, "?mode=webassembly");
        await Expect(page.Locator("main")).ToHaveAttributeAsync("data-renderer", "WebAssembly");
        const string inGroup = "() => Boolean(document.activeElement.closest('.tb-dock__group[data-maximized]'))";

        // Here the dock renders before the menu has given the focus back to its button.
        await page.Tb().Dock().MaximizeAsync("Weather");

        await Expect(page.Locator(".tb-dock__group[data-maximized]")).ToHaveCountAsync(1);
        await page.WaitForFunctionAsync(inGroup);
        Assert.Equal(["Weather"], await page.Tb().Dock().ShowingAsync());
    }
}
