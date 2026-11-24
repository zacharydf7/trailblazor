using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ToolbarScenarioTests : BunitContext
{
    public ToolbarScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailNotes_ShouldRunTheClickedAction_WhenItsButtonsAreClicked()
    {
        var cut = Render<ToolbarExample>();
        var notes = cut.Tb().Toolbar("Trail notes");

        // ClickAsync clicks a toolbar button by its name. bUnit lays nothing out, so no button moves into the More menu
        // here; the browser test narrows the page to show that.
        await notes.ClickAsync("Add a note");
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Last action: Add a note");

        await notes.ClickAsync("Delete note");
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Last action: Delete note");
    }
}
