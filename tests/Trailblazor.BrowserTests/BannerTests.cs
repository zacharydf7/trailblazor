namespace Trailblazor.BrowserTests;

public class BannerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Banner_ShouldClose_WhenItsCloseButtonIsActivatedByKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/examples/banner");
        var banner = page.GetByRole(AriaRole.Region, new() { Name = "Maintenance tonight" });
        await banner.GetByRole(AriaRole.Button, new() { Name = "Dismiss" }).FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(banner).ToBeHiddenAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Show the banner again" })).ToBeFocusedAsync();
    }
}
