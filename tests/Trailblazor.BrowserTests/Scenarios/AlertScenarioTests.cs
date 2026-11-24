using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AlertScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Warning_ShouldGoAndComeBack_WhenItIsClosedAndShownAgain(string browser)
    {
        var page = await OpenAsync(browser, "/examples/alert/closing");
        // An alert is found by its text.
        var warning = page.Tb().Alert("Close this alert with its button.");

        // CloseAsync presses the alert's close button, and the app shows a button in its place.
        await warning.CloseAsync();
        Assert.Empty(await page.Tb().AlertsAsync());

        // AlertsAsync reads the text of every alert that shows.
        await page.Tb().Button("Show the alert again").ClickAsync();
        Assert.Equal(["Close this alert with its button."], await page.Tb().AlertsAsync());
    }
}
