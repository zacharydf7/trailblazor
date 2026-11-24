using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class BannerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Banner_ShouldWrapItsMessageBesideTheIcon_WhenThePartsDontFitOneRow(string browser)
    {
        var page = await OpenAsync(browser, "/examples/banner");
        var banner = page.GetByRole(AriaRole.Region, new() { Name = "Maintenance tonight" });
        var (icon, message) = (banner.Locator("[data-part=icon]").First, banner.Locator("[data-part=message]"));
        var (actions, close) = (banner.Locator("[data-part=actions]"), banner.Locator("[data-part=close]"));

        // A message too long for the window wraps between the icon and the actions, which stay in its row.
        await message.EvaluateAsync("message => message.append(' Trail maps come back at midnight.'.repeat(8))");
        var (mark, text) = ((await icon.BoundingBoxAsync())!, (await message.BoundingBoxAsync())!);
        var (links, dismiss) = ((await actions.BoundingBoxAsync())!, (await close.BoundingBoxAsync())!);
        Assert.True(text.Height > 2 * mark.Height, $"The message is {text.Height}px tall, so it didn't wrap.");
        Assert.True(mark.X + mark.Width <= text.X && text.X + text.Width <= links.X
            && links.X + links.Width <= dismiss.X, "The parts aren't in a row.");
        Assert.All(new[] { mark, links, dismiss }, part => Assert.InRange(part.Y + part.Height / 2, text.Y,
            text.Y + text.Height));

        // In a narrow banner the icon and the close button keep to the first line, and the actions go under the
        // message, starting where it does.
        await page.SetViewportSizeAsync(390, 800);
        (mark, text) = ((await icon.BoundingBoxAsync())!, (await message.BoundingBoxAsync())!);
        (links, dismiss) = ((await actions.BoundingBoxAsync())!, (await close.BoundingBoxAsync())!);
        Assert.True(mark.X + mark.Width <= text.X, "The icon isn't before the message.");
        Assert.Equal(text.Y, mark.Y, 2.0);
        Assert.Equal(text.X, links.X, 0.5);
        Assert.True(links.Y >= text.Y + text.Height, "The actions aren't under the message.");
        Assert.True(dismiss.X >= text.X + text.Width, "The close button isn't after the message.");
        Assert.Equal(text.Y, dismiss.Y, 2.0);

        // A banner with no icon or actions has no gap of theirs beside its message, which stays centered.
        var plain = page.GetByRole(AriaRole.Region, new() { Name = "Connection" });
        var (strip, words) = ((await plain.BoundingBoxAsync())!,
            (await plain.Locator("[data-part=message]").BoundingBoxAsync())!);
        Assert.Equal(words.X - strip.X, strip.X + strip.Width - words.X - words.Width, 1.0);
    }

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
