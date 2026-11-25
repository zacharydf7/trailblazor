namespace Trailblazor.BrowserTests;

public class QrCodeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // The marks' color, then the background's, for the first QR code and the first barcode.
    private const string Fills = """
        () => {
            const style = selector => getComputedStyle(document.querySelector(selector));
            return [style('.tb-qr-code__modules').fill, style('.tb-qr-code__background').fill,
                style('.tb-barcode__marks').fill, style('.tb-barcode').backgroundColor];
        }
        """;

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Codes_ShouldStayDarkOnLight_WhenTheThemeIsDark(string browser)
    {
        var page = await OpenAsync(browser, "/examples/qr-code", new() { ColorScheme = ColorScheme.Dark });

        var fills = await page.EvaluateAsync<string[]>(Fills);

        Assert.Equal(["rgb(0, 0, 0)", "rgb(255, 255, 255)", "rgb(0, 0, 0)", "rgb(255, 255, 255)"], fills);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Codes_ShouldStayDarkOnLight_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/qr-code",
            new() { ColorScheme = ColorScheme.Dark, ForcedColors = ForcedColors.Active });

        var fills = await page.EvaluateAsync<string[]>(Fills);

        Assert.Equal(["rgb(0, 0, 0)", "rgb(255, 255, 255)", "rgb(0, 0, 0)", "rgb(255, 255, 255)"], fills);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task QrCode_ShouldBeASquareOfFourPixelsAModule_WhenItHasNoWidth(string browser)
    {
        var page = await OpenAsync(browser, "/examples/qr-code");

        var box = await page.GetByRole(AriaRole.Img, new() { Name = "Link to the trail map", Exact = true })
            .BoundingBoxAsync();

        Assert.Equal((132f, 132f), (box!.Width, box.Height));
    }
}
