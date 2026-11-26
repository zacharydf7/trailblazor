using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ChartScenarioTests : BunitContext
{
    public ChartScenarioTests() => this.AddTrailblazor();

    [Fact]
    public void HikersByMonth_ShouldSayWhatItDrawsAndHoldJulysValue_WhenRead()
    {
        var cut = Render<ChartExample>();
        var chart = cut.Find("figure");

        // A chart has no helper of its own: it is a figure named by its title, with its drawing as an image inside.
        Assert.Equal("Hikers by month", chart.QuerySelector("figcaption")!.TextContent);
        Assert.Equal("Bar chart: Hikers", chart.QuerySelector("[role=img]")!.GetAttribute("aria-label"));
        // Values are read from the data table, where each row's header is the item's name.
        var july = chart.QuerySelectorAll("tbody tr").Single(row => row.QuerySelector("th")!.TextContent == "Jul");
        Assert.Equal("1,460", july.QuerySelector("td")!.TextContent);
    }
}
