using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ViewportScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CampgroundPlan_ShouldTakeTheAppsZoomAndFit_WhenItsButtonsArePressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/viewport/zoom-from-code");
        var plan = page.Tb().Viewport("Campground plan");
        Assert.Equal(1, await plan.ZoomAsync());

        // The app's buttons set the bound Zoom and Fit, which the viewport takes.
        await page.Tb().Button("Show at 200%").ClickAsync();
        Assert.Equal(2, await plan.ZoomAsync());

        await page.Tb().Button("Fit the width").ClickAsync();
        Assert.Equal(ViewportFit.Width, await plan.CurrentFitAsync());
    }
}
