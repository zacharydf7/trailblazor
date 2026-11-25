using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TextScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailNames_ShouldShowTheWholeOfTheOneCutOff_WhenItIsTooLong(string browser)
    {
        const string name = "Pine Hollow loop climbs through old cedar groves to a lookout over the river valley.";
        var page = await OpenAsync(browser, "/examples/text/full-text");
        var pine = page.Tb().Text(name);

        // IsCutAsync waits for the text's script to measure its box.
        Assert.True(await pine.IsCutAsync());
        await pine.Element.HoverAsync();

        await Expect(pine.Element.Locator("[role=tooltip]")).ToHaveTextAsync(name);
    }
}
