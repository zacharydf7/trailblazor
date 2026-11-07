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

    private static ILocator Link(IPage page, string name) =>
        page.GetByRole(AriaRole.Link, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Table_ShouldScrollAFocusedLinkClearOfTheHeader_WhenItLandsBehindIt(string browser)
    {
        var page = await OpenAsync(browser, "/table-sticky");

        foreach (var (box, name) in new[] { ("plain", "Plain 12"), ("zero", "Zero 12"), ("large", "Large 12") })
        {
            // The link starts just below the box's top, where the header covers it.
            await page.GetByTestId(box).EvaluateAsync("""
                (box, name) => {
                    const link = [...box.querySelectorAll('a')].find(candidate => candidate.textContent === name);
                    box.scrollTop += link.getBoundingClientRect().top - box.getBoundingClientRect().top - 4;
                }
                """, name);
            await Link(page, name).FocusAsync();

            var header = (await page.GetByTestId(box).GetByRole(AriaRole.Columnheader).BoundingBoxAsync())!;
            var link = (await Link(page, name).BoundingBoxAsync())!;
            Assert.True(link.Y >= header.Y + header.Height - 1,
                $"{box}: link at {link.Y}, header ends at {header.Y + header.Height}");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Table_ShouldLeaveAMenusItemsOutOfTheHeadersRoom_WhenARowHoldsAMenu(string browser)
    {
        var page = await OpenAsync(browser, "/table-sticky");
        var margin = (ILocator control) =>
            control.EvaluateAsync<string>("control => getComputedStyle(control).scrollMarginBlockStart");
        var actions = page.GetByRole(AriaRole.Button, new() { Name = "Actions" });
        await actions.ClickAsync();
        var item = page.GetByRole(AriaRole.Menuitem, new() { Name = "Rename" });
        await Expect(item).ToBeVisibleAsync();

        Assert.NotEqual("0px", await margin(actions));
        Assert.Equal("0px", await margin(item));
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
