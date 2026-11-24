using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class IconButtonScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Bold_ShouldMakeEveryWordBold_WhenPressedWhileOnlySomeAre(string browser)
    {
        var page = await OpenAsync(browser, "/examples/icon-button/mixed");
        // An icon button is a button to the helpers, found by its AriaLabel.
        var bold = page.Tb().Button("Bold");

        // Only one word is bold, so the button shows its mixed state.
        Assert.True(await bold.MixedAsync());

        await bold.ClickAsync();

        Assert.True(await bold.PressedAsync());
        Assert.True(await page.Tb().Checkbox("Pack in bold").CheckedAsync());
        Assert.True(await page.Tb().Checkbox("today in bold").CheckedAsync());
    }
}
