using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class MenubarScenarioTests : BunitContext
{
    public MenubarScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailEditor_ShouldRunTheChosenCommand_WhenItemsAreChosenFromItsMenus()
    {
        var cut = Render<MenubarExample>();
        var editor = cut.Tb().Menubar("Trail editor");
        var last = cut.Find("output");

        // Menu finds one of the bar's menus by its name, and ChooseAsync clicks the item.
        await editor.Menu("File").ChooseAsync("Export as GPX");
        Assert.Equal("Last command: Export", last.TextContent);

        await editor.Menu("View").ChooseAsync("Zoom in");
        Assert.Equal("Last command: Zoom in", last.TextContent);
    }
}
