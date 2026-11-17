namespace Trailblazor.BrowserTests;

public class WatermarkTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Watermark_ShouldCoverTheContentWithTiledMarks_WhenGivenText(string browser)
    {
        var page = await OpenAsync(browser, "/examples/watermark");
        var marks = page.Locator(".tb-watermark [data-part=marks]");

        var covered = (await page.Locator(".tb-watermark").BoundingBoxAsync())!;
        var box = (await marks.BoundingBoxAsync())!;
        Assert.Equal((covered.X, covered.Y, covered.Width, covered.Height), (box.X, box.Y, box.Width, box.Height));
        Assert.StartsWith("url(", await marks.EvaluateAsync<string>(
            "marks => getComputedStyle(marks).maskImage || getComputedStyle(marks).webkitMaskImage"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Watermark_ShouldLetClicksAndFocusThrough_WhenOverContent(string browser)
    {
        var page = await OpenAsync(browser, "/examples/watermark");
        var approve = page.GetByRole(AriaRole.Button, new() { Name = "Approve report" });

        await approve.ClickAsync();
        await Expect(page.GetByText("Approved", new() { Exact = true })).ToBeVisibleAsync();

        // From the text before the button, Tab reaches it and then passes over the marks.
        await page.GetByText("The lower bridge is out").ClickAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(approve).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.Locator(".tb-watermark [data-part=marks]")).Not.ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Watermark_ShouldUseTheSystemGray_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/watermark", new() { ForcedColors = ForcedColors.Active });
        await Expect(page.Locator(".tb-watermark [data-part=marks]"))
            .ToHaveCSSAsync("background-color", await SystemColorAsync(page, "GrayText"));
    }
}
