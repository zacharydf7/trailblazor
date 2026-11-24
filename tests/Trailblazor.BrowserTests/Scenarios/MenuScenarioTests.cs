using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class MenuScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ViewMenu_ShouldGiveTheAppItsChecksAndUnits_WhenItemsAreChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu/checks-and-radios");
        var view = page.Tb().Menu("View");
        var state = page.GetByLabel("View state");
        Assert.True(await view.CheckedAsync("Map"));

        // ChooseAsync opens the menu and clicks the item with this name.
        await view.ChooseAsync("Elevation");
        await view.ChooseAsync("Kilometers");

        Assert.True(await view.CheckedAsync("Elevation"));
        Assert.True(await view.CheckedAsync("Kilometers"));
        await Expect(state).ToHaveTextAsync("Kilometers, map, elevation");
    }
}
