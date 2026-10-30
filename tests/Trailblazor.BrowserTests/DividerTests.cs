namespace Trailblazor.BrowserTests;

public class DividerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // How far the content's middle sits from the line's start, as a share of the line.
    private static async Task<double> PlaceAsync(IPage page, string testId, bool vertical = false)
    {
        var line = (await page.GetByTestId(testId).BoundingBoxAsync())!;
        var content = (await page.GetByTestId(testId).Locator("[data-part=content]").BoundingBoxAsync())!;
        return vertical ? (content.Y + content.Height / 2 - line.Y) / line.Height
            : (content.X + content.Width / 2 - line.X) / line.Width;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Divider_ShouldPlaceItsContentAlongTheLine_WhenAligned(string browser)
    {
        var page = await OpenAsync(browser, "/divider");

        Assert.InRange(await PlaceAsync(page, "center"), 0.45, 0.55);
        Assert.InRange(await PlaceAsync(page, "start"), 0, 0.2);
        Assert.InRange(await PlaceAsync(page, "end"), 0.8, 1);
        Assert.InRange(await PlaceAsync(page, "vertical", vertical: true), 0.4, 0.6);
    }

    // Each line's length along the divider and its drawn thickness, before and after the content.
    private static Task<double[][]> LinesAsync(IPage page, string testId, bool vertical) =>
        page.GetByTestId(testId).EvaluateAsync<double[][]>("""
            (divider, vertical) => ['::before', '::after'].map(side => {
                const style = getComputedStyle(divider, side);
                return vertical ? [parseFloat(style.height), parseFloat(style.borderLeftWidth)]
                    : [parseFloat(style.width), parseFloat(style.borderTopWidth)];
            })
            """, vertical);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Divider_ShouldDrawLinesOnBothSidesOfItsContent_WhenGivenContent(string browser)
    {
        var page = await OpenAsync(browser, "/divider");

        Assert.All(await LinesAsync(page, "center", vertical: false), line => Assert.True(line[0] > 100 && line[1] >= 1,
            $"A line is {line[0]}px long and {line[1]}px thick."));
        Assert.All(await LinesAsync(page, "vertical", vertical: true), line => Assert.True(line[0] > 40 && line[1] >= 1,
            $"A line is {line[0]}px long and {line[1]}px thick."));
    }
}
