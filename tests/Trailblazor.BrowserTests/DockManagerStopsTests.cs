namespace Trailblazor.BrowserTests;

public class DockManagerStopsTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldGoFromTheGroupsMenuToThePanelsFirstControl_WhenThePanelStartsWithOne(string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Options(page, "Search").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByLabel("Trail", new() { Exact = true })).ToBeFocusedAsync();
        await Expect(Panel(page, "Search")).ToHaveAttributeAsync("tabindex", "-1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldStopOnThePanel_WhenItStartsWithText(string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Options(page, "Notes").FocusAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(Panel(page, "Notes")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Edit notes" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldGiveThePanelItsStopAndTakeItAgain_WhenTextTakesItsFirstControlsPlaceAndGoes(
        string browser)
    {
        var page = await OpenStopsAsync(browser);
        var swap = page.GetByRole(AriaRole.Button, new() { Name = "Swap" });

        await swap.ClickAsync();
        await Expect(Panel(page, "Search")).ToHaveAttributeAsync("tabindex", "0");
        await Options(page, "Search").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(Panel(page, "Search")).ToBeFocusedAsync();

        // The panel keeps the focus it has as it gives the stop up.
        await swap.DispatchEventAsync("click");
        await Expect(Panel(page, "Search")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Panel(page, "Search")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldGiveThePanelItsStop_WhenTextFillsInBeforeItsFirstControl(string browser)
    {
        var page = await OpenStopsAsync(browser);
        await Expect(Panel(page, "Search")).ToHaveAttributeAsync("tabindex", "-1");

        // The text changes in a node that is already there, which adds none.
        await page.GetByRole(AriaRole.Button, new() { Name = "Note", Exact = true }).ClickAsync();

        await Expect(Panel(page, "Search")).ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldStopOnEveryPanelOrOnNone_WhenFocusablePanelsIsAlwaysOrNever(string browser)
    {
        var page = await OpenStopsAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Always" }).ClickAsync();
        await Expect(Panel(page, "Search")).ToHaveAttributeAsync("tabindex", "0");
        await Options(page, "Search").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(Panel(page, "Search")).ToBeFocusedAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Never" }).ClickAsync();
        await Expect(Panel(page, "Notes")).ToHaveAttributeAsync("tabindex", "-1");
        await Options(page, "Notes").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Edit notes" })).ToBeFocusedAsync();
    }

    private async Task<IPage> OpenStopsAsync(string browser)
    {
        var page = await OpenAsync(browser, "/dock-stops");
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    private static ILocator Options(IPage page, string panel) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Options for {panel}" });
}
