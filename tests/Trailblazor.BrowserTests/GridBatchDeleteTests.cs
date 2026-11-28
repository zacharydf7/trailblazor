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

    // A pressed Delete button already has an outline, so focus has to change the outline's shape.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DeleteButton_ShouldDoubleItsOutline_WhenPressedAndFocusedWithColorsForced(string browser)
    {
        var page = await OpenAsync(browser, "/grid-batch-delete", new() { ForcedColors = ForcedColors.Active });
        var grid = page.Tb().Grid("Hikes");
        await Expect(page.Locator("[data-part=grid]")).ToHaveAttributeAsync("data-ready", "");
        await (await grid.CellAsync("Eagle Ridge", "Name")).FocusAsync();
        await page.Keyboard.PressAsync("Delete");
        var delete = grid.Row("Eagle Ridge").Locator("[data-part=delete]");
        await Expect(delete).ToHaveAttributeAsync("aria-pressed", "true");

        await delete.FocusAsync();

        await Expect(delete).ToHaveCSSAsync("outline-style", "double");
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
