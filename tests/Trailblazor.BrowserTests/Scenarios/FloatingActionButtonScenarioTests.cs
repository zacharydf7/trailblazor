using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class FloatingActionButtonScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SpeedDial_ShouldRunTheChosenAction_WhenAnActionIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/floating-action-button/speed-dial");
        var last = page.GetByLabel("Last action");
        await Expect(last).ToHaveTextAsync("Nothing added yet.");

        // A speed dial is a menu to the helpers, found by its label.
        await page.Tb().Menu("Add").ChooseAsync("Reminder");

        await Expect(last).ToHaveTextAsync("Added a reminder.");
    }
}
