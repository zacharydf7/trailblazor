using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TimestampTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task BrowserService_ShouldReadTheBrowsersTimeZone_WhenAsked(string browser)
    {
        var page = await OpenAsync(browser, "/browser-time-zone", new() { TimezoneId = "Asia/Tokyo" });

        await page.Tb().Button("Read the zone").ClickAsync();

        await Expect(page.Locator("output")).ToHaveTextAsync("Asia/Tokyo");
    }
}
