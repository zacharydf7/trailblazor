using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class OverlayScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PageOverlay_ShouldClose_WhenClickedOutsideItsContentOrOnEscape(string browser)
    {
        var page = await OpenAsync(browser, "/examples/overlay/page");
        var overlay = page.Tb().Overlay();

        await page.Tb().Button("Show page overlay").ClickAsync();
        Assert.True(await overlay.IsOpenAsync());

        // ClickAsync clicks the overlay outside its content, which closes an overlay with CloseOnClick, and waits until
        // it has closed.
        await overlay.ClickAsync();
        Assert.False(await overlay.IsOpenAsync());

        // PressEscapeAsync presses Escape, which closes it too, and waits until it has closed.
        await page.Tb().Button("Show page overlay").ClickAsync();
        await overlay.PressEscapeAsync();
        Assert.False(await overlay.IsOpenAsync());
    }
}
