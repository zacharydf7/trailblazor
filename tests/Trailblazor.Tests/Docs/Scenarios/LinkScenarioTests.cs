using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class LinkScenarioTests : BunitContext
{
    public LinkScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task BatchLink_ShouldShowTheBatchId_WhenClicked()
    {
        var cut = Render<LinkButtonExample>();
        Assert.DoesNotContain("2041-B", cut.Markup);

        // A link with no address runs the app's handler, and ClickAsync waits for it.
        await cut.Tb().Link("Show the batch ID").ClickAsync();

        Assert.StartsWith("Batch 2041-B", cut.Find("p").TextContent.Trim());
    }
}
