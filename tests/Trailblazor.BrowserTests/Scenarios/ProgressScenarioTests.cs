using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ProgressScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Progress_ShouldSayItsValueOrNone_WhenGivenAValueOrNot(string browser)
    {
        var page = await OpenAsync(browser, "/examples/progress/value");

        // A progress bar has no helper of its own; it is a progressbar named by its AriaLabel.
        var upload = page.GetByRole(AriaRole.Progressbar, new() { Name = "Upload" });
        await Expect(upload).ToHaveAttributeAsync("aria-valuenow", "25");
        // One without a Value is indeterminate, so it has no aria-valuenow.
        var loading = page.GetByRole(AriaRole.Progressbar, new() { Name = "Loading the trails" });
        await Expect(loading).ToBeVisibleAsync();
        Assert.Null(await loading.GetAttributeAsync("aria-valuenow"));
    }
}
