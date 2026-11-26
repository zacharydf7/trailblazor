using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ChartScenarioTests : BunitContext
{
    public ChartScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task RescuesByMonth_ShouldShowAMonthsBusiestDay_WhenItsBarIsClicked()
    {
        var cut = Render<ChartClickExample>();
        var chart = cut.Tb().Chart("Rescues by month");

        // Values are read as the chart's data table writes them, by series and place.
        Assert.Equal(["Rescues"], chart.Series);
        Assert.Equal("9", chart.Value("Rescues", "Jul"));

        // A click on a point returns once the app's OnPointClick has run.
        await chart.ClickPointAsync("Rescues", "Jul");
        Assert.Contains("Jul: 9 rescues, most of them on July 4.", cut.Markup);
    }
}
