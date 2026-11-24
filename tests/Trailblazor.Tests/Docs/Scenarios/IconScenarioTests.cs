using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class IconScenarioTests : BunitContext
{
    public IconScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Results_ShouldBeNamedForScreenReaders_WhenTheirIconsHaveLabels()
    {
        var cut = Render<IconMeaningExample>();

        // An icon has no helper of its own; one with a Label is an image named by that label.
        Assert.Equal(["Passed", "Failed"], cut.FindAll("[role=img]").Select(icon => icon.GetAttribute("aria-label")));
    }
}
