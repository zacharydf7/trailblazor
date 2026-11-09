namespace Trailblazor.BrowserTests;

public class AvatarTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Avatar_ShouldShowItsPictureOrItsInitials_WhenThePictureLoadsOrFails(string browser)
    {
        var page = await OpenAsync(browser, "/examples/avatar");
        var turing = page.GetByRole(AriaRole.Img, new() { Name = "Alan Turing" });
        var ada = page.GetByRole(AriaRole.Img, new() { Name = "Ada Lovelace" });

        await Expect(turing.Locator("[data-part=image]")).ToHaveCountAsync(0);
        await Expect(turing.Locator("[data-part=initials]")).ToHaveTextAsync("AT");
        await Expect(ada.Locator("[data-part=initials]")).ToHaveCountAsync(0);
        await Expect(ada.Locator("[data-part=image]")).ToHaveCountAsync(1);
    }

    // Before the page is interactive nothing handles the error, so the picture itself must draw nothing when it fails:
    // the avatar has to look just like one with no picture.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Avatar_ShouldLookAsIfItHadNoPicture_WhenItsPictureFailsBeforeThePageIsInteractive(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-avatar");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Equal(await page.GetByTestId("plain").ScreenshotAsync(), await page.GetByTestId("failed").ScreenshotAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Avatar_ShouldDrawItsPicture_WhenItsAddressIsRelativeToThePage(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-avatar");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.NotEqual(await page.GetByTestId("plain").ScreenshotAsync(),
            await page.GetByTestId("relative").ScreenshotAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Avatar_ShouldKeepAnOutline_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/avatar", new() { ForcedColors = ForcedColors.Active });

        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "Grace Hopper" }))
            .ToHaveCSSAsync("border-top-style", "solid");
    }
}
