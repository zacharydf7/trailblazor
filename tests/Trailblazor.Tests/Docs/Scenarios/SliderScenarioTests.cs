using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SliderScenarioTests : BunitContext
{
    public SliderScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Elevation_ShouldGiveTheAppBothEnds_WhenTheRangeIsSet()
    {
        var cut = Render<SliderRangeExample>();
        var elevation = cut.Tb().RangeSlider("Elevation");
        var value = cut.Find("output");
        Assert.Equal((800, 2400), elevation.Range);

        // SetAsync moves each thumb as dragging it does.
        await elevation.SetAsync(1200, 3000);

        Assert.Equal((1200, 3000), elevation.Range);
        Assert.Equal("Between 1200 and 3000 m", value.TextContent);
    }
}
