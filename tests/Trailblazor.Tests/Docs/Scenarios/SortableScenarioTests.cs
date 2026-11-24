using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SortableScenarioTests : BunitContext
{
    public SortableScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Alerts_ShouldTakeTheNewOrderAndKeepTheLastRuleLast_WhenRulesAreMoved()
    {
        var cut = Render<SortableExample>();
        var alerts = cut.Tb().Sortable("Trail alerts, first match wins");
        Assert.Equal(["Trail closed", "Storm warning", "Parking full", "Everything else"], alerts.Items);

        // MoveAsync uses the item's handle menu, as a user who can't drag does.
        await alerts.MoveAsync("Parking full", 0);
        Assert.Equal(["Parking full", "Trail closed", "Storm warning", "Everything else"], alerts.Items);

        // The app refuses a move that would put another rule last, so the order stays.
        await alerts.MoveAsync("Trail closed", 3);
        Assert.Equal(["Parking full", "Trail closed", "Storm warning", "Everything else"], alerts.Items);
    }
}
