using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CascaderScenarioTests : BunitContext
{
    public CascaderScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Trailhead_ShouldGiveTheAppTheLot_WhenAPathIsChosenAndAnotherIsSearchedFor()
    {
        var cut = Render<CascaderExample>();
        var trailhead = cut.Tb().Cascader("Trailhead");
        var value = cut.Find("output");

        // ChooseAsync clicks through the lists, one choice a level.
        await trailhead.ChooseAsync("North ridge", "Pine Hollow", "Upper lot");
        Assert.Equal("Trailhead value: pine-upper", value.TextContent);

        // ChooseResultAsync types the text and chooses a path that matches, named as the results list shows it.
        await trailhead.ChooseResultAsync("overflow", "North ridge / Eagle Ridge / Overflow lot");
        Assert.Equal("North ridge / Eagle Ridge / Overflow lot", trailhead.Text);
        Assert.Equal("Trailhead value: eagle-overflow", value.TextContent);
    }
}
