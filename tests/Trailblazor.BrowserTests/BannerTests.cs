using Trailblazor.Testing.Playwright;

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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReadAndCloseTheBanner_WhenItHasOnClose(string browser)
    {
        var page = await OpenAsync(browser, "/examples/banner");

        var text = await page.Tb().Banner("Maintenance tonight").TextAsync();
        await page.Tb().Banner("Maintenance tonight").CloseAsync();

        Assert.Equal("Trail maps are offline from 10 PM to midnight.", text);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Show the banner again" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldCloseTheBannerOnceInteractive_WhenThePageIsStillPrerendered(string browser)
    {
        var (page, held) = await OpenHeldAsync(browser, "/examples/banner");

        var closing = page.Tb().Banner("Maintenance tonight").CloseAsync();
        await page.WaitForTimeoutAsync(300);
        held.SetResult();
        await closing;

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Show the banner again" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldRefuseToClose_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-notices");

        var alert = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Alert("Trail maps").CloseAsync());
        var banner = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Banner("Maintenance tonight").CloseAsync());

        Assert.All(new[] { alert, banner }, error => Assert.Contains("static page", error.Message));
    }
}
