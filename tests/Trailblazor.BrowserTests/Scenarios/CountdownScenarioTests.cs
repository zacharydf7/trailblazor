using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CountdownScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ResetRedirect_ShouldStop_WhenTheUserStays(string browser)
    {
        var page = await OpenAsync(browser, "/examples/countdown/basic");

        // A countdown has no helper of its own; its buttons are TbButtons, found by their names.
        await page.Tb().Button("Stay on this page").ClickAsync();

        await Expect(page.Locator(".tb-countdown")).ToHaveTextAsync("Stayed on this page");
        await Expect(page.Locator(".tb-countdown")).ToBeFocusedAsync();
    }
}
