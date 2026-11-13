namespace Trailblazor.BrowserTests;

public class PropertyGridTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Group_ShouldFoldAwayAndOpenAgain_WhenItsHeadingIsPressedWithTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/property-grid");
        var heading = page.Locator("summary", new() { HasText = "Route" });
        var length = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Length (km)" });
        await heading.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(length).ToBeHiddenAsync();
        await page.Keyboard.PressAsync("Space");

        await Expect(length).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldVisitEachEditorAndGroupHeadingInOrder_WhenPressedThroughTheGrid(string browser)
    {
        var page = await OpenAsync(browser, "/property-grid");
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FocusAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(page.Locator("summary", new() { HasText = "Route" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Spinbutton, new() { Name = "Length (km)" })).ToBeFocusedAsync();
    }
}
