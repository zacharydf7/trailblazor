namespace Trailblazor.BrowserTests;

// The focus scope page's examples, whose promises are all about where focus lands.
public class FocusScopeExampleTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldKeepTabInsideAndGiveFocusBack_WhenTrapped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/focus-scope");
        var panel = page.GetByRole(AriaRole.Group, new() { Name = "Find a trail" });
        await Button(page, "Find a trail").ClickAsync();
        await Expect(panel.GetByLabel("Trail name")).ToBeFocusedAsync();

        foreach (var _ in Enumerable.Range(0, 4))
        {
            await page.Keyboard.PressAsync("Tab");
            Assert.True(await panel.EvaluateAsync<bool>("panel => panel.contains(document.activeElement)"));
        }

        await panel.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();

        await Expect(Button(page, "Find a trail")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldFocusTheMarkedField_WhenAFieldHasAutoFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/focus-scope");

        await Button(page, "Focus the marked field").ClickAsync();

        await Expect(page.GetByLabel("Last name")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldLeaveFocusAlone_WhenAutoFocusIsOff(string browser)
    {
        var page = await OpenAsync(browser, "/examples/focus-scope");

        await Button(page, "Leave focus where it is").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Town" })).ToBeVisibleAsync();

        await Expect(Button(page, "Leave focus where it is")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldGiveFocusBackOnlyWhenRestoreFocusIsOn_WhenItCloses(string browser)
    {
        var page = await OpenAsync(browser, "/examples/focus-scope");
        var start = Button(page, "Start the checklist");
        var done = page.GetByRole(AriaRole.Group, new() { Name = "Checklist" })
            .GetByRole(AriaRole.Button, new() { Name = "Done" });

        await start.ClickAsync();
        await done.ClickAsync();
        await Expect(start).ToBeFocusedAsync();

        await page.GetByRole(AriaRole.Switch, new() { Name = "Give focus back" }).ClickAsync();
        await start.ClickAsync();
        await done.ClickAsync();
        await Expect(done).ToHaveCountAsync(0);

        await Expect(start).Not.ToBeFocusedAsync();
    }
}
