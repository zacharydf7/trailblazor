namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class GaugeScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SummitWind_ShouldSayItsValueAndRange_WhenTheWeatherStationIsConnected(string browser)
    {
        var page = await OpenAsync(browser, "/examples/gauge/empty");

        // A gauge is a meter, found by its role and its name. Its value text is what it shows and what screen
        // readers hear.
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Summit wind (km/h)" });
        await Expect(gauge).ToHaveAttributeAsync("aria-valuetext", "Offline");

        await page.GetByRole(AriaRole.Button, new() { Name = "Connect the weather station" }).ClickAsync();

        // The text holds the value and the name of the range the value is in.
        await Expect(gauge).ToHaveAttributeAsync("aria-valuetext", "52, Windy");
    }
}
