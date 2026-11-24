using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CommandPaletteScenarioTests : BunitContext
{
    public CommandPaletteScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Commands_ShouldRunTheCommandFound_WhenTheUserSearchesByAKeyword()
    {
        var cut = Render<CommandPaletteExample>();
        var commands = cut.Tb().CommandPalette();

        // OpenAsync opens the palette as its shortcut does. A search matches a command's keywords as well as its name.
        await commands.OpenAsync();
        await commands.SearchAsync("load");
        Assert.Equal(["Open a saved map"], commands.Results);

        await commands.ChooseAsync("Open a saved map");
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Last command: Open a saved map");
    }
}
