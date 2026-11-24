using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CascaderScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Trailhead_ShouldGiveTheAppTheLot_WhenAPathIsChosenAndAnotherIsSearchedFor(string browser)
    {
        var page = await OpenAsync(browser, "/examples/cascader/basic");
        var trailhead = page.Tb().Cascader("Trailhead");
        var value = page.Locator("output");

        // ChooseAsync clicks through the lists, one choice a level.
        await trailhead.ChooseAsync("North ridge", "Pine Hollow", "Upper lot");
        await Expect(value).ToHaveTextAsync("Trailhead value: pine-upper");

        // ChooseResultAsync types the text and chooses a path that matches, named as the results list shows it.
        await trailhead.ChooseResultAsync("overflow", "North ridge / Eagle Ridge / Overflow lot");
        Assert.Equal("North ridge / Eagle Ridge / Overflow lot", await trailhead.TextAsync());
        await Expect(value).ToHaveTextAsync("Trailhead value: eagle-overflow");
    }
}
