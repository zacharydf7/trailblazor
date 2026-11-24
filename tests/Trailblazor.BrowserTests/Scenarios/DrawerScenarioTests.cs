using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DrawerScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filters_ShouldKeepTheTickedFiltersUntilReset_WhenTheDrawerIsClosedAndOpenedAgain(string browser)
    {
        var page = await OpenAsync(browser, "/examples/drawer/basic");
        // A drawer is a dialog to the helpers, found by its title.
        var filters = page.Tb().Dialog("Trail filters");

        await page.Tb().Button("Filters").ClickAsync();
        await page.Tb().Checkbox("Dogs allowed").CheckAsync();
        await filters.ChooseAsync("Show trails");
        Assert.False(await filters.IsOpenAsync());

        // The app keeps the filters, so they're still ticked when the drawer opens again.
        await page.Tb().Button("Filters").ClickAsync();
        Assert.True(await page.Tb().Checkbox("Dogs allowed").CheckedAsync());

        // ChooseAsync waits for the drawer to close. Reset leaves it open, so it's clicked as any button is.
        await page.Tb().Button("Reset").ClickAsync();
        Assert.False(await page.Tb().Checkbox("Dogs allowed").CheckedAsync());
    }
}
