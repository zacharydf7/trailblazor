using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SegmentedControlScenarioTests : BunitContext
{
    public SegmentedControlScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Difficulty_ShouldGiveTheAppTheLevel_WhenAnotherSegmentIsChosen()
    {
        var cut = Render<SegmentedControlExample>();
        // A segmented control is a radio group drawn as segments, so the radio group's helper drives it.
        var difficulty = cut.Tb().RadioGroup("Trail difficulty");
        Assert.Equal("Moderate", difficulty.Chosen);

        await difficulty.ChooseAsync("Hard");

        Assert.Equal("Hard", difficulty.Chosen);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Showing hard trails.");
    }
}
