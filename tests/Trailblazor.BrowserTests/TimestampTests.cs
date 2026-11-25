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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Relative_ShouldShowTheExactTime_WhenTapped(string browser)
    {
        var page = await OpenAsync(browser, "/timestamp-relative", new() { HasTouch = true });
        var exact = (await page.GetByRole(AriaRole.Tooltip, new() { IncludeHidden = true }).First.TextContentAsync())!
            .Trim();

        await page.Locator("time").First.TapAsync();

        await Expect(page.GetByRole(AriaRole.Tooltip)).ToHaveTextAsync(exact);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Relative_ShouldLeaveTheTapToTheButton_WhenInsideOne(string browser)
    {
        var page = await OpenAsync(browser, "/timestamp-relative", new() { HasTouch = true });

        await page.GetByRole(AriaRole.Button).Locator("time").TapAsync();

        await Expect(page.Locator("output")).ToHaveTextAsync("1");
        await Expect(page.GetByRole(AriaRole.Tooltip)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Relative_ShouldBeDescribedByTheExactTime_WhenFocused(string browser)
    {
        var page = await OpenAsync(browser, "/timestamp-relative");
        var time = page.Locator("time").First;
        var exact = (await page.GetByRole(AriaRole.Tooltip, new() { IncludeHidden = true }).First.TextContentAsync())!
            .Trim();

        await page.Keyboard.PressAsync("Tab");

        await Expect(time).ToBeFocusedAsync();
        await Expect(time).ToHaveTextAsync("in 3 days");
        await Expect(time).ToHaveAccessibleDescriptionAsync(exact);
        await Expect(page.GetByRole(AriaRole.Tooltip)).ToHaveTextAsync(exact);
    }
}
