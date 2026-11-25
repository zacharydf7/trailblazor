using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CopyButtonScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task InstallCommand_ShouldSayItCopied_WhenCopied(string browser)
    {
        var page = await OpenAsync(browser, "/examples/copy-button/basic");
        // Chromium copies only for a page granted the clipboard.
        if (browser == "chromium")
        {
            await page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        }

        var copy = page.Tb().CopyButton();

        await copy.CopyAsync();

        await Expect(copy.Element).ToHaveTextAsync("Copied");
    }
}
