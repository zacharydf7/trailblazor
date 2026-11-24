using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class HighlightScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailList_ShouldMarkWhatMatches_WhenTheSearchChanges(string browser)
    {
        var page = await OpenAsync(browser, "/examples/highlight/basic");
        var trails = page.GetByRole(AriaRole.List, new() { Name = "Trails" });
        // The search starts as "pine", so the two Pine trails show with the word marked.
        await Expect(trails.Locator("[data-part=match]")).ToHaveTextAsync(["Pine", "Pine"]);

        await page.Tb().TextField("Search trails").EnterAsync("falls");

        // Each trail's text reads whole, since the marks don't split it.
        await Expect(trails.GetByRole(AriaRole.Listitem)).ToHaveTextAsync(["Cedar Falls walk"]);
        await Expect(trails.Locator("[data-part=match]")).ToHaveTextAsync(["Falls"]);
    }
}
