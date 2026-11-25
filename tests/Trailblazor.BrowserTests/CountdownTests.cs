namespace Trailblazor.BrowserTests;

public class CountdownTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TbCountdown_ShouldPause_WhileThePointerOrFocusIsInside(string browser)
    {
        var page = await OpenAsync(browser, "/countdown");
        var countdown = page.Locator(".tb-countdown");
        await Expect(countdown).ToHaveAttributeAsync("data-state", "running");

        await countdown.Locator("[data-part=text]").HoverAsync();
        await Expect(countdown).ToHaveAttributeAsync("data-state", "paused");
        await page.GetByRole(AriaRole.Button, new() { Name = "Outside" }).HoverAsync();
        await Expect(countdown).ToHaveAttributeAsync("data-state", "running");

        await page.GetByRole(AriaRole.Button, new() { Name = "Stay on this page" }).FocusAsync();
        await Expect(countdown).ToHaveAttributeAsync("data-state", "paused");
        await page.GetByRole(AriaRole.Button, new() { Name = "Outside" }).FocusAsync();

        await Expect(countdown).ToHaveAttributeAsync("data-state", "running");
    }
}
