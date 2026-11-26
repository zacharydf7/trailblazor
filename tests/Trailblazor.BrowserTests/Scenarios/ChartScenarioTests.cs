using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ChartScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RescuesByMonth_ShouldShowAMonthsBusiestDay_WhenItsBarIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/click");
        var chart = page.Tb().Chart("Rescues by month");

        // Values are read as the chart's data table writes them, by series and place.
        Assert.Equal(["Rescues"], await chart.SeriesAsync());
        Assert.Equal("9", await chart.ValueAsync("Rescues", "Jul"));

        // A click on a point returns once the app's OnPointClick has run.
        await chart.ClickPointAsync("Rescues", "Jul");
        await Expect(page.GetByText("Jul: 9 rescues, most of them on July 4.")).ToBeVisibleAsync();
    }
}
