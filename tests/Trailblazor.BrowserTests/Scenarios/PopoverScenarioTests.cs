using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class PopoverScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filters_ShouldKeepTheTickedFilter_WhenThePopoverIsClosedWithApplyAndOpenedAgain(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover/opening-from-code");
        // A popover is found by the name of the button that opens it, and OpenAsync returns its panel.
        var filters = await page.Tb().Popover("Filters").OpenAsync();

        await page.Tb().Checkbox("Only open items").CheckAsync();
        await page.Tb().Button("Apply").ClickAsync();
        await Expect(filters).ToBeHiddenAsync();
        await Expect(page.GetByLabel("Filter state")).ToHaveTextAsync("Open items");

        // The app opens the popover again from its own button, through @bind-Open.
        await page.Tb().Button("Show filter options").ClickAsync();
        await Expect(filters).ToBeVisibleAsync();
        Assert.True(await page.Tb().Checkbox("Only open items").CheckedAsync());
    }
}
