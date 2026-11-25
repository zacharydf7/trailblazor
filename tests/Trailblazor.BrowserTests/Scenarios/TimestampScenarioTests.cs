using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TimestampScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Departure_ShouldShowInTokyoTime_WhenTokyoIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/timestamp/zones");

        // The timestamp has no helper; it is a time element, read by its text and its datetime.
        var departure = page.Locator("time").First;
        await Expect(departure).ToHaveTextAsync(new Regex(@"UTC\+9$"));
        await Expect(departure).ToHaveAttributeAsync("datetime", "2026-12-04T14:30:00.000+00:00");
    }
}
