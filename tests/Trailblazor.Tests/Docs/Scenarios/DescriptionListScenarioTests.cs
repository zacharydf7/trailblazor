using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DescriptionListScenarioTests : BunitContext
{
    public DescriptionListScenarioTests() => this.AddTrailblazor();

    [Fact]
    public void RemovedTrail_ShouldShowWhoRemovedItAndWhy_WhenRendered()
    {
        var cut = Render<DescriptionListExample>();

        var details = cut.Tb().DescriptionList();

        Assert.Equal("Robin Hale", details.ValueOf("Removed by"));
        Assert.StartsWith("Merged into the Cedar Flats loop", details.ValueOf("Note"));
    }
}
