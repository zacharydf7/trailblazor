namespace Trailblazor.BrowserTests;

public class TableTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Table_ShouldKeepItsHeaderInView_WhenItsRowsScrollWithStickyHeader(string browser)
    {
        var page = await OpenAsync(browser, "/examples/table");
        var log = page.GetByRole(AriaRole.Region, new() { Name = "Trail log" });
        var header = log.GetByRole(AriaRole.Columnheader, new() { Name = "Trail" });

        await log.EvaluateAsync("log => log.scrollTop = log.scrollHeight");

        await Expect(log.GetByRole(AriaRole.Rowheader, new() { Name = "June 20" })).ToBeInViewportAsync();
        var area = (await log.BoundingBoxAsync())!;
        var box = (await header.BoundingBoxAsync())!;
        Assert.Equal(area.Y, box.Y, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Table_ShouldScrollInItsAreaAndLeaveThePageAlone_WhenItIsWiderThanAPhone(string browser)
    {
        var page = await OpenAsync(browser, "/examples/table",
            new() { ViewportSize = new() { Width = 360, Height = 640 } });
        var area = page.GetByRole(AriaRole.Region, new() { Name = "Trail details" });

        var scrolls = await area.EvaluateAsync<bool>("area => area.scrollWidth > area.clientWidth");
        var pageScrolls = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.True(scrolls);
        Assert.False(pageScrolls);
    }
}
