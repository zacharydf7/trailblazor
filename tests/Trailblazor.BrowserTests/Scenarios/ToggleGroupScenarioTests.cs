using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ToggleGroupScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Seasons_ShouldGiveTheAppThePressedSeasons_WhenSeasonsAreTurnedOnAndOff(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toggle-group/any-number");
        var seasons = page.Tb().ToggleGroup("Seasons open");
        Assert.Equal(["Summer"], await seasons.ChosenAsync());

        // ChooseAsync presses the items that aren't pressed yet, and UnchooseAsync the ones that are.
        await seasons.ChooseAsync("Spring", "Fall");
        Assert.Equal(["Spring", "Summer", "Fall"], await seasons.ChosenAsync());

        await seasons.UnchooseAsync("Summer");
        await Expect(page.GetByText("Open in: Spring, Fall")).ToBeVisibleAsync();
    }
}
