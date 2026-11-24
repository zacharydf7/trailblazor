using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ProgressScenarioTests : BunitContext
{
    public ProgressScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Progress_ShouldSayItsValueOrNone_WhenGivenAValueOrNot()
    {
        var cut = Render<ProgressExample>();

        // A progress bar has no helper of its own; it is a progressbar named by its AriaLabel.
        Assert.Equal("25", cut.Find("[role=progressbar][aria-label='Upload']").GetAttribute("aria-valuenow"));
        // One without a Value is indeterminate, so it has no aria-valuenow.
        Assert.False(cut.Find("[role=progressbar][aria-label='Loading the trails']").HasAttribute("aria-valuenow"));
    }
}
