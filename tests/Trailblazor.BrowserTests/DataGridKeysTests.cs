using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Keyboard tests and shared helpers for DataGridTests; the slow-link class reruns only these.
public abstract class DataGridKeysTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected async Task<IPage> OpenRowsAsync(string browser)
    {
        var page = await OpenAsync(browser, "/grid-rows");
        await Expect(page.Locator("[data-part=grid]").First).ToHaveAttributeAsync("data-ready", "");
        await Expect(page.Locator("[data-part=grid]").Nth(1)).ToHaveAttributeAsync("data-ready", "");
        await Expect(page.Locator("[data-part=grid]").Nth(2)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    // The cell is wider than its text, so this point sits near the start, on the text.
    protected static readonly Position OnTheText = new() { X = 20, Y = 12 };

    protected static ILocator RowCell(IPage page, string name) =>
        page.GetByRole(AriaRole.Gridcell, new() { Name = name, Exact = true });

    private static ILocator DeleteButton(IPage page, string trail) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Delete {trail}", Exact = true });

    protected static ILocator Table(IPage page, string name) =>
        page.GetByRole(AriaRole.Table, new() { Name = name, Exact = true });

    protected static ILocator Names(IPage page, string grid = "Trails") =>
        Table(page, grid).Locator("tbody td[data-column=Name]");

    protected static ILocator Readout(IPage page, string name) =>
        page.GetByRole(AriaRole.Status, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Row_ShouldBeClicked_WhenEnterIsPressedOnACellWithNothingToGoInto(string browser)
    {
        var page = await OpenRowsAsync(browser);

        await RowCell(page, "Ridge loop").FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(Readout(page, "Row clicks")).ToHaveTextAsync("Ridge loop");
        await Expect(Readout(page, "Selected")).ToHaveTextAsync("Ridge loop");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Row_ShouldNotBeClicked_WhenEnterStartsAnEdit(string browser)
    {
        var page = await OpenRowsAsync(browser);

        await page.Locator("td[data-editable]").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Textbox)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator("td[data-editable]")).ToBeFocusedAsync();
        await RowCell(page, "Creek path").FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        // Events reach the server in order, so this last click shows that the earlier keys raised none.
        await Expect(Readout(page, "Row clicks")).ToHaveTextAsync("Creek path");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Row_ShouldNotBeClicked_WhenF2IsPressedOnACellWithNothingToGoInto(string browser)
    {
        var page = await OpenRowsAsync(browser);

        await RowCell(page, "Ridge loop").FocusAsync();
        await page.Keyboard.PressAsync("F2");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Readout(page, "Row clicks")).ToHaveTextAsync("Ridge loop");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task HeaderRow_ShouldGetNoClick_WhenEnterIsPressedOnAHeaderCell(string browser)
    {
        var page = await OpenRowsAsync(browser);
        await page.EvaluateAsync("""
            () => {
                window.clicks = 0;
                document.addEventListener('click', () => window.clicks++, true);
            }
            """);

        await page.GetByRole(AriaRole.Columnheader, new() { Name = "Actions" }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        Assert.Equal(0, await page.EvaluateAsync<int>("() => window.clicks"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Row_ShouldBeClickedOnce_WhenEnterIsHeld(string browser)
    {
        var page = await OpenRowsAsync(browser);

        await RowCell(page, "Ridge loop").FocusAsync();
        await page.Keyboard.DownAsync("Enter");
        await page.Keyboard.DownAsync("Enter");
        await page.Keyboard.DownAsync("Enter");
        await page.Keyboard.UpAsync("Enter");
        await RowCell(page, "Creek path").ClickAsync();

        await Expect(Readout(page, "Row clicks")).ToHaveTextAsync("Ridge loop, Creek path");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Row_ShouldLoseOnlyOneRow_WhenEnterIsHeldOnADeleteButton(string browser)
    {
        var page = await OpenRowsAsync(browser);

        await DeleteButton(page, "Ridge loop").FocusAsync();
        await page.Keyboard.DownAsync("Enter");
        await Expect(DeleteButton(page, "Creek path")).ToBeFocusedAsync();
        await page.Keyboard.DownAsync("Enter");
        await page.Keyboard.DownAsync("Enter");
        await page.Keyboard.UpAsync("Enter");
        await page.GetByRole(AriaRole.Button, new() { Name = "Remove the first row" }).ClickAsync();

        // Events reach the server in order, so what this removes shows that the held key removed nothing more.
        await Expect(DeleteButton(page, "Creek path")).ToHaveCountAsync(0);
        await Expect(DeleteButton(page, "Pine Bluff")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Row_ShouldBeClicked_WhenEnterIsPressedWithTextSelected(string browser)
    {
        var page = await OpenRowsAsync(browser);
        await RowCell(page, "Ridge loop").DblClickAsync(new() { Position = OnTheText });
        Assert.NotEqual("", await page.EvaluateAsync<string>("() => document.getSelection().toString()"));

        await RowCell(page, "Creek path").FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(Readout(page, "Row clicks")).ToHaveTextAsync("Ridge loop, Creek path");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Focus_ShouldMoveToTheRowThatTakesItsPlace_WhenTheFocusedRowIsRemoved(string browser)
    {
        var page = await OpenRowsAsync(browser);

        await DeleteButton(page, "Ridge loop").FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(DeleteButton(page, "Ridge loop")).ToHaveCountAsync(0);
        await Expect(DeleteButton(page, "Creek path")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Focus_ShouldMoveToTheRowThatTakesItsPlace_WhenTheFocusedRowIsRemovedFromAVirtualizedGrid(string browser)
    {
        var page = await OpenRowsAsync(browser);

        await DeleteButton(page, "Stop 1").FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(DeleteButton(page, "Stop 1")).ToHaveCountAsync(0);
        await Expect(DeleteButton(page, "Stop 2")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Focus_ShouldMoveToTheLastRow_WhenTheFocusedLastRowIsRemoved(string browser)
    {
        var page = await OpenRowsAsync(browser);

        await DeleteButton(page, "Pine Bluff").FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(DeleteButton(page, "Pine Bluff")).ToHaveCountAsync(0);
        await Expect(DeleteButton(page, "Creek path")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldAddToTheSorts_WhenShiftEnterIsPressedOnAHeader(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        await page.Tb().Grid("Trails").SortAsync("Region");

        await Table(page, "Trails").Locator("th[data-title=Name] [data-part=sort]").FocusAsync();
        await page.Keyboard.PressAsync("Shift+Enter");

        await Expect(Table(page, "Trails").Locator(".tb-data-grid__sort-order")).ToHaveCountAsync(2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldSortFromTheKeyboard_WhenEnterIsPressedOnAHeader(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var header = Table(page, "Trails").Locator("th[data-title='Length (km)']");
        await Expect(page.Locator(".tb-data-grid").First).ToHaveAttributeAsync("data-ready", "");

        await header.GetByRole(AriaRole.Button).FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(header).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(Names(page).First).ToHaveTextAsync("Waterfall spur");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FilterMenu_ShouldLeaveTheRowsAlone_WhenClosedWithEscapeBeforeApplying(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = Table(page, "Campsites") }))
            .ToHaveAttributeAsync("data-ready", "");
        var button = page.GetByRole(AriaRole.Button, new() { Name = "Filter Sites", Exact = true });
        var menu = page.GetByRole(AriaRole.Dialog, new() { Name = "Filter Sites" });

        await button.ClickAsync();
        // A number column's end alignment stays out of its menu.
        await Expect(menu.Locator(".tb-data-grid__filter-form")).ToHaveCSSAsync("text-align", "start");
        await menu.GetByLabel("Value").FillAsync("40");
        await page.Keyboard.PressAsync("Escape");

        await Expect(menu).ToBeHiddenAsync();
        await Expect(button).ToBeFocusedAsync();
        await Expect(Names(page, "Campsites")).ToHaveCountAsync(5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FilterMenu_ShouldApplyTheValue_WhenEnterIsPressedInIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = Table(page, "Campsites") }))
            .ToHaveAttributeAsync("data-ready", "");
        var menu = page.GetByRole(AriaRole.Dialog, new() { Name = "Filter Name" });

        await page.GetByRole(AriaRole.Button, new() { Name = "Filter Name", Exact = true }).ClickAsync();
        // The menu focuses its first field as it opens, so typing waits for that.
        await Expect(menu.GetByRole(AriaRole.Combobox)).ToBeFocusedAsync();
        await menu.GetByLabel("Value").PressSequentiallyAsync("dune");
        await page.Keyboard.PressAsync("Enter");

        await Expect(menu).ToBeHiddenAsync();
        await Expect(Names(page, "Campsites")).ToHaveTextAsync(["Dune Point"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filters_ShouldApplyWithoutSubmittingThePagesForm_WhenEnterIsPressedInThem(string browser)
    {
        var page = await OpenAsync(browser, "/grid-in-form");
        var menu = page.GetByRole(AriaRole.Dialog, new() { Name = "Filter Trail" });

        await page.Tb().Grid("Row trails").FilterAsync("Trail", "ridge");
        await Expect(page.Locator("[data-part=grid]").Nth(1)).ToHaveAttributeAsync("data-ready", "");
        await page.GetByRole(AriaRole.Button, new() { Name = "Filter Trail", Exact = true }).ClickAsync();
        await Expect(menu.GetByRole(AriaRole.Combobox)).ToBeFocusedAsync();
        await menu.GetByLabel("Value").PressSequentiallyAsync("cedar");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Table(page, "Row trails").Locator("tbody td")).ToHaveTextAsync(["Eagle Ridge"]);
        await Expect(Table(page, "Menu trails").Locator("tbody td[data-column=Trail]")).ToHaveTextAsync(["Cedar Flats"]);
        await Expect(page.GetByTestId("saves")).ToHaveTextAsync("Saves: 0");
        await page.Tb().Grid("Row trails").FilterAsync("Trail", "");
        await Expect(Table(page, "Row trails").Locator("tbody td"))
            .ToHaveTextAsync(["Pine Hollow", "Eagle Ridge", "Cedar Flats"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FilterMenu_ShouldLeaveThePagesFormUnsubmitted_WhenEnterIsPressedOnAChoice(string browser)
    {
        var page = await OpenAsync(browser, "/grid-in-form");
        var menu = page.GetByRole(AriaRole.Dialog, new() { Name = "Filter Pick" });
        await Expect(page.Locator("[data-part=grid]").Nth(1)).ToHaveAttributeAsync("data-ready", "");

        await page.GetByRole(AriaRole.Button, new() { Name = "Filter Pick", Exact = true }).ClickAsync();
        // The menu focuses its first choice as it opens, so the test waits for that before focusing another.
        await Expect(menu.GetByRole(AriaRole.Checkbox, new() { Name = "Pine Hollow" })).ToBeFocusedAsync();
        await menu.GetByRole(AriaRole.Checkbox, new() { Name = "Eagle Ridge" }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Space");
        await menu.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();

        await Expect(Table(page, "Menu trails").Locator("tbody td[data-column=pick]"))
            .ToHaveTextAsync(["Eagle Ridge"]);
        await Expect(page.GetByTestId("saves")).ToHaveTextAsync("Saves: 0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FilterMenu_ShouldApply_WhenEnterIsPressedOnItsApplyButton(string browser)
    {
        var page = await OpenAsync(browser, "/grid-in-form");
        var menu = page.GetByRole(AriaRole.Dialog, new() { Name = "Filter Trail" });
        await Expect(page.Locator("[data-part=grid]").Nth(1)).ToHaveAttributeAsync("data-ready", "");

        await page.GetByRole(AriaRole.Button, new() { Name = "Filter Trail", Exact = true }).ClickAsync();
        await Expect(menu.GetByRole(AriaRole.Combobox)).ToBeFocusedAsync();
        await menu.GetByLabel("Value").FillAsync("pine");
        await menu.GetByRole(AriaRole.Button, new() { Name = "Apply" }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(Table(page, "Menu trails").Locator("tbody td[data-column=Trail]")).ToHaveTextAsync(["Pine Hollow"]);
        await Expect(page.GetByTestId("saves")).ToHaveTextAsync("Saves: 0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualRows_ShouldKeepFocusOnAMovedRowsHandle_WhenItMovesOutOfView(string browser)
    {
        var page = await OpenAsync(browser, "/virtual-grid");
        var table = Table(page, "Stops");
        await Expect(page.Locator("[data-part=grid]:not([data-ready])")).ToHaveCountAsync(0);
        var handle = page.GetByRole(AriaRole.Button, new() { Name = "Move Stop 1", Exact = true });

        await handle.FocusAsync();
        await page.Keyboard.PressAsync("End");

        await Expect(handle).ToBeFocusedAsync();
        await Expect(table.Locator("tr[aria-rowindex='201']")).ToContainTextAsync("Stop 1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GroupToggle_ShouldCloseItsGroupAndKeepFocus_WhenEnterIsPressedOnIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var table = Table(page, "Routes by region");
        var toggle = table.GetByRole(AriaRole.Button, new() { Name = "Region: North ridge Rows: 2" });

        await toggle.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(toggle).ToBeFocusedAsync();
        await Expect(table.Locator("tbody td[data-column=Name]")).ToHaveTextAsync(["Summit Gate", "Lake camp",
            "Cedar Flats", "Fern Gully", "Waterfall spur"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GroupBy_ShouldGroupWithoutSubmittingThePagesForm_WhenPickedWithTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/grid-in-form");
        var table = Table(page, "Grouped trails");

        var groupBy = page.GetByRole(AriaRole.Combobox, new() { Name = "Group by" });
        await Expect(groupBy).ToHaveAttributeAsync("data-ready", "");
        await groupBy.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Enter");

        await Expect(table.Locator("tr[data-part=group]")).ToHaveCountAsync(3);
        await Expect(page.GetByTestId("saves")).ToHaveTextAsync("Saves: 0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellNavigation_ShouldMoveBetweenCellsIntoAFieldAndBack_WhenDrivenByKeys(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail keys" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");
        await Expect(grid.Locator("[tabindex='0']")).ToHaveCountAsync(1);
        var selectAll = grid.GetByRole(AriaRole.Checkbox, new() { Name = "Select all rows" });
        await selectAll.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(grid.GetByRole(AriaRole.Button, new() { Name = "Name", Exact = true })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        var filter = grid.Locator("td[data-part=filter][data-column=Name]");
        await Expect(filter).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(filter.Locator("input")).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("cedar");
        await page.Keyboard.PressAsync("Escape");
        await Expect(filter).ToBeFocusedAsync();
        await Expect(grid.Locator("tbody tr[data-part=row]")).ToHaveCountAsync(1);
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync(" ");
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("Enter");

        await Expect(grid.Locator("tbody tr[data-part=row]")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Readout(page, "Chosen and opened")).ToHaveTextAsync("Chosen: 1. Map of Cedar Flats.");
        await Expect(grid.Locator("[tabindex='0']")).ToHaveCountAsync(1);
        await Expect(page.Tb().Grid("Trail keys").Row("Cedar Flats")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellNavigation_ShouldLeaveTheRowsUnselected_WhenSpaceIsPressedOnAHeader(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail keys" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");
        await grid.Locator("thead th[data-column=map]").ClickAsync();
        var scrolled = await page.EvaluateAsync<double>("() => window.scrollY");

        await page.Keyboard.PressAsync(" ");
        Assert.Equal(scrolled, await page.EvaluateAsync<double>("() => window.scrollY"));
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync(" ");

        // Only the body row's Space selects; had the header's selected every row, this one would now be left out.
        await Expect(Readout(page, "Chosen and opened")).ToHaveTextAsync("Chosen: 1.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellNavigation_ShouldMoveOverRowHandlesWithoutMovingRows_WhenArrowsArePressed(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Stops in order" });
        await Expect(page.Locator("[data-part=grid]").Nth(2)).ToHaveAttributeAsync("data-ready", "");
        await grid.Locator("tbody td[data-column=stop]").First.ClickAsync();

        await page.Keyboard.PressAsync("Home");
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(grid.Locator("tbody tr[data-part=row]").Nth(1).Locator("td").First).ToBeFocusedAsync();
        await Expect(grid.Locator("tbody td[data-column=stop]")).ToHaveTextAsync(["Trailhead", "Lake camp",
            "Summit Gate"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellNavigation_ShouldScrollToRowsNotYetDrawn_WhenControlEndAndHomeArePressed(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Many numbers" });
        await Expect(page.Locator("[data-part=grid]").First).ToHaveAttributeAsync("data-ready", "");
        await grid.Locator("tbody td[data-column=Number]").First.ClickAsync();

        await page.Keyboard.PressAsync("PageDown");
        await Expect(grid.Locator("tbody td:focus")).ToHaveAttributeAsync("data-column", "Number");
        Assert.True(await grid.Locator("tbody tr:has(td:focus)").EvaluateAsync<int>("row => +row.ariaRowIndex") > 4);
        await page.Keyboard.PressAsync("Control+End");
        await Expect(grid.Locator("tr[aria-rowindex='301'] td[data-column=double]")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Control+Home");
        await Expect(grid.Locator("thead th").First).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "After the grid" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellNavigation_ShouldMoveFromARowsRadioWithoutChoosingIt_WhenAnArrowIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Many numbers" });
        await Expect(page.Locator("[data-part=grid]").First).ToHaveAttributeAsync("data-ready", "");
        // Rows are found by their place, since a virtualized grid may drop the first as it scrolls.
        await grid.Locator("tr[aria-rowindex='2'] input[type=radio]").FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(grid.Locator("tr[aria-rowindex='3'] input[type=radio]")).ToBeFocusedAsync();
        await Expect(grid.Locator("tbody tr[aria-selected='true']")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellNavigation_ShouldKeepItsColumnThroughAGroupsHeading_WhenMovingDown(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Grouped numbers" });
        await Expect(page.Locator("[data-part=grid]").Nth(1)).ToHaveAttributeAsync("data-ready", "");
        await grid.Locator("tbody tr[data-part=row]").Nth(2).Locator("td[data-column=odd]").ClickAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(grid.GetByRole(AriaRole.Button, new() { Name = "Odd: True Rows: 3" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(grid.Locator("tbody tr[data-part=row]").Nth(3).Locator("td[data-column=odd]")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellNavigation_ShouldGoRoundACellsButtonsAndBackOut_WhenEnteredWithEnter(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Grouped numbers" });
        await Expect(page.Locator("[data-part=grid]").Nth(1)).ToHaveAttributeAsync("data-ready", "");
        var cell = grid.Locator("tbody tr[data-part=row]").First.Locator("td[data-column=actions]");
        await cell.ClickAsync(new() { Position = new() { X = 1, Y = 1 } });
        await Expect(cell).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(cell.GetByRole(AriaRole.Button, new() { Name = "Up 2" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(cell.GetByRole(AriaRole.Button, new() { Name = "Down 2" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(cell.GetByRole(AriaRole.Button, new() { Name = "Up 2" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(cell).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellNavigation_ShouldMoveOverAMenusCellWithoutOpeningIt_WhenArrowsArePressed(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Stops in order" });
        await Expect(page.Locator("[data-part=grid]").Nth(2)).ToHaveAttributeAsync("data-ready", "");
        await grid.Locator("tbody td[data-column=stop]").First.ClickAsync();

        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(grid.Locator("tbody tr[data-part=row]").Nth(1).Locator("td[data-column=more]")).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.Menu)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellNavigation_ShouldReachRetry_WhenAVirtualizedGridsRowsFailToLoad(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Broken numbers" });
        await Expect(page.Locator("[data-part=grid]").Nth(3)).ToHaveAttributeAsync("data-ready", "");
        await Expect(grid.Locator("[data-part=failed]")).ToBeVisibleAsync();
        await grid.Locator("thead th").First.ClickAsync();

        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(grid.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellEdit_ShouldSaveAndFocusTheCellAgain_WhenEnterStartsAndEndsIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail edits" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");
        var cell = grid.Locator("tr[data-key='3'] td[data-column=Name]");
        await cell.ClickAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(cell.Locator("input")).ToBeFocusedAsync();
        await cell.Locator("input").FillAsync("Cedar Flats loop");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Readout(page, "Last cell saved")).ToHaveTextAsync("Saved Name of Cedar Flats loop.");
        await Expect(cell).ToHaveTextAsync("Cedar Flats loop");
        await Expect(cell).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellEdit_ShouldStayOpenWithWhyAndCancel_WhenACheckFailsAndEscapeIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail edits" });
        var cell = grid.Locator("tr[data-key='1'] td[data-column=LengthKm]");
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");

        await cell.DblClickAsync();
        await cell.GetByRole(AriaRole.Spinbutton).FillAsync("99");
        await page.Keyboard.PressAsync("Enter");
        await Expect(cell).ToContainTextAsync("between 0.5 and 50");
        await page.Keyboard.PressAsync("Escape");

        await Expect(cell.Locator("[data-part=editor]")).ToHaveCountAsync(0);
        await Expect(cell).ToHaveTextAsync("6.4");
        await Expect(Readout(page, "Last cell saved")).ToHaveTextAsync("Nothing saved yet.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellEdit_ShouldSaveWithoutSubmittingThePagesForm_WhenEnterIsPressedInIt(string browser)
    {
        var page = await OpenAsync(browser, "/grid-in-form");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Edited trails" });
        var cell = grid.Locator("tbody td").First;
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");

        await cell.DblClickAsync();
        await cell.Locator("input").FillAsync("Pine Hollow loop");
        await page.Keyboard.PressAsync("Enter");

        await Expect(cell).ToHaveTextAsync("Pine Hollow loop");
        await Expect(page.GetByTestId("saves")).ToHaveTextAsync("Saves: 0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellEdit_ShouldLeaveTheValue_WhenEscapeCancelsAChange(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail edits" });
        var cell = grid.Locator("tr[data-key='2'] td[data-column=Name]");
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");
        // A dialog around the grid would close on an Escape nothing has used; this page listener sees what it would.
        await page.EvaluateAsync("() => document.addEventListener('keydown', event => "
            + "window.escapeUsed = event.key === 'Escape' && event.defaultPrevented)");

        await cell.DblClickAsync();
        await cell.Locator("input").FillAsync("Changed");
        await page.Keyboard.PressAsync("Escape");

        Assert.True(await page.EvaluateAsync<bool>("() => window.escapeUsed"));

        await Expect(cell).ToHaveTextAsync("Eagle Ridge traverse");
        await Expect(cell).ToBeFocusedAsync();
        await Expect(Readout(page, "Last cell saved")).ToHaveTextAsync("Nothing saved yet.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellEdit_ShouldSaveAChoicePickedByKeysAlone_WhenItsListOpensAndClosesInTheEdit(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail edits" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");
        var cell = grid.Locator("tr[data-key='1'] td[data-column=Difficulty]");
        await cell.ClickAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(cell.GetByRole(AriaRole.Combobox)).ToBeFocusedAsync();
        await Expect(cell.GetByRole(AriaRole.Combobox)).ToHaveAttributeAsync("data-ready", "");
        await page.Keyboard.PressAsync("Enter");
        await Expect(cell.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(cell.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await Expect(cell.GetByRole(AriaRole.Combobox)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(cell.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Readout(page, "Last cell saved")).ToHaveTextAsync("Saved Difficulty of Pine Hollow loop.");
        await Expect(cell).ToHaveTextAsync("Moderate");
        await Expect(cell).ToBeFocusedAsync();
        await Expect(grid.Locator("tr[data-key='1']")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellEdit_ShouldKeepFocusInTheGrid_WhenTheSavedRowSortsElsewhere(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail edits" });
        await page.Tb().Grid("Trail edits").SortAsync("Name");
        var cell = grid.Locator("tr[data-key='3'] td[data-column=Name]");
        await cell.ClickAsync();

        await page.Keyboard.PressAsync("Enter");
        await cell.Locator("input").FillAsync("Zion Flats");
        await page.Keyboard.PressAsync("Enter");

        await Expect(grid.Locator("tbody tr[data-part=row]").Last).ToContainTextAsync("Zion Flats");
        await Expect(grid.Locator("tbody td:focus")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellEdit_ShouldKeepFocusInTheGrid_WhenTheSavedRowNoLongerMatchesItsFilter(string browser)
    {
        var page = await OpenAsync(browser, "/grid-in-form");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Edited trails" });
        await page.Tb().Grid("Edited trails").FilterAsync("Trail", "eagle");
        var cell = grid.Locator("tbody td[data-column=trail]");
        await Expect(cell).ToHaveTextAsync(["Eagle Ridge"]);
        await cell.ClickAsync();

        await page.Keyboard.PressAsync("Enter");
        await cell.Locator("input").FillAsync("Lake shore");
        await page.Keyboard.PressAsync("Enter");

        await Expect(grid.Locator("[data-part=empty]")).ToBeVisibleAsync();
        await Expect(grid.Locator(":focus")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RowEdit_ShouldKeepTabInTheRowAndSaveItWhole_WhenEnterIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail rows" });
        var row = grid.Locator("tr[data-key='3']");
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");
        await row.Locator("td[data-column=Name]").ClickAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(row.Locator("td[data-column=Name] input")).ToBeFocusedAsync();
        await row.Locator("td[data-column=Name] input").FillAsync("Cedar Flats loop");
        await page.Keyboard.PressAsync("Tab");
        await row.GetByRole(AriaRole.Spinbutton).FillAsync("4.5");
        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Tab");
        await Expect(row.GetByRole(AriaRole.Button, new() { Name = "Save Cedar Flats" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Tab");
        await Expect(row.Locator("td[data-column=Name] input")).ToBeFocusedAsync();
        await Expect(Readout(page, "Last row saved")).ToHaveTextAsync("No row saved yet.");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Readout(page, "Last row saved")).ToHaveTextAsync("Saved Cedar Flats loop.");
        await Expect(row.Locator("td")).ToHaveTextAsync(["Cedar Flats loop", "4.5", "Easy", "South"]);
        await Expect(row.Locator("td[data-column=Name]")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RowEdit_ShouldLeaveTheRow_WhenEscapeIsPressedOnItsButtons(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail rows" });
        var row = grid.Locator("tr[data-key='1']");
        // The grid's script takes Escape, so the keys wait for it to attach.
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid })).ToHaveAttributeAsync("data-ready", "");
        await page.EvaluateAsync("() => document.addEventListener('keydown', event => "
            + "window.escapeUsed = event.key === 'Escape' && event.defaultPrevented)");

        await row.Locator("td[data-column=Name]").DblClickAsync();
        await row.Locator("td[data-column=Name] input").FillAsync("Changed");
        await row.GetByRole(AriaRole.Button, new() { Name = "Save Pine Hollow loop" }).FocusAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(row.Locator("[data-part=editor], [data-part=row-actions]")).ToHaveCountAsync(0);
        await Expect(row.Locator("td[data-column=Name]")).ToHaveTextAsync("Pine Hollow loop");
        await Expect(Readout(page, "Last row saved")).ToHaveTextAsync("No row saved yet.");
        Assert.True(await page.EvaluateAsync<bool>("() => window.escapeUsed"));
    }
}
