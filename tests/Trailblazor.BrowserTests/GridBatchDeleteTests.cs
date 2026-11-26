using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridBatchDeleteTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldMarkTheRowAndKeepFocus_WhenDeleteIsPressedOnACell(string browser)
    {
        var page = await OpenAsync(browser, "/grid-batch-delete");
        var grid = page.Tb().Grid("Hikes");
        await Expect(page.Locator("[data-part=grid]")).ToHaveAttributeAsync("data-ready", "");
        var cell = await grid.CellAsync("Eagle Ridge", "Name");
        await cell.FocusAsync();

        await page.Keyboard.PressAsync("Delete");

        await Expect(grid.Row("Eagle Ridge")).ToHaveAttributeAsync("data-deleted", "");
        await Expect(cell).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Delete");
        await Expect(grid.Row("Eagle Ridge")).Not.ToHaveAttributeAsync("data-deleted", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SaveChangesAsync_ShouldRemoveTheRow_WhenTheRowIsMarkedDeleted(string browser)
    {
        var page = await OpenAsync(browser, "/grid-batch-delete");
        var grid = page.Tb().Grid("Hikes");
        await grid.DeleteRowAsync("Eagle Ridge");

        await grid.SaveChangesAsync();

        await Expect(grid.Row("Eagle Ridge")).ToHaveCountAsync(0);
        Assert.Equal(1, await grid.RowCountAsync());
    }
}
