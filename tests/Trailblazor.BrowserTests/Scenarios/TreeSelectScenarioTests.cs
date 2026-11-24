using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TreeSelectScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Trail_ShouldGiveTheAppTheChosenTrail_WhenItIsFoundByFilteringAndChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select/choose-one");
        var trail = page.Tb().TreeSelect("Trail");
        var chosen = page.Locator("output");
        await Expect(chosen).ToHaveTextAsync("No trail chosen.");

        // FilterAsync types in the field's filter box, and a row is found by the texts along its path.
        await trail.FilterAsync("fern");
        await trail.ChooseAsync("River valley", "Fern Gully");

        Assert.Equal("Fern Gully", await trail.TextAsync());
        await Expect(chosen).ToHaveTextAsync("Chosen: fern.");
    }
}
