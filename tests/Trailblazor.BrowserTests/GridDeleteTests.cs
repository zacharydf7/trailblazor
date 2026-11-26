using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridDeleteTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldDeleteTheRowAndFocusTheOneBefore_WhenDeleteIsPressedOnTheLastRow(string browser)
    {
        var page = await OpenAsync(browser, "/grid-delete");
        var grid = page.Tb().Grid("Hikes");
        await Expect(page.Locator("[data-part=grid]").First).ToHaveAttributeAsync("data-ready", "");
        await (await grid.CellAsync("Eagle Ridge", "Name")).FocusAsync();

        await page.Keyboard.PressAsync("Delete");

        await Expect(grid.Row("Eagle Ridge")).ToHaveCountAsync(0);
        await Expect(await grid.CellAsync("Held", "Name")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldDeleteOnce_WhenDeleteIsHeld(string browser)
    {
        var page = await OpenAsync(browser, "/grid-delete");
        var grid = page.Tb().Grid("Hikes");
        await Expect(page.Locator("[data-part=grid]").First).ToHaveAttributeAsync("data-ready", "");
        var cell = await grid.CellAsync("Pine Hollow", "Name");
        await cell.FocusAsync();

        await cell.DispatchEventAsync("keydown", new { key = "Delete", repeat = true, bubbles = true });
        // Events reach the server in order, so once a later delete has landed, the repeat's would have too.
        await (await grid.CellAsync("Eagle Ridge", "Name")).FocusAsync();
        await page.Keyboard.PressAsync("Delete");
        await Expect(grid.Row("Eagle Ridge")).ToHaveCountAsync(0);

        await Expect(grid.Row("Pine Hollow")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DeleteRowAsync_ShouldFocusTheNextRowsDeleteButton_WhenTheTableIsPlain(string browser)
    {
        var page = await OpenAsync(browser, "/grid-delete");
        var grid = page.Tb().Grid("Trails");

        await grid.DeleteRowAsync("Fern Gully");

        await Expect(grid.Row("Fern Gully")).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Delete Lake Shore" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldKeepTheRowAndShowWhy_WhenTheAppRefusesTheDelete(string browser)
    {
        var page = await OpenAsync(browser, "/grid-delete");
        var grid = page.Tb().Grid("Hikes");
        await Expect(page.Locator("[data-part=grid]").First).ToHaveAttributeAsync("data-ready", "");
        await (await grid.CellAsync("Held", "Name")).FocusAsync();

        await page.Keyboard.PressAsync("Delete");

        await Expect(page.Locator("[data-part=delete-error]")).ToHaveTextAsync("Held has trips booked.");
        await Expect(grid.Row("Held").GetByRole(AriaRole.Button, new() { Name = "Delete Held" }))
            .ToHaveAccessibleDescriptionAsync("Held has trips booked.");
        await Expect(await grid.CellAsync("Held", "Name")).ToBeFocusedAsync();
    }
}
