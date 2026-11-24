using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TreeSelectScenarioTests : BunitContext
{
    public TreeSelectScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Trail_ShouldGiveTheAppTheChosenTrail_WhenItIsFoundByFilteringAndChosen()
    {
        var cut = Render<TreeSelectExample>();
        var trail = cut.Tb().TreeSelect("Trail");
        var chosen = cut.Find("output");
        Assert.Equal("No trail chosen.", chosen.TextContent);

        // FilterAsync types in the field's filter box, and a row is found by the texts along its path.
        await trail.FilterAsync("fern");
        await trail.ChooseAsync("River valley", "Fern Gully");

        Assert.Equal("Fern Gully", trail.Text);
        Assert.Equal("Chosen: fern.", chosen.TextContent);
    }
}
