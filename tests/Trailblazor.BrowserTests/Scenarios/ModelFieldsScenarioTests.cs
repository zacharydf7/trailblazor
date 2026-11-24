using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ModelFieldsScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NewHike_ShouldSaveOnlyOnceItsTrailIsNamed_WhenSubmitted(string browser)
    {
        var page = await OpenAsync(browser, "/examples/model-fields/basic");
        // Each field the model makes is found by its label with the helper for its kind.
        var name = page.Tb().TextField("Trail name");

        // ClickAsync on the submit button waits until the form has handled the submit.
        await page.Tb().Button("Save hike").ClickAsync();
        Assert.Equal("Trail name is required.", await name.ErrorAsync());

        await name.EnterAsync("Ridge loop");
        await page.Tb().NumberField("Length (km)").EnterAsync("12");
        await page.Tb().Button("Save hike").ClickAsync();

        Assert.Null(await name.ErrorAsync());
        await Expect(page.Locator("output")).ToHaveTextAsync("Saved Ridge loop.");
    }
}
