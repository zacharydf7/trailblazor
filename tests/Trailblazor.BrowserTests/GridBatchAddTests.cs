using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridBatchAddTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldKeepFocusOnTheAddedRowsCell_WhenEnterSavesItsEdit(string browser)
    {
        var page = await OpenAsync(browser, "/grid-batch-add");
        var grid = page.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await Expect(grid.NewRow.Locator("td[data-column=Name] input")).ToBeFocusedAsync();

        await page.Keyboard.TypeAsync("Fern Gully");
        await page.Keyboard.PressAsync("Enter");

        await Expect(grid.NewRow.Locator("td[data-column=Name]")).ToHaveTextAsync("Fern Gully");
        await Expect(grid.NewRow.Locator("td[data-column=Name]")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldRemoveTheAddedRowAndFocusTheRowInItsPlace_WhenDeleteIsPressedOnItsCell(string browser)
    {
        var page = await OpenAsync(browser, "/grid-batch-add");
        var grid = page.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await page.Keyboard.TypeAsync("Fern Gully");
        await page.Keyboard.PressAsync("Enter");
        await Expect(grid.NewRow.Locator("td[data-column=Name]")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Delete");

        await Expect(page.Locator("tr[data-new]")).ToHaveCountAsync(0);
        await Expect(page.Locator("[data-part=change-count]")).ToHaveTextAsync("Unsaved changes: 0");
        await Expect(await grid.CellAsync("Pine Hollow", "Name")).ToBeFocusedAsync();
    }
}
