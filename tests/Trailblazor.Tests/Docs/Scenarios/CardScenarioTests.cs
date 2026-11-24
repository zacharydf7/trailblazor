using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CardScenarioTests : BunitContext
{
    public CardScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailCards_ShouldKeepWhatIsSaved_WhenTheirSaveButtonsAreToggled()
    {
        var cut = Render<CardLinkExample>();

        // Each card's Save button is named for its trail, and toggles.
        await cut.Tb().Button("Save Old mill").ClickAsync();
        await cut.Tb().Button("Save Lake shore").ClickAsync();
        await cut.Tb().Button("Save Old mill").ClickAsync();

        Assert.False(cut.Tb().Button("Save Old mill").Pressed);
        Assert.True(cut.Tb().Button("Save Lake shore").Pressed);

        // A card's title is its link; bUnit doesn't follow a plain link, so the test checks where it goes.
        var link = cut.FindAll("article [data-part=link]").Single(link => link.TextContent == "Pine hollow");
        Assert.EndsWith("#pine-hollow", link.GetAttribute("href"));
    }
}
