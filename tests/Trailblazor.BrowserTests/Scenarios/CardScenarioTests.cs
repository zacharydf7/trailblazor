using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CardScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailCards_ShouldKeepWhatIsSaved_WhenTheirSaveButtonsAreToggled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/card/link");

        // Each card's Save button is named for its trail, and toggles.
        await page.Tb().Button("Save Old mill").ClickAsync();
        await page.Tb().Button("Save Lake shore").ClickAsync();
        await page.Tb().Button("Save Old mill").ClickAsync();

        Assert.False(await page.Tb().Button("Save Old mill").PressedAsync());
        Assert.True(await page.Tb().Button("Save Lake shore").PressedAsync());

        // A card is an article named by its title, and a click anywhere on it follows its link.
        await page.GetByRole(AriaRole.Article, new() { Name = "Pine hollow" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex("#pine-hollow$"));
    }
}
