using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class PropertyGridScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailProperties_ShouldTellTheAppWhatChanged_WhenAPropertyIsEdited(string browser)
    {
        var page = await OpenAsync(browser, "/examples/property-grid/model");
        // PropertyGrid limits the helpers to the grid, whose fields are found by their labels, as anywhere else.
        var trail = page.Tb().PropertyGrid("Trail properties");
        Assert.Equal("Ridge loop", await trail.TextField("Name").TextAsync());

        await trail.Select("Surface").ChooseAsync("Dirt");
        await Expect(page.GetByText("Last changed: Surface")).ToBeVisibleAsync();

        await trail.Switch("Open").UncheckAsync();
        await Expect(page.GetByText("Last changed: Open")).ToBeVisibleAsync();
    }
}
