using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SkeletonScenarioTests : BunitContext
{
    public SkeletonScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Trip_ShouldShowAndBeUsable_WhenLoadingEnds()
    {
        var cut = Render<SkeletonWrapExample>();
        // While it loads, the region is busy and its content is inert, out of reach.
        Assert.Equal("true", cut.Find("[aria-label=Trip]").GetAttribute("aria-busy"));
        Assert.True(cut.Find("[aria-label=Trip] [data-part=content]").HasAttribute("inert"));

        // In an app, loading ends when the data comes; here the example's switch ends it.
        await cut.Tb().Switch("Loading the trip").UncheckAsync();

        Assert.Null(cut.Find("[aria-label=Trip]").GetAttribute("aria-busy"));
        Assert.False(cut.Find("[aria-label=Trip] [data-part=content]").HasAttribute("inert"));
    }
}
