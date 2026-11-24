using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class HotkeysScenarioTests : BunitContext
{
    public HotkeysScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task SaveShortcut_ShouldSaveTheTrailAndBeListedInTheHelp_WhenPressed()
    {
        var cut = Render<HotkeysExample>();

        // A hotkey is found by its keys, and PressAsync runs it as pressing them does.
        await cut.Tb().Hotkey("Alt+Shift+S").PressAsync();
        Assert.Equal("Saved", cut.Find("output").TextContent);

        await cut.Tb().Button("Keyboard shortcuts").ClickAsync();
        var help = await cut.Tb().Dialog("Keyboard shortcuts").WaitForAsync();
        Assert.Equal(["Save the trail", "Add a waypoint"],
            help.QuerySelectorAll("table").Single(table => table.QuerySelector("caption")!.TextContent == "Trail")
                .QuerySelectorAll("tbody td:last-child").Select(cell => cell.TextContent));
    }
}
