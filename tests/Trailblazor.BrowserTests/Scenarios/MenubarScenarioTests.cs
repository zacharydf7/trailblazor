using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class MenubarScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailEditor_ShouldRunTheChosenCommand_WhenItemsAreChosenFromItsMenus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menubar/basic");
        var editor = page.Tb().Menubar("Trail editor");
        var last = page.Locator("output");

        // Menu finds one of the bar's menus by its name, and ChooseAsync opens it and clicks the item.
        await editor.Menu("File").ChooseAsync("Export as GPX");
        await Expect(last).ToHaveTextAsync("Last command: Export");

        await editor.Menu("View").ChooseAsync("Zoom in");
        await Expect(last).ToHaveTextAsync("Last command: Zoom in");
    }
}
