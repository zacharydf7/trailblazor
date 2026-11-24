using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CarouselScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FeaturedTrails_ShouldGiveTheAppTheSlide_WhenTheSlidesAreStepped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/carousel/basic");
        var featured = page.Tb().Carousel("Featured trails");

        await featured.NextAsync();
        Assert.Equal(1, await featured.ActiveIndexAsync());
        await Expect(page.GetByText("Showing Cedar Flats.")).ToBeVisibleAsync();

        // GoToAsync shows a slide by its place, counted from 0.
        await featured.GoToAsync(2);
        Assert.Equal(2, await featured.ActiveIndexAsync());
        await Expect(page.GetByText("Showing Eagle Ridge traverse.")).ToBeVisibleAsync();
    }
}
