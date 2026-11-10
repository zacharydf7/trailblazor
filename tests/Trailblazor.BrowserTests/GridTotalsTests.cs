using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridTotalsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<IPage> OpenTotalsAsync(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail totals" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Totals_ShouldShowEachColumnsTotalOverEveryPage_WhenTheGridIsShown(string browser)
    {
        var page = await OpenTotalsAsync(browser);
        var grid = page.Tb().Grid("Trail totals");

        Assert.Equal("Count 6", await grid.TotalAsync("Name"));
        Assert.Equal("Sum 49.9", await grid.TotalAsync("Length (km)"));
        Assert.Equal("Max 1,250", await grid.TotalAsync("Climb (m)"));
        Assert.Equal("3 of 6 done", await grid.TotalAsync("Done"));
        Assert.Null(await grid.TotalAsync("Region"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GroupTotals_ShouldShowEachGroupsTotalUnderItsRows_WhenTheGridGroups(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.Tb().Grid("Routes by region");

        Assert.Equal(["Sum 20.4", "Sum 20.6", "Sum 10.1"], await grid.GroupTotalsAsync("Length (km)"));
        Assert.Equal([null, null, null], await grid.GroupTotalsAsync("Name"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Totals_ShouldBeReachedByTheArrowKeys_WhenCellNavigationIsOn(string browser)
    {
        var page = await OpenTotalsAsync(browser);
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail totals" });
        await grid.Locator("[tabindex='0']").FocusAsync();

        await page.Keyboard.PressAsync("Control+End");

        await Expect(grid.Locator("tfoot [data-part=totals] td[data-column=Done]")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(grid.Locator("tbody tr[data-part=row]").Last.Locator("td[data-column=Done]")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Totals_ShouldLeaveTheFocusedRowInView_WhenTheRowsScrollUnderThem(string browser)
    {
        var page = await OpenAsync(browser, "/grid-totals");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Scrolling totals" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");
        await grid.Locator("tbody td[data-column=Name]").First.ClickAsync();

        for (var step = 0; step < 6; step++)
        {
            await page.Keyboard.PressAsync("ArrowDown");
        }

        var focused = grid.Locator("tbody td:focus");
        await Expect(focused).ToHaveTextAsync("Leg 7");
        var cellBottom = await focused.EvaluateAsync<double>("cell => cell.getBoundingClientRect().bottom");
        var totalsTop = await grid.Locator("tfoot").EvaluateAsync<double>("foot => foot.getBoundingClientRect().top");
        Assert.True(cellBottom <= totalsTop + 1,
            $"The focused cell ends at {cellBottom}, under the totals at {totalsTop}.");
    }
}
