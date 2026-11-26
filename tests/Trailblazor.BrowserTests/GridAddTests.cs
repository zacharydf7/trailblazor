using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridAddTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AddRowAsync_ShouldFocusTheNewRowsFirstField_WhenCalled(string browser)
    {
        var page = await OpenAsync(browser, "/grid-add");
        var grid = page.Tb().Grid("Hikes");

        await grid.AddRowAsync();

        await Expect(grid.NewRow.Locator("td[data-column=Name] input")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldAddTheRowAndFocusItsCell_WhenEnterIsPressedInTheNewRow(string browser)
    {
        var page = await OpenAsync(browser, "/grid-add");
        var grid = page.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await Expect(grid.NewRow.Locator("td[data-column=Name] input")).ToBeFocusedAsync();

        await page.Keyboard.TypeAsync("Fern Gully");
        await page.Keyboard.PressAsync("Enter");

        await Expect(grid.NewRow).ToHaveCountAsync(0);
        await Expect(await grid.CellAsync("Fern Gully", "Name")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldDiscardTheNewRowAndFocusAddRow_WhenEscapeIsPressedInIt(string browser)
    {
        var page = await OpenAsync(browser, "/grid-add");
        var grid = page.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await Expect(grid.NewRow.Locator("td[data-column=Name] input")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(grid.NewRow).ToHaveCountAsync(0);
        await Expect(page.Locator("[data-part=add-row]")).ToBeFocusedAsync();
        Assert.Equal(1, await grid.RowCountAsync());
    }
}
