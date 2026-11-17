namespace Trailblazor.BrowserTests;

public class HighlightTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Highlight_ShouldUseTheSystemHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/highlight", new() { ForcedColors = ForcedColors.Active });
        var match = page.Locator("[data-part=match]").First;

        await Expect(match).ToHaveCSSAsync("background-color", await SystemColorAsync(page, "Mark"));
    }
}
