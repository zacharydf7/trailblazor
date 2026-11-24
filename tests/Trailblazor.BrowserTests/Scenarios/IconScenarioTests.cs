using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class IconScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Results_ShouldBeNamedForScreenReaders_WhenTheirIconsHaveLabels(string browser)
    {
        var page = await OpenAsync(browser, "/examples/icon/meaning");

        // An icon has no helper of its own; one with a Label is an image named by that label.
        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "Passed" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "Failed" })).ToBeVisibleAsync();
    }
}
