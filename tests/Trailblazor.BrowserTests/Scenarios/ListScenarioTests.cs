using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ListScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Campsites_ShouldShowTheChosenSiteAndLoseARemovedOne_WhenItemsAreClickedAndRemoved(string browser)
    {
        var page = await OpenAsync(browser, "/examples/list/buttons");
        var campsites = page.Tb().List("Campsites");
        await Expect(page.GetByText("Showing: Pine Hollow")).ToBeVisibleAsync();

        // An item is found by its text, and ClickAsync runs the item's OnClick.
        await campsites.ClickAsync("Eagle Ridge");
        await Expect(page.GetByText("Showing: Eagle Ridge")).ToBeVisibleAsync();

        // The item's own buttons are found by their names, as any button is.
        await page.Tb().Button("Remove Eagle Ridge").ClickAsync();
        Assert.DoesNotContain("Eagle Ridge", await campsites.ItemsAsync());
        await Expect(page.GetByText("Showing: nothing")).ToBeVisibleAsync();
    }
}
