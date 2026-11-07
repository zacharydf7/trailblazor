namespace Trailblazor.BrowserTests;

public class GridSelectTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/grid-select");
        foreach (var grid in await page.Locator("[data-part=grid]").AllAsync())
        {
            await Expect(grid).ToHaveAttributeAsync("data-ready", "");
        }

        return page;
    }

    private static ILocator Grid(IPage page, string name = "Trails") =>
        page.GetByRole(AriaRole.Grid, new() { Name = name });

    private static ILocator Cell(IPage page, string name, string grid = "Trails") =>
        Grid(page, grid).GetByRole(AriaRole.Gridcell, new() { Name = name, Exact = true });

    private static ILocator Box(IPage page, string name) =>
        Grid(page).GetByRole(AriaRole.Row, new() { Name = name }).GetByRole(AriaRole.Checkbox);

    private static ILocator Selected(IPage page) =>
        page.GetByRole(AriaRole.Status, new() { Name = "Selected", Exact = true });

    private static Task<bool> NoTextSelectedAsync(IPage page) =>
        page.EvaluateAsync<bool>("() => document.getSelection().isCollapsed");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldGrowAndShrinkTheSelection_WhenShiftAndTheArrowsMove(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Cell(page, "Birch").FocusAsync();

        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await Expect(Selected(page)).ToHaveTextAsync("Birch, Cedar");
        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await Expect(Selected(page)).ToHaveTextAsync("Birch, Cedar, Dogwood");
        await Expect(Cell(page, "Dogwood")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+ArrowUp");

        await Expect(Selected(page)).ToHaveTextAsync("Birch, Cedar");
        await Expect(Cell(page, "Cedar")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldGrowUpward_WhenShiftAndUpMoveFromARow(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Cell(page, "Cedar").FocusAsync();

        await page.Keyboard.PressAsync("Shift+ArrowUp");

        await Expect(Selected(page)).ToHaveTextAsync("Birch, Cedar");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldGoOnFromTheRowLastChosen_WhenAPlainMoveCameBeforeShiftAndAnArrow(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Cell(page, "Birch").ClickAsync();
        await Expect(Selected(page)).ToHaveTextAsync("Birch");

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Cell(page, "Dogwood")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+ArrowDown");

        await Expect(Selected(page)).ToHaveTextAsync("Birch, Cedar, Dogwood, Elm");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldExtendAcrossAGroupHeading_WhenShiftAndTheArrowsReachOne(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var selected = page.GetByRole(AriaRole.Status, new() { Name = "Places selected" });
        await Cell(page, "Alder", "Places").FocusAsync();

        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await Expect(selected).ToHaveTextAsync("Alder, Bluff");
        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await Expect(Cell(page, "Cove", "Places")).ToBeFocusedAsync();
        await Expect(selected).ToHaveTextAsync("Alder, Bluff, Cove");

        await page.Keyboard.PressAsync("Shift+ArrowUp");
        await page.Keyboard.PressAsync("Shift+ArrowUp");
        await Expect(Cell(page, "Bluff", "Places")).ToBeFocusedAsync();
        await Expect(selected).ToHaveTextAsync("Alder, Bluff");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldKeepTheRowItStartedFrom_WhenTheFirstMoveLandsOnAGroupHeading(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var selected = page.GetByRole(AriaRole.Status, new() { Name = "Places selected" });
        await Cell(page, "Bluff", "Places").FocusAsync();

        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await page.Keyboard.PressAsync("Shift+ArrowDown");

        await Expect(Cell(page, "Cove", "Places")).ToBeFocusedAsync();
        await Expect(selected).ToHaveTextAsync("Bluff, Cove");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldExtendOntoARowNotDrawnYet_WhenTheGridIsVirtualized(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = Grid(page, "Numbers");
        var rows = grid.Locator("tr[data-part=row]");
        await Expect(rows.First).ToBeVisibleAsync();
        var lastNumber = int.Parse((await rows.Last.Locator("td:not([data-part=select])").First.TextContentAsync())!);
        var last = grid.GetByRole(AriaRole.Gridcell, new() { Name = $"{lastNumber}", Exact = true });
        var next = grid.GetByRole(AriaRole.Gridcell, new() { Name = $"{lastNumber + 1}", Exact = true });
        await last.EvaluateAsync("cell => cell.focus({ preventScroll: true })");
        await Expect(next).ToHaveCountAsync(0);

        await page.Keyboard.PressAsync("Shift+ArrowDown");

        await Expect(next).ToBeFocusedAsync();
        await Expect(grid.Locator("[aria-selected=true]")).ToHaveCountAsync(2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldLeaveCtrlAToAField_WhenAShiftClickCameBeforeIt(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var note = Grid(page).GetByRole(AriaRole.Textbox, new() { Name = "Note for Birch", Exact = true });
        await note.ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

        await page.Keyboard.PressAsync("Control+a");

        Assert.Equal(5, await note.EvaluateAsync<int>("note => note.selectionEnd - note.selectionStart"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldSelectEveryRowAndNoText_WhenCtrlAIsPressedInACell(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Cell(page, "Birch").FocusAsync();

        await page.Keyboard.PressAsync("Control+a");

        await Expect(Selected(page)).ToHaveTextAsync("Ash, Birch, Cedar, Dogwood, Elm, Fir");
        Assert.True(await NoTextSelectedAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldLeaveCtrlAToTheBrowser_WhenItsRowsArentAllLoaded(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = Grid(page, "Numbers");
        await grid.GetByRole(AriaRole.Gridcell, new() { Name = "0", Exact = true }).FocusAsync();

        await page.Keyboard.PressAsync("Control+a");

        await Expect(grid.Locator("[aria-selected=true]")).ToHaveCountAsync(0);
        Assert.False(await NoTextSelectedAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldSelectARangeFocusTheRowAndSelectNoText_WhenARowIsShiftClicked(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Cell(page, "Birch").ClickAsync();
        await Expect(Selected(page)).ToHaveTextAsync("Birch");

        await Cell(page, "Elm").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(Selected(page)).ToHaveTextAsync("Birch, Cedar, Dogwood, Elm");
        await Expect(Cell(page, "Elm")).ToBeFocusedAsync();
        Assert.True(await NoTextSelectedAsync(page));

        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await Expect(Selected(page)).ToHaveTextAsync("Birch, Cedar, Dogwood, Elm, Fir");

        await Cell(page, "Cedar").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(Selected(page)).ToHaveTextAsync("Birch, Cedar");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldAddJustTheRow_WhenItIsShiftClickedWithNothingChosenBefore(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await Cell(page, "Elm").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

        await Expect(Selected(page)).ToHaveTextAsync("Elm");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldSelectARangeAndKeepItsBoxesRight_WhenABoxIsShiftClicked(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Box(page, "Birch").ClickAsync();
        await Expect(Selected(page)).ToHaveTextAsync("Birch");

        await Box(page, "Dogwood").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

        await Expect(Selected(page)).ToHaveTextAsync("Birch, Cedar, Dogwood");
        await Expect(Box(page, "Cedar")).ToBeCheckedAsync();
        await Expect(Box(page, "Dogwood")).ToBeCheckedAsync();

        // Shift on a box already in the range keeps it checked, which a plain click would undo.
        await Box(page, "Cedar").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

        await Expect(Selected(page)).ToHaveTextAsync("Birch, Cedar");
        await Expect(Box(page, "Cedar")).ToBeCheckedAsync();
        await Expect(Box(page, "Dogwood")).Not.ToBeCheckedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldToggleEveryRow_WhenTheHeadersBoxIsShiftClicked(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await Grid(page).Locator("thead").GetByRole(AriaRole.Checkbox)
            .ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

        await Expect(Selected(page)).ToHaveTextAsync("Ash, Birch, Cedar, Dogwood, Elm, Fir");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldLeaveAFieldsOwnTextSelection_WhenShiftClickedInARowsField(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var note = Grid(page).GetByRole(AriaRole.Textbox, new() { Name = "Note for Birch", Exact = true });
        await note.ClickAsync();
        await page.Keyboard.PressAsync("Home");

        await note.ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

        await Expect(note).ToBeFocusedAsync();
        Assert.True(await note.EvaluateAsync<bool>("note => note.selectionEnd > note.selectionStart"));
        await Expect(Selected(page)).ToBeEmptyAsync();

        var free = Grid(page).GetByRole(AriaRole.Textbox, new() { Name = "Free note for Birch" });
        await free.ClickAsync();
        await page.Keyboard.PressAsync("Home");
        await free.ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

        Assert.False(await NoTextSelectedAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldLeaveShiftClickToTheBrowser_WhenAClickOnARowDoesntSelectIt(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Locator("[data-part=grid]").Last;
        await grid.GetByRole(AriaRole.Cell, new() { Name = "Birch", Exact = true }).ClickAsync();

        await grid.GetByRole(AriaRole.Cell, new() { Name = "Elm", Exact = true })
            .ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

        Assert.False(await NoTextSelectedAsync(page));
        await Expect(grid.Locator("input:checked")).ToHaveCountAsync(0);
    }
}
