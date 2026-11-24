using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SegmentedControlScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Difficulty_ShouldGiveTheAppTheLevel_WhenAnotherSegmentIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/segmented-control/segments");
        // A segmented control is a radio group drawn as segments, so the radio group's helper drives it.
        var difficulty = page.Tb().RadioGroup("Trail difficulty");
        Assert.Equal("Moderate", await difficulty.ChosenAsync());

        await difficulty.ChooseAsync("Hard");

        Assert.Equal("Hard", await difficulty.ChosenAsync());
        await Expect(page.GetByText("Showing hard trails.")).ToBeVisibleAsync();
    }
}
