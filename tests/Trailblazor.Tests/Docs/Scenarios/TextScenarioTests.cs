using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TextScenarioTests : BunitContext
{
    public TextScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailNames_ShouldShowTheWholeOfTheOneCutOff_WhenItIsTooLong()
    {
        const string name = "Pine Hollow loop climbs through old cedar groves to a lookout over the river valley.";
        var cut = Render<TextFullTextExample>();
        var pine = cut.Tb().Text(name);

        // Only a browser measures the box, so SetCutAsync tells the text it is cut off, as its script would.
        await pine.SetCutAsync(true);

        Assert.True(pine.IsCut);
        Assert.Equal(name, cut.Find(".tb-text--truncate > [role=tooltip]").TextContent.Trim());
    }
}
