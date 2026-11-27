using System.Globalization;
using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class PieChartScenarioTests : BunitContext
{
    public PieChartScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task RescuesByCause_ShouldShowTheRangersAdvice_WhenACausesSliceIsClicked()
    {
        var cut = Render<PieChartClickExample>();
        var chart = cut.Tb().PieChart("Rescues by cause");

        // Values and shares are read as the chart's data table writes them, by the item's label. A share is written
        // as the current culture writes a percentage, "28%" in English.
        Assert.Equal(["Lost", "Injury", "Weather", "Exhaustion"], chart.Slices);
        Assert.Equal("9", chart.Value("Injury"));
        Assert.Equal(0.28.ToString("P0", CultureInfo.CurrentCulture), chart.Share("Injury"));

        // A click on a slice returns once the app's OnSliceClick has run.
        await chart.ClickSliceAsync("Injury");
        Assert.Contains("Injury: 9 rescues. Wear boots that hold your ankles.", cut.Markup);
    }
}
