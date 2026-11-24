using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SelectScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Topics_ShouldGiveTheAppWhatIsChosen_WhenTopicsAreChosenTakenAwayAndCleared(string browser)
    {
        var page = await OpenAsync(browser, "/examples/select/multiple");
        var topics = page.Tb().Select("Notify me about");
        var value = page.Locator("output");

        await topics.ChooseAsync("Returns", "Offers");
        Assert.Equal(["Orders", "Returns", "Offers"], await topics.ChosenAsync());
        await Expect(value).ToHaveTextAsync("Notifications value: Orders, Returns, Offers");

        await topics.UnchooseAsync("Orders");
        await Expect(value).ToHaveTextAsync("Notifications value: Returns, Offers");

        await topics.ClearAsync();
        await Expect(value).ToHaveTextAsync("Notifications value:");
    }
}
