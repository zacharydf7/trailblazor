namespace Trailblazor.BrowserTests;

public class TimePickerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Safari has a time box, but the WebKit build Playwright uses on Windows shows a text box whose edits never reach the
    // page.
    private const string NoTimeBox = "WebKit on Windows has no time box.";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldSetTheTime_WhenTheBoxIsFilled(string browser)
    {
        Assert.SkipWhen(browser == "webkit", NoTimeBox);
        var page = await OpenAsync(browser, "/examples/time-picker");
        var start = page.GetByLabel("Start time");

        await start.FillAsync("14:45");
        await start.PressAsync("Tab");

        await Expect(page.GetByText($"Starting at {new TimeOnly(14, 45):t}.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldShowAMessage_WhenTheTimeIsOutsideTheLimits(string browser)
    {
        Assert.SkipWhen(browser == "webkit", NoTimeBox);
        var page = await OpenAsync(browser, "/examples/time-picker");
        var pickup = page.GetByLabel("Pickup time");

        await pickup.FillAsync("18:00");
        await pickup.PressAsync("Tab");

        // The app runs in this process, so it shows times in this process's culture.
        await Expect(page.Locator("[data-part=error]"))
            .ToHaveTextAsync($"Choose a time from {new TimeOnly(9, 0):t} to {new TimeOnly(17, 0):t}.");
        await Expect(pickup).ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldPostTheTime_WhenTheFormIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-time-picker");

        await page.GetByLabel("Start time").FillAsync("16:20");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("16:20");
    }
}
