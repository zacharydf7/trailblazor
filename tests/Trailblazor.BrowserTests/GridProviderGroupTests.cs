using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridProviderGroupTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ExpandGroupAsync_ShouldWaitForTheProvidersRows_WhenTheGroupOpens(string browser)
    {
        var page = await OpenAsync(browser, "/grid-provider-groups");
        var grid = page.Tb().Grid("Hikes");
        await Expect(page.Locator("[data-part=group-toggle]")).ToHaveCountAsync(2);

        await grid.ExpandGroupAsync("Region: North ridge");

        Assert.Equal([["Eagle Ridge", "North ridge"], ["Pine Hollow", "North ridge"]], await grid.RowsAsync());
    }
}
