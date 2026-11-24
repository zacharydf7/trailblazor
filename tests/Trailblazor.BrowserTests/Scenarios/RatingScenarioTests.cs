using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class RatingScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RidgeLoop_ShouldGiveTheAppItsStars_WhenRatedAndCleared(string browser)
    {
        var page = await OpenAsync(browser, "/examples/rating/basic");
        var rating = page.Tb().Rating("Rate the ridge loop");
        Assert.Equal(0, await rating.ValueAsync());

        await rating.RateAsync(4);
        Assert.Equal(4, await rating.ValueAsync());
        await Expect(page.GetByText("You gave it 4 of 5.")).ToBeVisibleAsync();

        await rating.ClearAsync();
        Assert.Equal(0, await rating.ValueAsync());
        await Expect(page.GetByText("Not rated yet.")).ToBeVisibleAsync();
    }
}
