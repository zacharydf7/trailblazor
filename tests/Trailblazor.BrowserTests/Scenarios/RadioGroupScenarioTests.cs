using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class RadioGroupScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ArrivalDay_ShouldShowTheAppsErrorUntilAnotherDayIsChosen_WhenTheDayIsChecked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/radio-group/error");
        var day = page.Tb().RadioGroup("Arrival day");
        Assert.Equal("Saturday", await day.ChosenAsync());

        // The app's answer shows on the group until the day changes.
        await page.Tb().Button("Check the day").ClickAsync();
        Assert.Equal("Saturday is fully booked.", await day.ErrorAsync());

        await day.ChooseAsync("Friday");

        Assert.Equal("Friday", await day.ChosenAsync());
        Assert.Null(await day.ErrorAsync());
    }
}
