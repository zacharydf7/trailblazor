using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class EmptyStateScenarioTests : BunitContext
{
    public EmptyStateScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task NoTrails_ShouldOpenTheTrailFinder_WhenItsActionIsClicked()
    {
        var cut = Render<EmptyStateExample>();
        Assert.Equal("No trails saved yet", cut.Find("h3").TextContent);

        // The empty state has no helper of its own; its action is a TbButton, found by its name.
        await cut.Tb().Button("Find a trail").ClickAsync();

        Assert.Equal("Opened the trail finder.", cut.Find("output").TextContent);
    }
}
