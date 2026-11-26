using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TimePickerScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StartTime_ShouldGiveTheAppTheTime_WhenATimeIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/time-picker/value");
        var start = page.Tb().TimePicker("Start time");
        var time = new TimeOnly(7, 45);

        // The helper takes a TimeOnly, so the test doesn't depend on how the user's language writes a time.
        await start.ChooseAsync(time);

        Assert.Equal(time, await start.ValueAsync());
        await Expect(page.Locator("output")).ToHaveTextAsync($"Starting at {time:t}.");
    }
}
