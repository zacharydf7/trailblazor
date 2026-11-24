using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class HotkeysScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SaveShortcut_ShouldSaveTheTrailAndBeListedInTheHelp_WhenPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/hotkeys/page");

        // A hotkey is found by its keys. PressAsync presses them once the hotkey is ready, and waits until its OnPress
        // has run.
        await page.Tb().Hotkey("Alt+Shift+S").PressAsync();
        await Expect(page.GetByLabel("Last shortcut")).ToHaveTextAsync("Saved");

        await page.Tb().Button("Keyboard shortcuts").ClickAsync();
        var help = page.GetByRole(AriaRole.Dialog, new() { Name = "Keyboard shortcuts" });
        await Expect(help.GetByRole(AriaRole.Table, new() { Name = "Trail" }).Locator("tbody").GetByRole(AriaRole.Row))
            .ToContainTextAsync(["Save the trail", "Add a waypoint"]);
    }
}
