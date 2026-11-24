using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ImageScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailPhotos_ShouldTellTheAppWhichLoadedAndWhichFailed_WhenTheyLoad(string browser)
    {
        var page = await OpenAsync(browser, "/examples/image/events");

        // An image has no helper of its own; the browser loads one photo and fails on the missing one.
        await Expect(page.GetByLabel("Image state")).ToHaveTextAsync("Loaded: True. Failed: True.");
        // The failed photo keeps its name from its Alt.
        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "The lookout over the river valley" }))
            .ToBeVisibleAsync();
    }
}
