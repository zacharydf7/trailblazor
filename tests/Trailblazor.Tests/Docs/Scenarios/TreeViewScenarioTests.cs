using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TreeViewScenarioTests : BunitContext
{
    public TreeViewScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Trails_ShouldShowOnlyMatchesAndOpenATrail_WhenFilteredAndActivated()
    {
        var cut = Render<TreeViewExample>();
        var trails = cut.Tb().Tree("Trails by region");

        // The filter keeps the rows that match and the regions they sit in.
        await cut.Tb().TextField("Find a trail").EnterAsync("cedar");
        Assert.Equal(["River valley", "Cedar Flats"], trails.Texts);

        // A row is found by the texts along its path. ActivateAsync does what a double click or Enter does.
        await trails.ActivateAsync("River valley", "Cedar Flats");
        Assert.Contains(cut.FindAll("p"), text => text.TextContent.Trim() == "Opened Cedar Flats.");
    }
}
