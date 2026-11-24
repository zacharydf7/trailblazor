using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DatePickerScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StartDate_ShouldGiveTheAppTheDate_WhenADateIsChosenAndCleared(string browser)
    {
        var page = await OpenAsync(browser, "/examples/date-picker/value");
        var start = page.Tb().DatePicker("Start date");
        var shown = page.Locator("output");
        var date = new DateOnly(2027, 5, 14);

        // The helper takes a DateOnly, so the test doesn't depend on how the user's language writes a date.
        await start.ChooseAsync(date);
        Assert.Equal(date, await start.ValueAsync());
        await Expect(shown).ToHaveTextAsync($"Starting {date:D}.");

        await start.ChooseAsync(null);
        Assert.Null(await start.ValueAsync());
        await Expect(shown).ToHaveTextAsync("No date chosen yet.");
    }
}
