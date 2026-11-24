using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class HighlightScenarioTests : BunitContext
{
    public HighlightScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailList_ShouldMarkWhatMatches_WhenTheSearchChanges()
    {
        var cut = Render<HighlightExample>();
        // The search starts as "pine", so the two Pine trails show with the word marked.
        Assert.Equal(["Pine", "Pine"], cut.FindAll("ul [data-part=match]").Select(match => match.TextContent));

        await cut.Tb().TextField("Search trails").EnterAsync("falls");

        Assert.Equal(["Cedar Falls walk"], cut.FindAll("ul li").Select(trail => trail.TextContent));
        Assert.Equal(["Falls"], cut.FindAll("ul [data-part=match]").Select(match => match.TextContent));
    }
}
