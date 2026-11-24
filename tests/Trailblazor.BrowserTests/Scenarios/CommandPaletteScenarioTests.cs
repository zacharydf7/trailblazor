using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CommandPaletteScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Commands_ShouldRunTheCommandFound_WhenTheUserSearchesByAKeyword(string browser)
    {
        var page = await OpenAsync(browser, "/examples/command-palette/commands");
        var commands = page.Tb().CommandPalette();

        // OpenAsync opens the palette as its shortcut does. A search matches a command's keywords as well as its name.
        await commands.OpenAsync();
        await commands.SearchAsync("load");
        Assert.Equal(["Open a saved map"], await commands.ResultsAsync());

        await commands.ChooseAsync("Open a saved map");
        await Expect(page.GetByText("Last command: Open a saved map")).ToBeVisibleAsync();
    }
}
