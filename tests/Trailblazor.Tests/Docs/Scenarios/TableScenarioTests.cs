using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TableScenarioTests : BunitContext
{
    public TableScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task OpeningHours_ShouldListTheCentersAndTheirHours_WhenRead()
    {
        var cut = Render<TableExample>();
        var hours = cut.Find("table[aria-label='Opening hours']");

        // A table has no helper of its own; its markup is the table the app wrote, read like any other.
        Assert.Equal(["North ridge", "River valley", "High peaks"],
            hours.QuerySelectorAll("tbody th").Select(center => center.TextContent));
        var highPeaks = hours.QuerySelectorAll("tbody tr")
            .Single(row => row.QuerySelector("th")!.TextContent == "High peaks");
        Assert.Equal(["Closed", "9–15"], highPeaks.QuerySelectorAll("td").Select(cell => cell.TextContent));
    }
}
