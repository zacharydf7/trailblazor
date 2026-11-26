using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridFormEditTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldOpenTheFormWithFocusInTheCellsField_WhenEnterIsPressedOnACell(string browser)
    {
        var page = await OpenAsync(browser, "/grid-form-edit");
        var grid = page.Tb().Grid("Hikes");
        await Expect(page.Locator("[data-part=grid]").First).ToHaveAttributeAsync("data-ready", "");
        await (await grid.CellAsync("Pine Hollow", "Length (km)")).FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Edit Pine Hollow" })).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Length (km)", new() { Exact = true })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldSaveTheNumberTypedAndFocusTheCell_WhenEnterIsPressedInTheForm(string browser)
    {
        var page = await OpenAsync(browser, "/grid-form-edit");
        var grid = page.Tb().Grid("Hikes");
        await Expect(page.Locator("[data-part=grid]").First).ToHaveAttributeAsync("data-ready", "");
        await (await grid.CellAsync("Pine Hollow", "Length (km)")).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByLabel("Length (km)", new() { Exact = true })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("ControlOrMeta+A");
        await page.Keyboard.TypeAsync("9");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        var cell = await grid.CellAsync("Pine Hollow", "Length (km)");
        await Expect(cell).ToHaveTextAsync("9");
        await Expect(cell).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldCancelAndFocusTheCell_WhenEscapeIsPressedInTheForm(string browser)
    {
        var page = await OpenAsync(browser, "/grid-form-edit");
        var grid = page.Tb().Grid("Hikes");
        await Expect(page.Locator("[data-part=grid]").First).ToHaveAttributeAsync("data-ready", "");
        await (await grid.CellAsync("Pine Hollow", "Name")).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByLabel("Name", new() { Exact = true })).ToBeFocusedAsync();

        await page.Keyboard.TypeAsync(" Loop");
        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await Expect(await grid.CellAsync("Pine Hollow", "Name")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldNotSubmitThePagesForm_WhenEnterSavesTheGridsForm(string browser)
    {
        var page = await OpenAsync(browser, "/grid-form-edit");
        var grid = page.Tb().Grid("Trails");
        await Expect(page.Locator("[data-part=grid]").Last).ToHaveAttributeAsync("data-ready", "");
        await (await grid.CellAsync("Fern Gully", "Name")).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByLabel("Name", new() { Exact = true })).ToBeFocusedAsync();

        await page.Keyboard.TypeAsync(" Loop");
        await page.Keyboard.PressAsync("Enter");

        await Expect(await grid.CellAsync("Fern Gully Loop", "Name")).ToBeFocusedAsync();
        await Expect(page.Locator("#submits")).ToHaveTextAsync("Submitted 0 times");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AddRowAsync_ShouldFocusTheSavedRowsCell_WhenTheNewRowsFormIsSaved(string browser)
    {
        var page = await OpenAsync(browser, "/grid-form-edit");
        var grid = page.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Add row" })).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Name", new() { Exact = true })).ToBeFocusedAsync();

        await page.Keyboard.TypeAsync("Fern Gully");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await Expect(await grid.CellAsync("Fern Gully", "Name")).ToBeFocusedAsync();
    }
}
