using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class BannerScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Notice_ShouldGoAndComeBack_WhenItIsClosedAndShownAgain(string browser)
    {
        var page = await OpenAsync(browser, "/examples/banner/basic");
        var notice = page.Tb().Banner("Maintenance tonight");
        Assert.Equal("Trail maps are offline from 10 PM to midnight.", await notice.TextAsync());

        // CloseAsync presses the banner's close button, and the app shows a button in its place.
        await notice.CloseAsync();
        await Expect(page.GetByText("Maintenance tonight")).ToHaveCountAsync(0);

        await page.Tb().Button("Show the banner again").ClickAsync();
        Assert.Equal("Trail maps are offline from 10 PM to midnight.", await notice.TextAsync());
    }
}
