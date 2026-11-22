using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockKeptWindowTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Opens the page whose editor panel keeps its browser window for this many seconds, with the editor in its window.
    private async Task<(IPage Page, IPage Window)> OpenEditorInItsWindowAsync(string browser, int seconds)
    {
        var page = await OpenAsync(browser, $"/dock-short-lived?keep={seconds}");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Trips" })).ToHaveAttributeAsync("data-ready", "");
        return (page, (await page.Tb().Dock("Trips").PopOutAsync("Editor")).Page);
    }

    private static ILocator Note(IPage window) => window.Locator("[data-part=empty-window]");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldStayOpenAndShowThePanelAgain_WhenTheAppTakesThePanelAwayAndMakesItAgain(
        string browser)
    {
        var (page, window) = await OpenEditorInItsWindowAsync(browser, 600);
        var dock = page.Tb().Dock("Trips");

        // The task ends in the panel's window: the app takes the panel away, and the window says so and stays.
        await window.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();

        await Expect(Note(window))
            .ToHaveTextAsync("Editor isn't open right now. This window shows it again when it is.");
        await Expect(Note(window)).ToHaveRoleAsync(AriaRole.Status);
        Assert.Equal(["List", "Map"], await dock.PanelsAsync());
        Assert.Empty(await dock.PoppedOutAsync());
        await page.EvaluateAsync("""
            () => {
                window.sawEditorDocked = false;
                const docked = '[role=tabpanel][data-panel=editor]:not([data-popped])';
                new MutationObserver(() => window.sawEditorDocked ||= document.querySelector(docked) !== null)
                    .observe(document.body, { childList: true, subtree: true, attributes: true });
            }
            """);

        await page.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();

        // The panel is back in the same window, with no click on Open in new window, and not in the dock.
        await Expect(window.GetByRole(AriaRole.Button, new() { Name = "Done" })).ToBeVisibleAsync();
        await Expect(Note(window)).ToHaveCountAsync(0);
        await Expect(window.GetByRole(AriaRole.Region, new() { Name = "Editor" })).ToBeVisibleAsync();
        // It never showed in the dock on its way there.
        Assert.False(await page.EvaluateAsync<bool>("() => window.sawEditorDocked"));
        Assert.Equal(["Editor"], await dock.PoppedOutAsync());
        Assert.Equal(["List", "Map"], await dock.PanelsAsync());
        Assert.Equal(2, page.Context.Pages.Count);

        // It works there, and can go and come again.
        await window.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();
        await Expect(Note(window)).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await Expect(window.GetByRole(AriaRole.Button, new() { Name = "Done" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldShowWhereItWasDocked_WhenItsKeptWindowWasClosedBeforeTheAppMakesItAgain(
        string browser)
    {
        var (page, window) = await OpenEditorInItsWindowAsync(browser, 600);
        var dock = page.Tb().Dock("Trips");
        await window.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();
        await Expect(Note(window)).ToHaveCountAsync(1);

        await window.CloseAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Editor" })).ToBeVisibleAsync();
        Assert.Equal(["List", "Editor", "Map"], await dock.PanelsAsync());
        Assert.Empty(await dock.FloatingAsync());
        Assert.Empty(await dock.PoppedOutAsync());
        Assert.Single(page.Context.Pages);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldClose_WhenItsPanelStaysAwayLongerThanItsKeepWindowFor(string browser)
    {
        var (page, window) = await OpenEditorInItsWindowAsync(browser, 1);

        await window.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();

        await ClosedAsync(window);
        // The panel then shows where it was docked, as one with no window to go back to.
        await page.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Editor" })).ToBeVisibleAsync();
        Assert.Single(page.Context.Pages);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldShowThePanelMadeAnew_WhenTheAppTakesThePanelAwayAndMakesItAgainAtOnce(
        string browser)
    {
        var (page, window) = await OpenEditorInItsWindowAsync(browser, 600);
        await window.GetByLabel("Draft").FillAsync("Day one");

        // The panel is back before the dock's script has heard that it had gone.
        await window.GetByRole(AriaRole.Button, new() { Name = "Save and new" }).ClickAsync();

        await Expect(window.GetByLabel("Draft")).ToHaveValueAsync("");
        await Expect(window.GetByRole(AriaRole.Region, new() { Name = "Editor" })).ToBeVisibleAsync();
        Assert.Equal(["Editor"], await page.Tb().Dock("Trips").PoppedOutAsync());
        Assert.Equal(["List", "Map"], await page.Tb().Dock("Trips").PanelsAsync());
        Assert.Equal(2, page.Context.Pages.Count);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldStayOpen_WhenItsPanelCameBackBeforeItsTimeWasUp(string browser)
    {
        var (page, window) = await OpenEditorInItsWindowAsync(browser, 2);
        await window.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();
        await Expect(Note(window)).ToHaveCountAsync(1);

        await page.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await Expect(window.GetByRole(AriaRole.Button, new() { Name = "Done" })).ToBeVisibleAsync();

        // The empty window's two seconds run out while the panel is back in it.
        await Task.Delay(2500, TestContext.Current.CancellationToken);
        Assert.False(window.IsClosed);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldCloseWithThePage_WhenItIsKeptForAPanelThatIsAway(string browser)
    {
        var (page, window) = await OpenEditorInItsWindowAsync(browser, 600);
        await window.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();
        await Expect(Note(window)).ToHaveCountAsync(1);

        await page.GotoAsync("about:blank");

        await ClosedAsync(window);
    }
}
