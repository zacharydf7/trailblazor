using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TreeGridTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldLeaveFocusOnTheCellChosen_WhenItTakesFocusBeforeTheOpenedRowsChildrenArrive(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Folders");
        await Toggle(page, "Folders", "Docs").FocusAsync();

        // Focusing Music's size stands in for a click on it while the server is still drawing Docs' children.
        await page.EvaluateAsync("""
            () => {
                const toggle = document.activeElement;
                for (const key of ['ArrowRight', 'ArrowDown']) {
                    toggle.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
                }

                document.querySelector("[aria-label='Folders'] tr[data-key='Music'] td:last-child").focus();
            }
            """);

        await Expect(grid.Row("Notes")).ToHaveCountAsync(1);
        await Expect(await grid.CellAsync("Music", "Size")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldOpenAndCloseRowsAndGoToTheirParent_WhenPressedOnTheFirstCell(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Folders");
        var docs = Toggle(page, "Folders", "Docs");
        await docs.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(grid.Row("Docs")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(docs).ToBeFocusedAsync();
        await Expect(page.GetByLabel("Open rows")).ToHaveTextAsync("Docs");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(await grid.CellAsync("Docs", "Size")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(await grid.CellAsync("Notes", "Name")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(docs).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(grid.Row("Docs")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(grid.Row("Notes")).ToHaveCountAsync(0);
        await Expect(docs).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(docs).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByLabel("Open rows")).ToHaveTextAsync("Docs");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldOpenThenMoveOn_WhenRightIsPressedTwiceQuickly(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Folders");
        await Toggle(page, "Folders", "Photos").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(await grid.CellAsync("Photos", "Size")).ToBeFocusedAsync();
        await Expect(page.GetByLabel("Open rows")).ToHaveTextAsync("Photos");
        await Expect(grid.Row("Trips")).ToHaveAttributeAsync("aria-level", "2");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ExpandAsync_ShouldOpenTheRowOnce_WhenCalledTwice(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Folders");

        await grid.ExpandAsync("Docs");
        await grid.ExpandAsync("Docs");
        await Expect(grid.Row("Notes")).ToHaveAttributeAsync("aria-level", "2");
        await grid.CollapseAsync("Docs");
        await grid.CollapseAsync("Docs");

        await Expect(grid.Row("Notes")).ToHaveCountAsync(0);
        await Expect(page.GetByLabel("Open rows")).ToHaveTextAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldSwapLeftAndRight_WhenTheGridIsRightToLeft(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Folders right to left");
        var docs = Toggle(page, "Folders right to left", "Docs");
        await docs.FocusAsync();

        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(grid.Row("Docs")).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(await grid.CellAsync("Notes", "Name")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(docs).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(grid.Row("Docs")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Left_ShouldGoToTheParentRow_WhenItIsScrolledOutOfAVirtualizedGrid(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Many files");
        var archive = Toggle(page, "Many files", "Archive");
        // Until the rows are measured, the box is too short to scroll to the last row.
        await page.WaitForFunctionAsync("box => box.scrollHeight > box.clientHeight * 10",
            await page.Locator("[data-part=scroll]").Filter(new() { Has = archive }).ElementHandleAsync());
        await archive.FocusAsync();

        await page.Keyboard.PressAsync("Control+End");
        await Expect(await grid.CellAsync("File 200", "Size")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(await grid.CellAsync("File 200", "Name")).ToBeFocusedAsync();
        await Expect(archive).ToHaveCountAsync(0);
        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(archive).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toggle_ShouldOpenTheRowWithoutEditing_WhenTheFirstColumnIsEditable(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Editable folders");
        var docs = Toggle(page, "Editable folders", "Docs");
        var editor = page.GetByRole(AriaRole.Treegrid, new() { Name = "Editable folders" }).Locator("[data-part=editor]");
        await docs.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(grid.Row("Docs")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(grid.Row("Notes")).ToHaveAttributeAsync("aria-level", "2");
        await docs.DblClickAsync();
        // The server answers events in order, so once Photos opens it has handled the double-click. The click is sent
        // without moving focus, which would save and close an edit the double-click had opened.
        await Toggle(page, "Editable folders", "Photos").DispatchEventAsync("click");
        await Expect(grid.Row("Trips")).ToHaveAttributeAsync("aria-level", "2");
        await Expect(grid.Row("Notes")).ToHaveAttributeAsync("aria-level", "2");
        await Expect(editor).ToHaveCountAsync(0);
        await docs.FocusAsync();
        await page.Keyboard.PressAsync("F2");
        await Expect(editor).ToHaveCountAsync(1);
        await page.Keyboard.PressAsync("Escape");
        await Expect(editor).ToHaveCountAsync(0);
        await docs.FocusAsync();
        await page.Keyboard.PressAsync("x");

        await Expect(editor.GetByRole(AriaRole.Textbox)).ToHaveValueAsync("x");
    }

    private static ILocator Toggle(IPage page, string grid, string row) =>
        page.GetByRole(AriaRole.Treegrid, new() { Name = grid, Exact = true })
            .GetByRole(AriaRole.Button, new() { Name = $"Rows under {row}", Exact = true });

    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/tree-grid");
        await Expect(page.Locator("[data-part=grid]")).ToHaveCountAsync(4);
        await Expect(page.Locator("[data-part=grid]:not([data-ready])")).ToHaveCountAsync(0);
        return page;
    }
}
