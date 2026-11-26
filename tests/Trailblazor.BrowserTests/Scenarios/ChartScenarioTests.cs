using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ChartScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task HikersByMonth_ShouldSayWhatItDrawsAndHoldJulysValue_WhenRead(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/basic");
        var chart = page.GetByRole(AriaRole.Figure, new() { Name = "Hikers by month" });

        // A chart has no helper of its own: it is a figure named by its title, with its drawing as an image inside.
        await Expect(chart.GetByRole(AriaRole.Img)).ToHaveAccessibleNameAsync("Bar chart: Hikers");
        // Values are read from the data table, which opens under the chart; each row's header is the item's name.
        await chart.GetByText("Data table").ClickAsync();
        await Expect(chart.GetByRole(AriaRole.Row, new() { Name = "Jul" }).GetByRole(AriaRole.Cell))
            .ToHaveTextAsync("1,460");
    }
}
