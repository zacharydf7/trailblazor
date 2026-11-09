using System.Globalization;

namespace Trailblazor.BrowserTests;

public class TimelineTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator History(IPage page) => page.GetByRole(AriaRole.List, new() { Name = "Trip history" });

    // For each event, whether its marker draws a line, and the gap from the line's end to the next event's dot.
    private const string LinesAndGaps = @"items => items.map(item => {
        const marker = item.querySelector('.tb-timeline__marker');
        const next = item.nextElementSibling?.querySelector('[data-part=dot]');
        const gap = next ? next.getBoundingClientRect().top - marker.getBoundingClientRect().bottom : 0;
        return [getComputedStyle(marker, '::after').content, String(gap)];
    })";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Timeline_ShouldJoinEachDotToTheNextAndEndAtTheLast_WhenRendered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/timeline");

        var items = await History(page).GetByRole(AriaRole.Listitem).EvaluateAllAsync<string[][]>(LinesAndGaps);

        Assert.Equal(["\"\"", "\"\"", "none"], items.Select(item => item[0]));
        Assert.All(items, item => Assert.InRange(double.Parse(item[1], CultureInfo.InvariantCulture), 0, 8));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Timeline_ShouldKeepItsDotsAndLine_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/timeline", new() { ForcedColors = ForcedColors.Active });
        var text = await SystemColorAsync(page, "CanvasText");
        var first = History(page).GetByRole(AriaRole.Listitem).First;

        await Expect(first.Locator("[data-part=dot]")).ToHaveCSSAsync("background-color", text);
        Assert.Equal(text, await first.Locator(".tb-timeline__marker")
            .EvaluateAsync<string>("marker => getComputedStyle(marker, '::after').backgroundColor"));
    }
}
