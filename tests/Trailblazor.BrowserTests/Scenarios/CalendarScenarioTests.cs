using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CalendarScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task HikeDate_ShouldGiveTheAppTheDay_WhenADayIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/calendar/value");
        var hike = page.Tb().Calendar("Hike date");
        var date = DateOnly.FromDateTime(DateTime.Today).AddDays(10);

        // ChooseAsync picks the day as a user would, through the calendar's year and month views.
        await hike.ChooseAsync(date);

        await Expect(page.Locator("output")).ToHaveTextAsync($"Hiking on {date:D}.");
    }
}
