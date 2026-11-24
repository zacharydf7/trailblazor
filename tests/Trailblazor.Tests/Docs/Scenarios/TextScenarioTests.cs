using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TextScenarioTests : BunitContext
{
    public TextScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Headings_ShouldBeTheTextGivenAHeadingElement_WhenRead()
    {
        var cut = Render<TextElementExample>();

        // Text has no helper of its own. Its look doesn't make it a heading; its As does.
        Assert.Equal("A small heading that's still a heading", cut.Find("h3").TextContent);
        Assert.Single(cut.FindAll("h1, h2, h3, h4, h5, h6"));
    }
}
