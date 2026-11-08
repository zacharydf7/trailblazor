namespace Trailblazor.BrowserTests;

public class EditableStopsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldWrapBothWays_WhenAnEditableRegionEndsIt(string browser)
    {
        var page = await OpenAsync(browser, "/editable-stops");
        await Button(page, "Open editable last").ClickAsync();
        await Expect(Button(page, "Before")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(page.GetByTestId("notes")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(Button(page, "Before")).ToBeFocusedAsync();
    }

    // Only Shift+Tab: Tab steps through a player's own controls over several presses, which the scope doesn't follow.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldWrapBackwardToThePlayer_WhenAPlayerWithControlsEndsIt(string browser)
    {
        var page = await OpenAsync(browser, "/editable-stops");
        await Button(page, "Open player last").ClickAsync();
        await Expect(Button(page, "Before")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(page.GetByTestId("player")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStartOnTheEditableRegion_WhenItComesFirst(string browser)
    {
        var page = await OpenAsync(browser, "/editable-stops");

        await Button(page, "Open editable first").ClickAsync();

        await Expect(page.GetByTestId("draft")).ToBeFocusedAsync();
    }
}
