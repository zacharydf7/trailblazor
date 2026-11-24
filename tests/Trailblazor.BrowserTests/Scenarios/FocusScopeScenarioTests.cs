using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class FocusScopeScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FindPanel_ShouldKeepFocusInsideAndGiveItBack_WhenOpenedAndClosed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/focus-scope/trapping");
        await page.Tb().Button("Find a trail").ClickAsync();

        // The scope moves focus to its first field, and Tab and Shift+Tab stay inside it.
        var name = page.GetByLabel("Trail name");
        await Expect(name).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Close" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(name).ToBeFocusedAsync();

        // When the scope closes, focus goes back to the button that opened it.
        await page.Tb().Button("Close").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Find a trail" })).ToBeFocusedAsync();
    }
}
