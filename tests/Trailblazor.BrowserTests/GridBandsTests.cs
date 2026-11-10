using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridBandsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Grid(IPage page) => page.GetByRole(AriaRole.Grid, new() { Name = "Trail effort" });

    private static ILocator Header(IPage page, string title) => Grid(page).Locator($"thead th[data-title='{title}']");

    private static ILocator Band(IPage page, string title) =>
        Grid(page).GetByRole(AriaRole.Columnheader, new() { Name = title, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Bands_ShouldLeaveSortingAndResizingToTheColumns_WhenColumnsAreGrouped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.Tb().Grid("Trail effort");

        await grid.SortAsync("Climb", descending: true);
        await grid.ResizeColumnAsync("Hours", 180);

        await Expect(Header(page, "Climb")).ToHaveAttributeAsync("aria-sort", "descending");
        Assert.Equal("Summit Gate", (await Grid(page).Locator("tbody td[data-column=Name]").AllInnerTextsAsync())[0]);
        var width = await Header(page, "Hours").EvaluateAsync<double>("header => header.getBoundingClientRect().width");
        Assert.Equal(180, width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Bands_ShouldLeaveTheGridWorking_WhenItsColumnsComeAfterItsFirstRender(string browser)
    {
        var page = await OpenAsync(browser, "/grid-bands-late");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Late trails" });
        await Expect(page.Locator("[data-part=grid]")).ToHaveAttributeAsync("data-ready", "");
        var show = page.GetByRole(AriaRole.Button, new() { Name = "Show the columns" });
        await show.ClickAsync();
        await Expect(grid.Locator("thead th[data-title=Name]")).ToBeVisibleAsync();

        await show.FocusAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(grid.Locator("thead th[data-title=Name]")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(grid.GetByRole(AriaRole.Columnheader, new() { Name = "Distance", Exact = true }))
            .ToBeFocusedAsync();
        await page.Tb().Grid("Late trails").ResizeColumnAsync("Length (km)", 200);

        var width = await grid.Locator("thead th[data-title='Length (km)']")
            .EvaluateAsync<double>("header => header.getBoundingClientRect().width");
        Assert.Equal(200, width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Bands_ShouldBeReachedFromTheirColumnsHeaders_WhenTheArrowKeysMoveUp(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = Grid(page) }))
            .ToHaveAttributeAsync("data-ready", "");

        await Header(page, "Climb").FocusAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(Band(page, "Distance")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(Band(page, "Effort")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(Header(page, "Climb")).ToBeFocusedAsync();
    }
}
