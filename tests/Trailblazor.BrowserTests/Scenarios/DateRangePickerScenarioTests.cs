using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DateRangePickerScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TripDates_ShouldGiveTheAppTheRange_WhenBothDatesAreChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/date-range-picker/range-and-value");
        var trip = page.Tb().DateRangePicker("Trip dates");
        var summary = page.Locator("output");
        var (start, end) = (new DateOnly(2027, 6, 4), new DateOnly(2027, 6, 8));

        // The helper takes both dates, so the test doesn't depend on how the user's language writes a date.
        await trip.ChooseAsync(start, end);

        Assert.Equal((start, end), await trip.RangeAsync());
        await Expect(summary).ToHaveTextAsync($"5 days, from {start:D}.");
    }
}
