using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AutocompleteScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Trailhead_ShouldGiveTheAppTheChosenTrailhead_WhenSearchedChosenAndCleared(string browser)
    {
        var page = await OpenAsync(browser, "/examples/autocomplete/search");
        var trailhead = page.Tb().Autocomplete("Trailhead");

        // TypeAsync types the text and waits for the search to answer. A suggestion is named by all the text it shows.
        await trailhead.TypeAsync("north");
        Assert.Equal(["Pine Hollow North ridge", "Eagle Ridge North ridge"], await trailhead.SuggestionsAsync());

        // ChooseAsync types its own search, then clicks the suggestion.
        await trailhead.ChooseAsync("eagle", "Eagle Ridge North ridge");
        Assert.Equal("Eagle Ridge", await trailhead.TextAsync());
        await Expect(page.GetByText("Starting at: Eagle Ridge")).ToBeVisibleAsync();

        await trailhead.ClearAsync();
        await Expect(page.GetByText("Starting at: nowhere yet")).ToBeVisibleAsync();
    }
}
