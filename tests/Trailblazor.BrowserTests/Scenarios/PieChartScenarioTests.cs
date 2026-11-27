using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class PieChartScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RescuesByCause_ShouldShowTheRangersAdvice_WhenACausesSliceIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pie-chart/click");
        var chart = page.Tb().PieChart("Rescues by cause");

        // Values and shares are read as the chart's data table writes them, by the item's label.
        Assert.Equal(["Lost", "Injury", "Weather", "Exhaustion"], await chart.SlicesAsync());
        Assert.Equal("9", await chart.ValueAsync("Injury"));
        Assert.Equal("28%", await chart.ShareAsync("Injury"));

        // A click on a slice returns once the app's OnSliceClick has run.
        await chart.ClickSliceAsync("Injury");
        await Expect(page.GetByText("Injury: 9 rescues. Wear boots that hold your ankles.")).ToBeVisibleAsync();
    }
}
