using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridCellsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldGrowAndClearTheRange_WhenShiftAndTheArrowsArePressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        await (await grid.CellAsync("Pine Hollow", "Name")).FocusAsync();

        await page.Keyboard.PressAsync("Shift+ArrowRight");
        await page.Keyboard.PressAsync("Shift+ArrowDown");

        await Expect(await grid.CellAsync("Eagle Ridge", "Region")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(await grid.CellAsync("Eagle Ridge", "Region")).ToBeFocusedAsync();
        Assert.Equal([["Pine Hollow", "North ridge"], ["Eagle Ridge", "North ridge"]], await grid.SelectedCellsAsync());
        await Expect(page.Locator("tr[aria-selected=true]")).ToHaveCountAsync(0);
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator("td[aria-selected=true]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Arrow_ShouldClearTheRange_WhenPressedWithoutShift(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        await grid.SelectCellsAsync("Pine Hollow", "Name", "Eagle Ridge", "Region");

        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(page.Locator("td[aria-selected=true]")).ToHaveCountAsync(0);
        await page.Keyboard.PressAsync("Shift+ArrowRight");
        Assert.Equal([["River valley", "3"]], await RangeAsync(page, grid));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CopyAsync_ShouldCopyTheRangeAsTextAndATable_WhenCtrlCIsPressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        await grid.SelectCellsAsync("Eagle Ridge", "Name", "Lake \"Blue\"", "Length (km)");
        await page.EvaluateAsync("""
            () => addEventListener('copy', event => window.copiedHtml = event.clipboardData.getData('text/html'),
                { once: true })
            """);

        var text = await grid.CopyAsync();

        Assert.Equal("Eagle Ridge\tNorth ridge\t14\r\n\"Lake \"\"Blue\"\"\"\tRiver valley\t3", text);
        Assert.Equal("<table><tr><td>Eagle Ridge</td><td>North ridge</td><td>14</td></tr>"
            + "<tr><td>Lake &quot;Blue&quot;</td><td>River valley</td><td>3</td></tr></table>",
            await page.EvaluateAsync<string>("() => window.copiedHtml"));
        await Expect(await grid.CellAsync("Lake \"Blue\"", "Length (km)")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CopyAsync_ShouldCopyTheCellInFocus_WhenNoRangeIsSelected(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        await (await grid.CellAsync("Eagle Ridge", "Region")).FocusAsync();

        Assert.Equal("North ridge", await grid.CopyAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drag_ShouldSelectTheCellsCrossedWithoutClickingTheRow_WhenThePointerMovesAcrossThem(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        var from = await (await grid.CellAsync("Pine Hollow", "Name")).BoundingBoxAsync();
        var to = await (await grid.CellAsync("Pine Hollow", "Length (km)")).BoundingBoxAsync();

        await page.Mouse.MoveAsync(from!.X + 5, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to!.X + to.Width / 2, to.Y + to.Height / 2, new() { Steps = 8 });
        await page.Mouse.UpAsync();

        var length = await grid.CellAsync("Pine Hollow", "Length (km)");
        await Expect(length).ToBeFocusedAsync();
        await Expect(length).ToHaveAttributeAsync("aria-selected", "true");
        Assert.Equal([["Pine Hollow", "North ridge", "6"]], await grid.SelectedCellsAsync());
        Assert.Equal("", await page.EvaluateAsync<string>("() => getSelection().toString()"));
        await Expect(page.GetByRole(AriaRole.Status, new() { Name = "Row clicks", Exact = true })).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ShiftClick_ShouldSelectCellsButNotRows_WhenTheGridSelectsBoth(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var clicks = page.GetByRole(AriaRole.Status, new() { Name = "Row clicks", Exact = true });

        await page.Tb().Grid("Hikes").SelectCellsAsync("Pine Hollow", "Name", "Eagle Ridge", "Region");

        await Expect(clicks).ToHaveTextAsync("1");
        await Expect(page.Locator("tr[aria-selected=true]")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Enter_ShouldClickTheRow_WhenPressedAfterADrag(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        var from = await (await grid.CellAsync("Pine Hollow", "Name")).BoundingBoxAsync();
        var to = await (await grid.CellAsync("Pine Hollow", "Region")).BoundingBoxAsync();
        await page.Mouse.MoveAsync(from!.X + 5, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to!.X + to.Width / 2, to.Y + to.Height / 2, new() { Steps = 8 });
        await page.Mouse.UpAsync();
        await Expect(await grid.CellAsync("Pine Hollow", "Region")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByRole(AriaRole.Status, new() { Name = "Row clicks", Exact = true })).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldStartANewRange_WhenTheFirstRowOfTheOldOneIsGone(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        await grid.SelectCellsAsync("Pine Hollow", "Name", "Eagle Ridge", "Name");
        await page.GetByRole(AriaRole.Button, new() { Name = "Remove the first hike" }).ClickAsync();
        await Expect(page.Locator("td[aria-selected=true]")).ToHaveCountAsync(0);
        await (await grid.CellAsync("Eagle Ridge", "Name")).FocusAsync();

        await page.Keyboard.PressAsync("Shift+ArrowDown");

        await Expect(page.Locator("td[aria-selected=true]")).ToHaveCountAsync(2);
        Assert.Equal([["Eagle Ridge"], ["Lake \"Blue\""]], await grid.SelectedCellsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldStartANewRange_WhenTheFirstColumnOfTheOldOneIsHidden(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        await grid.SelectCellsAsync("Pine Hollow", "Region", "Eagle Ridge", "Length (km)");
        await page.Tb().Menu("Columns").ChooseAsync("Region");
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator("td[aria-selected=true]")).ToHaveCountAsync(0);
        await (await grid.CellAsync("Eagle Ridge", "Name")).FocusAsync();

        await page.Keyboard.PressAsync("Shift+ArrowRight");

        await Expect(page.Locator("td[aria-selected=true]")).ToHaveCountAsync(2);
        Assert.Equal([["Eagle Ridge", "14"]], await grid.SelectedCellsAsync());
    }

    // The selected range, once the server has marked it.
    private static async Task<IReadOnlyList<IReadOnlyList<string>>> RangeAsync(IPage page, TbGridLocator grid)
    {
        await Expect(page.Locator("td[aria-selected=true]").First).ToBeVisibleAsync();
        return await grid.SelectedCellsAsync();
    }

    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/grid-cells");
        await Expect(page.Locator("[data-part=grid]")).ToHaveAttributeAsync("data-ready", "");
        return page;
    }
}
