using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class FloatingActionButtonScenarioTests : BunitContext
{
    public FloatingActionButtonScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task SpeedDial_ShouldRunTheChosenAction_WhenAnActionIsChosen()
    {
        var cut = Render<SpeedDialExample>();
        var last = cut.Find("output");
        Assert.Equal("Nothing added yet.", last.TextContent);

        // A speed dial is a menu to the helpers, found by its label.
        await cut.Tb().Menu("Add").ChooseAsync("Reminder");

        Assert.Equal("Added a reminder.", last.TextContent);
    }
}
