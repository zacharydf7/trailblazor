using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ContextMenuScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailMenu_ShouldRemoveATrailAndAddOne_WhenItemsAreChosenForARowAndForTheArea(string browser)
    {
        var page = await OpenAsync(browser, "/examples/context-menu/keys");
        var actions = page.Tb().ContextMenu("Trail actions");
        var last = page.Locator("output");

        // On names what was right-clicked, by its data-context-key, and ChooseAsync chooses an item of the menu that
        // opens for it.
        await actions.On("Lake path").ChooseAsync("Remove");
        await Expect(last).ToHaveTextAsync("Removed Lake path");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Lake path", Exact = true })).ToHaveCountAsync(0);

        // Without On, the menu is the one for the rest of the area.
        await actions.ChooseAsync("Add a trail");
        await Expect(last).ToHaveTextAsync("Added New trail 1");
    }
}
