using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AutocompleteScenarioTests : BunitContext
{
    public AutocompleteScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Trailhead_ShouldGiveTheAppTheChosenTrailhead_WhenSearchedChosenAndCleared()
    {
        var cut = Render<AutocompleteExample>();
        var trailhead = cut.Tb().Autocomplete("Trailhead");

        // TypeAsync types the text and waits for the search to answer. A suggestion is named by all the text it shows.
        await trailhead.TypeAsync("north");
        Assert.Equal(["Pine Hollow North ridge", "Eagle Ridge North ridge"], trailhead.Suggestions);

        // ChooseAsync types its own search, then clicks the suggestion.
        await trailhead.ChooseAsync("eagle", "Eagle Ridge North ridge");
        Assert.Equal("Eagle Ridge", trailhead.Text);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Starting at: Eagle Ridge");

        await trailhead.ClearAsync();
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Starting at: nowhere yet");
    }
}
