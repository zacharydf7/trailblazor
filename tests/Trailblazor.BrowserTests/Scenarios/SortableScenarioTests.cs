using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SortableScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Alerts_ShouldTakeTheNewOrderAndKeepTheLastRuleLast_WhenRulesAreMoved(string browser)
    {
        var page = await OpenAsync(browser, "/examples/sortable/order");
        var alerts = page.Tb().Sortable("Trail alerts, first match wins");
        Assert.Equal(["Trail closed", "Storm warning", "Parking full", "Everything else"], await alerts.ItemsAsync());

        // MoveAsync uses the item's handle menu, as a user who can't drag does.
        await alerts.MoveAsync("Parking full", 0);
        Assert.Equal(["Parking full", "Trail closed", "Storm warning", "Everything else"], await alerts.ItemsAsync());

        // The app refuses a move that would put another rule last, so the order stays.
        await alerts.MoveAsync("Trail closed", 3);
        Assert.Equal(["Parking full", "Trail closed", "Storm warning", "Everything else"], await alerts.ItemsAsync());
    }
}
