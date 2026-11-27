namespace Trailblazor.BrowserTests;

public class SparklineTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Hikers = "Hikers a day this week, rising to the weekend";

    // Where the two ends of a sparkline's line are on the page, the first value's end first.
    private const string Ends = """
        line => {
            const [first, last] = [line.getPointAtLength(0), line.getPointAtLength(line.getTotalLength())]
                .map(point => point.matrixTransform(line.getScreenCTM()));
            return [first.x, first.y, last.x, last.y];
        }
        """;

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sparkline_ShouldBeALineOfTextHighInTheFlowOfItsText_WhenDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/sparkline/basic");
        var picture = page.GetByRole(AriaRole.Img, new() { Name = Hikers });
        var box = await picture.BoundingBoxAsync();
        var text = await page.GetByText("Hikers this week:").BoundingBoxAsync();
        var size = await picture.EvaluateAsync<float>("picture => parseFloat(getComputedStyle(picture).fontSize)");
        var ends = await picture.Locator("path").EvaluateAsync<float[]>(Ends);

        // 6rem wide and one and a half times its text's size high, on the same line as the text before it.
        Assert.InRange(box!.Width, 95.5, 96.5);
        Assert.InRange(box.Height - size * 1.5f, -0.5, 0.5);
        Assert.True(box.Y < text!.Y + text.Height && box.Y + box.Height > text.Y, "The picture is on another line.");
        // The line runs from the left edge to the right one, and ends at the top, where the highest value is.
        Assert.InRange(ends[0] - box.X, -1, 1);
        Assert.InRange(ends[2] - (box.X + box.Width), -1, 1);
        Assert.InRange(ends[3] - box.Y, -1, 1);
        await Expect(picture.Locator("path")).ToHaveCSSAsync("stroke",
            await SystemColorAsync(page, "var(--tb-chart-series-1)"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sparkline_ShouldRunFromTheRight_WhenThePageRunsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/examples/sparkline/basic");
        await page.EvaluateAsync("() => { document.documentElement.dir = 'rtl'; }");
        var picture = page.GetByRole(AriaRole.Img, new() { Name = Hikers });
        var box = await picture.BoundingBoxAsync();

        var ends = await picture.Locator("path").EvaluateAsync<float[]>(Ends);

        // The first value is at the right edge and the last, the highest, at the left one.
        Assert.InRange(ends[0] - (box!.X + box.Width), -1, 1);
        Assert.InRange(ends[2] - box.X, -1, 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sparkline_ShouldTakeItsToneColorAndSize_WhenTheyAreGiven(string browser)
    {
        var page = await OpenAsync(browser, "/examples/sparkline/tones");
        var rescues = page.GetByRole(AriaRole.Img, new() { Name = "Rescues a month this year, rising since May" });
        var booked = page.GetByRole(AriaRole.Img, new() { Name = "Huts booked a month this year, highest in July" });

        await Expect(rescues.Locator("path")).ToHaveCSSAsync("stroke",
            await SystemColorAsync(page, "var(--tb-color-danger-text)"));
        await Expect(booked.Locator("path")).ToHaveCSSAsync("stroke",
            await SystemColorAsync(page, "var(--tb-chart-series-5)"));
        var box = await booked.BoundingBoxAsync();
        Assert.InRange(box!.Width, 159.5, 160.5);
        Assert.InRange(box.Height, 31.5, 32.5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Bars_ShouldStandOnTheZeroLineOrHangFromIt_WhenValuesAreOfBothSigns(string browser)
    {
        var page = await OpenAsync(browser, "/examples/sparkline/kinds");
        var picture = page.GetByRole(AriaRole.Img, new() { Name = "Change in snow depth each day, down from "
            + "Wednesday to Friday" });
        var bars = picture.Locator("rect");
        await Expect(bars).ToHaveCountAsync(7);
        var (up, down) = (await bars.Nth(0).BoundingBoxAsync(), await bars.Nth(4).BoundingBoxAsync());

        // Monday's +12 ends where Friday's -14 starts, at zero, and the two together are as high as the picture.
        var box = await picture.BoundingBoxAsync();
        Assert.InRange(up!.Y + up.Height - down!.Y, -0.6, 0.6);
        Assert.InRange(up.Height + down.Height - box!.Height, -0.6, 0.6);
        Assert.True(up.X < down.X && up.Width > 5, $"A bar is {up.Width} wide.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sparkline_ShouldBeDrawnInTheTextColor_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/sparkline/kinds", new() { ForcedColors = ForcedColors.Active });
        var text = await SystemColorAsync(page, "CanvasText");
        var area = page.GetByRole(AriaRole.Img, new() { Name = "Snow depth this week, lowest on Friday" });
        var bars = page.GetByRole(AriaRole.Img, new() { Name = "Change in snow depth each day, down from "
            + "Wednesday to Friday" });

        // An area keeps its line and loses its fill.
        await Expect(area.Locator(".tb-sparkline__line")).ToHaveCSSAsync("stroke", text);
        await Expect(area.Locator(".tb-sparkline__area")).ToHaveCSSAsync("fill", "none");
        await Expect(bars.Locator("rect").First).ToHaveCSSAsync("fill", text);
    }
}
