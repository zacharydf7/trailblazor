namespace Trailblazor.BrowserTests;

public class GridEditKeysTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = Grid(page) }))
            .ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    private static ILocator Grid(IPage page) => page.GetByRole(AriaRole.Grid, new() { Name = "Trail edits" });

    private static ILocator Cell(IPage page, int row, string column) =>
        Grid(page).Locator($"tr[data-key='{row}'] td[data-column={column}]");

    private static ILocator Saved(IPage page) =>
        page.GetByRole(AriaRole.Status, new() { Name = "Last cell saved", Exact = true });

    private static async Task StartEditAsync(ILocator cell)
    {
        await cell.ClickAsync();
        await cell.Page.Keyboard.PressAsync("F2");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Enter_ShouldStartAnEditThatF2LeavesAlone_WhenPressedOnAnEditableCell(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var cell = Cell(page, 3, "Name");
        await cell.ClickAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(cell.Locator("input")).ToBeFocusedAsync();
        await cell.Locator("input").FillAsync("Fir Gap");
        await page.Keyboard.PressAsync("F2");
        // An F2 that started the edit again would have saved the text first, which Escape couldn't take back.
        await page.Keyboard.PressAsync("Escape");

        await Expect(cell).ToBeFocusedAsync();
        await Expect(cell).ToHaveTextAsync("Cedar Flats");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldStartTheEditWithWhatWasTyped_WhenPressedOnATextCell(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var cell = Cell(page, 3, "Name");
        await cell.ClickAsync();

        await page.Keyboard.TypeAsync("Fir Gap");

        await Expect(cell.Locator("input")).ToBeFocusedAsync();
        await Expect(cell.Locator("input")).ToHaveValueAsync("Fir Gap");
        await page.Keyboard.PressAsync("Enter");
        await Expect(cell).ToHaveTextAsync("Fir Gap");
        await Expect(Saved(page)).ToHaveTextAsync("Saved Name of Fir Gap.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldStartTheEditWithWhatWasTyped_WhenPressedOnANumberCell(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var cell = Cell(page, 1, "LengthKm");
        await cell.ClickAsync();

        await page.Keyboard.PressAsync("7");

        await Expect(cell.GetByRole(AriaRole.Spinbutton)).ToBeFocusedAsync();
        await Expect(cell.GetByRole(AriaRole.Spinbutton)).ToHaveValueAsync("7");
        await page.Keyboard.PressAsync("Enter");
        await Expect(cell).ToHaveTextAsync("7.0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldKeepAMinusSign_WhenTheNumberComesAfterTheFieldIsDrawn(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var cell = Cell(page, 1, "LengthKm");
        await cell.ClickAsync();

        await page.Keyboard.PressAsync("-");
        await Expect(cell.GetByRole(AriaRole.Spinbutton)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("5");

        await Expect(cell.GetByRole(AriaRole.Spinbutton)).ToHaveValueAsync("-5");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldJustOpenTheEdit_WhenTheCellsFieldTakesNoText(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var choice = Cell(page, 1, "Difficulty");
        var yesNo = Cell(page, 1, "Open");

        await choice.ClickAsync();
        await page.Keyboard.PressAsync("h");
        await Expect(choice.GetByRole(AriaRole.Combobox)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        // Focus comes back to the cell only after the server hears the render landed, which a slow link delays.
        await Expect(choice).ToBeFocusedAsync();
        await Expect(choice).ToHaveTextAsync("Easy");

        await yesNo.ClickAsync();
        await page.Keyboard.PressAsync("n");
        await Expect(yesNo.GetByRole(AriaRole.Checkbox)).ToBeFocusedAsync();
        await Expect(yesNo.GetByRole(AriaRole.Checkbox)).ToBeCheckedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldLeaveTheCellAlone_WhenAModifierIsHeld(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var cell = Cell(page, 3, "Name");
        await cell.ClickAsync();

        await page.Keyboard.PressAsync("Control+b");
        await page.Keyboard.PressAsync("Alt+b");

        // F2 makes a trip to the server, so an edit the keys had started would have opened by the time this one shows.
        await page.Keyboard.PressAsync("F2");
        await Expect(cell.Locator("input")).ToBeFocusedAsync();
        await Expect(cell.Locator("input")).ToHaveValueAsync("Cedar Flats");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldSelectTheRowAndNotEdit_WhenSpaceIsPressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var cell = Cell(page, 3, "Name");
        // A click could land on the pinned checkbox column if scrolling the cell into view put it underneath.
        await cell.FocusAsync();
        await Expect(cell.Locator("xpath=..")).ToHaveAttributeAsync("aria-selected", "false");

        await page.Keyboard.PressAsync(" ");

        await Expect(cell.Locator("xpath=..")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(cell.Locator("[data-part=editor]")).ToHaveCountAsync(0);
        await Expect(cell).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldSaveAndOpenTheNextCell_WhenPressedInACellsEdit(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var name = Cell(page, 3, "Name");
        var length = Cell(page, 3, "LengthKm");
        await StartEditAsync(name);
        await name.Locator("input").FillAsync("Fir Gap");

        await page.Keyboard.PressAsync("Tab");

        await Expect(length.GetByRole(AriaRole.Spinbutton)).ToBeFocusedAsync();
        await Expect(name).ToHaveTextAsync("Fir Gap");
        await Expect(Saved(page)).ToHaveTextAsync("Saved Name of Fir Gap.");
        await Expect(name.Locator("[data-part=editor]")).ToHaveCountAsync(0);
        await Expect(length.GetByRole(AriaRole.Spinbutton)).ToBeFocusedAsync();

        // The next save starts from the row as the last one left it.
        await length.GetByRole(AriaRole.Spinbutton).FillAsync("8");
        await page.Keyboard.PressAsync("Enter");
        await Expect(length).ToHaveTextAsync("8.0");
        await Expect(name).ToHaveTextAsync("Fir Gap");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldSaveAndOpenThePreviousCell_WhenPressedWithShift(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var name = Cell(page, 3, "Name");
        var length = Cell(page, 3, "LengthKm");
        await StartEditAsync(length);
        await length.GetByRole(AriaRole.Spinbutton).FillAsync("8");

        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(name.Locator("input")).ToBeFocusedAsync();
        await Expect(length).ToHaveTextAsync("8.0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldGoOnToTheNextRow_WhenPressedInARowsLastEditableCell(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await StartEditAsync(Cell(page, 1, "Open"));
        await Expect(Cell(page, 1, "Open").GetByRole(AriaRole.Checkbox)).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(Cell(page, 2, "Name").Locator("input")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Cell(page, 1, "Open").GetByRole(AriaRole.Checkbox)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldGoOnFromAChoice_WhenItsListIsClosed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var choice = Cell(page, 1, "Difficulty");
        await StartEditAsync(choice);
        await Expect(choice.GetByRole(AriaRole.Combobox)).ToHaveAttributeAsync("data-ready", "");

        await page.Keyboard.PressAsync("Tab");

        await Expect(Cell(page, 1, "Open").GetByRole(AriaRole.Checkbox)).ToBeFocusedAsync();
        await Expect(choice.Locator("[data-part=editor]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellEdit_ShouldSaveAChoiceClickedInItsList_WhenTheListIsOpen(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var cell = Cell(page, 2, "Difficulty");

        await cell.DblClickAsync();
        // A click opens the editor's list only after its script attaches, which a slow link delays.
        await Expect(cell.GetByRole(AriaRole.Combobox)).ToHaveAttributeAsync("data-ready", "");
        await cell.GetByRole(AriaRole.Combobox).ClickAsync();
        await cell.GetByRole(AriaRole.Option, new() { Name = "Moderate" }).ClickAsync();

        await Expect(Saved(page)).ToHaveTextAsync("Saved Difficulty of Eagle Ridge traverse.");
        await Expect(cell).ToHaveTextAsync("Moderate");
        await Expect(Grid(page).Locator("tr[data-key='2']")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldSaveWhatWasTyped_WhenTheEditorsFieldCommitsOnChange(string browser)
    {
        var page = await OpenAsync(browser, "/grid-edit-keys");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Notes" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid })).ToHaveAttributeAsync("data-ready", "");
        var text = grid.Locator("tr[data-key='1'] td[data-column=Text]");
        await text.DblClickAsync();
        await text.Locator("textarea").FillAsync("New text");

        await page.Keyboard.PressAsync("Tab");

        await Expect(grid.Locator("tr[data-key='1'] td[data-column=Tag] input")).ToBeFocusedAsync();
        await Expect(text).ToHaveTextAsync("New text");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldKeepTheEditOpenWithWhy_WhenTheValueFailsItsCheck(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var name = Cell(page, 3, "Name");
        await StartEditAsync(name);
        await name.Locator("input").FillAsync("");

        await page.Keyboard.PressAsync("Tab");

        await Expect(name).ToContainTextAsync("Give the trail a name.");
        await Expect(name.Locator("input")).ToBeFocusedAsync();
        await Expect(Cell(page, 3, "LengthKm").GetByRole(AriaRole.Spinbutton)).ToHaveCountAsync(0);
        await Expect(Saved(page)).ToHaveTextAsync("Nothing saved yet.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldSaveAndLeaveTheGrid_WhenPressedInTheLastEditableCell(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var last = Cell(page, 3, "Open");
        await StartEditAsync(last);
        await Expect(last.GetByRole(AriaRole.Checkbox)).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(last.Locator("[data-part=editor]")).ToHaveCountAsync(0);
        Assert.False(await Grid(page).EvaluateAsync<bool>("grid => grid.contains(document.activeElement)"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldSaveAndGoBackToItsCell_WhenShiftIsHeldInTheFirstEditableCell(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var first = Cell(page, 1, "Name");
        await StartEditAsync(first);
        await first.Locator("input").FillAsync("Pine Hollow");

        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(first).ToHaveTextAsync("Pine Hollow");
        await Expect(first).ToBeFocusedAsync();
    }
}
