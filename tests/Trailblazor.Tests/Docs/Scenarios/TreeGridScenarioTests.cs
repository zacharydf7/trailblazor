using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TreeGridScenarioTests : BunitContext
{
    public TreeGridScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Trails_ShouldShowAMatchUnderItsParkAndRegion_WhenFilteredByName()
    {
        var cut = Render<TreeGridExample>();
        var trails = cut.Tb().Grid("Trails by region");
        Assert.Equal(["North ridge", "South valley"], trails.Rows.Select(row => row[0]));

        // A filter opens the rows a match is nested under, so it shows where the trail is.
        await trails.FilterAsync("Name", "Eagle");
        Assert.Equal(["North ridge", "Pine park", "Eagle Ridge"], trails.Rows.Select(row => row[0]));

        // ExpandAsync opens a row, found by the text of one of its cells.
        await trails.FilterAsync("Name", "");
        await trails.ExpandAsync("South valley");
        Assert.Equal(["North ridge", "South valley", "River park"], trails.Rows.Select(row => row[0]));
    }
}
