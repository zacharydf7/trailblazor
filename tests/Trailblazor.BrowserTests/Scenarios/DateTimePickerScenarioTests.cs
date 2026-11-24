using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DateTimePickerScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task HikeStart_ShouldHoldTheChosenDateAndTime_WhenBothAreChosen(string browser)
    {
        // Playwright's WebKit for Windows shows the time as a plain text box, whose edits never reach the page.
        Assert.SkipWhen(browser == "webkit" && OperatingSystem.IsWindows(), "WebKit on Windows has no time box.");
        var page = await OpenAsync(browser, "/examples/date-time-picker/value");
        var start = page.Tb().DateTimePicker("Guided hike starts");
        var hike = new DateTime(2027, 5, 14, 7, 45, 0);

        // ChooseAsync types the date in the field's own format, fills the time, and waits until the field has both.
        await start.ChooseAsync(hike);

        Assert.Equal(hike, await start.ValueAsync());
        await Expect(page.Locator("output")).ToHaveTextAsync($"Starting {hike:f}.");
    }
}
