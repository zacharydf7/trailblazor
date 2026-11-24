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
    public async Task Table_ShouldShadeAStripedRowsHeaderCellWithTheRow_WhenStriped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/table");
        var hours = page.GetByRole(AriaRole.Table, new() { Name = "Opening hours" });
        var striped = hours.GetByRole(AriaRole.Rowheader, new() { Name = "River valley" });
        const string Fill = "cell => getComputedStyle(cell.parentElement).backgroundColor";

        // The row is shaded, and its header cell paints nothing over that, as its other cells don't.
        Assert.NotEqual("rgba(0, 0, 0, 0)", await striped.EvaluateAsync<string>(Fill));
        foreach (var cell in new[] { striped, hours.GetByRole(AriaRole.Cell, new() { Name = "10–16" }) })
        {
            await Expect(cell).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
            await Expect(cell).ToHaveCSSAsync("background-image", "none");
        }

        // The stripe is the app's own color when it sets one.
        await hours.EvaluateAsync("table => table.style.setProperty('--tb-table-stripe-color', 'rgb(1, 2, 3)')");
        Assert.Equal("rgb(1, 2, 3)", await striped.EvaluateAsync<string>(Fill));
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
