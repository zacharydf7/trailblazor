using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SliderScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Elevation_ShouldGiveTheAppBothEnds_WhenTheRangeIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider/range");
        var elevation = page.Tb().RangeSlider("Elevation");
        var value = page.Locator("output");
        Assert.Equal((800, 2400), await elevation.RangeAsync());

        // SetAsync moves each thumb as dragging it does, and waits until the app has both ends.
        await elevation.SetAsync(1200, 3000);

        Assert.Equal((1200, 3000), await elevation.RangeAsync());
        await Expect(value).ToHaveTextAsync("Between 1200 and 3000 m");
    }
}
