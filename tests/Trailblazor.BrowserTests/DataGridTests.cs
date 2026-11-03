using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DataGridTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldSortByAColumnAndSaySo_WhenItsHeaderIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var trails = page.Tb().Grid("Trails");

        await trails.SortAsync("Name");
        await Expect(Names(page)).ToHaveTextAsync(["Cedar Flats", "Eagle Ridge traverse", "Fern Gully", "Lake camp",
            "Pine Hollow loop"]);
        await trails.SortAsync("Name", descending: true);

        await Expect(Names(page).First).ToHaveTextAsync("Waterfall spur");
        await Expect(page.Locator("[data-part=announcer] [role=status] p").Last)
            .ToHaveTextAsync("Sorted by Name, descending.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldSortByOneColumnAlone_WhenAskedWhileSortedByTwo(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var trails = page.Tb().Grid("Trails");
        await trails.SortAsync("Region");
        await Table(page, "Trails").Locator("th[data-title=Name] [data-part=sort]")
            .ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(Table(page, "Trails").Locator(".tb-data-grid__sort-order")).ToHaveCountAsync(2);

        await trails.SortAsync("Region", descending: true);

        await Expect(Table(page, "Trails").Locator(".tb-data-grid__sort-order")).ToHaveCountAsync(0);
        await Expect(Table(page, "Trails").Locator("th[data-title=Region]")).ToHaveAttributeAsync("aria-sort",
            "descending");
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
    public async Task Grid_ShouldShowEachPageAndItsRange_WhenPagedOrGivenAnotherPageSize(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var summary = Table(page, "Trails").Locator("xpath=ancestor::*[contains(@class, 'tb-data-grid ')][1]")
            .Locator("[data-part=summary]");
        await Expect(summary).ToHaveTextAsync("1–5 of 7");

        await page.Tb().Grid("Trails").GoToPageAsync(2);
        await Expect(Names(page)).ToHaveTextAsync(["Summit Gate", "Lake camp"]);
        await Expect(summary).ToHaveTextAsync("6–7 of 7");
        await page.Tb().Select("Rows per page").ChooseAsync("10");

        await Expect(Names(page)).ToHaveCountAsync(7);
        await Expect(summary).ToHaveTextAsync("1–7 of 7");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldSelectByBoxRowAndHeaderButNotByACellsButton_WhenEachIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var trails = page.Tb().Grid("Trails");
        var chosen = page.GetByTestId("chosen");

        await trails.SelectAsync("Cedar Flats");
        await (await trails.CellAsync("Fern Gully", "Region")).ClickAsync();
        await Expect(chosen).ToContainTextAsync("Chosen: Cedar Flats, Fern Gully.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Plan Pine Hollow loop" }).ClickAsync();
        await Expect(chosen).ToContainTextAsync("Planning Pine Hollow loop.");
        await Expect(chosen).ToContainTextAsync("Chosen: Cedar Flats, Fern Gully.");
        var all = Table(page, "Trails").GetByRole(AriaRole.Checkbox, new() { Name = "Select all rows" });
        Assert.True(await all.EvaluateAsync<bool>("box => box.indeterminate"));
        await all.CheckAsync();

        await Expect(Table(page, "Trails").Locator("tr[data-part=row][data-selected]")).ToHaveCountAsync(5);
        Assert.False(await all.EvaluateAsync<bool>("box => box.indeterminate"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldPressOnlyThePlannedTrailsButton_WhenAnotherTrailIsPlanned(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var pine = page.GetByRole(AriaRole.Button, new() { Name = "Plan Pine Hollow loop" });
        await pine.ClickAsync();
        await Expect(pine).ToHaveAttributeAsync("aria-pressed", "true");

        await page.GetByRole(AriaRole.Button, new() { Name = "Plan Cedar Flats" }).ClickAsync();

        await Expect(page.GetByTestId("chosen")).ToContainTextAsync("Planning Cedar Flats.");
        await Expect(pine).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldLoadEachSortedPageFromTheProvider_WhenSortedPagedAndAClickedRowIsChosen(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var log = page.Tb().Grid("Hike log");
        var hours = Table(page, "Hike log").Locator("tbody td[data-column=Hours]");
        await Expect(hours).ToHaveCountAsync(4);

        // Read once, without retrying, so the helpers must have waited for the provider's rows.
        await log.SortAsync("Hours", descending: true);
        Assert.Equal(["7.0", "7.0", "7.0", "5.5"], await hours.AllTextContentsAsync());
        await log.GoToPageAsync(2);
        Assert.Equal("5.5", await hours.First.TextContentAsync());
        await (await log.CellAsync("Lake camp", "Hours")).ClickAsync();

        await Expect(log.Row("Lake camp")).ToHaveAttributeAsync("data-selected", "");
        await Expect(page.GetByTestId("hike")).ToContainTextAsync("chosen");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldAlignEachCellAsItsColumnSays_WhenShown(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var table = Table(page, "Trails");

        await Expect(table.Locator("thead th[data-column=LengthKm]")).ToHaveCSSAsync("text-align", "end");
        await Expect(table.Locator("tbody td[data-column=LengthKm]").First).ToHaveCSSAsync("text-align", "end");
        await Expect(table.Locator("tbody td[data-column=Name]").First).ToHaveCSSAsync("text-align", "start");
        await Expect(table.Locator("tbody td[data-part=select]").First).ToHaveCSSAsync("text-align", "center");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldShowOnlyMatchingRowsAndCountThem_WhenFilterTextIsTyped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");

        await Table(page, "Hikes").GetByLabel("Filter Name", new() { Exact = true }).PressSequentiallyAsync("ridge");

        await Expect(Names(page, "Hikes")).ToHaveTextAsync(["Eagle Ridge traverse"]);
        await Expect(page.Locator("[data-part=announcer] [role=status] p").Last).ToHaveTextAsync("Rows shown: 1.");
        await Expect(page.GetByTestId("filters")).ToHaveTextAsync("Filters: Name Contains ridge.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldShowRowsMeetingEveryColumnsFilter_WhenSeveralAreSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var hikes = page.Tb().Grid("Hikes");

        await page.Tb().Select("Filter Difficulty").ChooseAsync("Hard");
        await hikes.FilterAsync("Length (km)", "14.2");

        await Expect(Names(page, "Hikes")).ToHaveTextAsync(["Eagle Ridge traverse"]);
        await hikes.FilterAsync("Length (km)", "11.9");
        await Expect(Names(page, "Hikes")).ToHaveTextAsync(["Summit Gate"]);
        await hikes.FilterAsync("Length (km)", "");
        await Expect(Names(page, "Hikes")).ToHaveTextAsync(["Eagle Ridge traverse", "Summit Gate"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FilterMenu_ShouldFilterByAConditionAndCheckedChoices_WhenApplied(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var camps = page.Tb().Grid("Campsites");

        await camps.FilterAsync("Sites", "At least", "16");
        await Expect(Names(page, "Campsites")).ToHaveTextAsync(["Aspen Flat", "Dune Point", "Elk Meadow"]);
        await camps.FilterChoicesAsync("Region", "River valley");

        await Expect(Names(page, "Campsites")).ToHaveTextAsync(["Dune Point"]);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Filter Region, on" })).ToBeVisibleAsync();
        await Expect(page.GetByTestId("camp-filters"))
            .ToHaveTextAsync("Filters: Sites GreaterThanOrEqualTo 16; Region IsAnyOf River valley.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FilterMenu_ShouldLeaveTheRowsAlone_WhenClosedWithEscapeBeforeApplying(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        await Expect(page.Locator("[data-part=grid]").Nth(2)).ToHaveAttributeAsync("data-ready", "");
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
        await Expect(page.Locator("[data-part=grid]").Nth(2)).ToHaveAttributeAsync("data-ready", "");
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
    public async Task FilterMenu_ShouldShowWhatATemplatesFilterApplies_WhenItsControlIsUsed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        await Expect(page.Locator("[data-part=grid]").Nth(2)).ToHaveAttributeAsync("data-ready", "");

        await page.GetByRole(AriaRole.Button, new() { Name = "Filter Water", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Switch, new() { Name = "Only with water" }).ClickAsync();

        await Expect(Names(page, "Campsites")).ToHaveTextAsync(["Aspen Flat", "Dune Point", "Elk Meadow"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PinnedColumns_ShouldStayAtTheirSidesOverTheOtherCells_WhenTheRowsScrollSideways(string browser)
    {
        var page = await OpenAsync(browser, "/pinned-grid");
        var table = Table(page, "Numbers");
        var scroll = table.Locator("xpath=..");
        await Expect(table).ToHaveAttributeAsync("style", new Regex("--tb-data-grid-pin-end-0"));

        await scroll.EvaluateAsync("box => box.scrollLeft = 150");

        var view = await scroll.EvaluateAsync<double[]>(
            "box => [box.getBoundingClientRect().left + box.clientLeft, box.clientWidth]");
        var select = (await table.Locator("tbody td[data-part=select]").First.BoundingBoxAsync())!;
        var number = table.Locator("tbody td[data-column=Number]").First;
        var start = (await number.BoundingBoxAsync())!;
        var end = (await table.Locator("tbody td[data-column=Quadruple]").First.BoundingBoxAsync())!;
        Assert.Equal(view[0], select.X, 0.5);
        Assert.Equal(select.X + select.Width, start.X, 0.5);
        Assert.Equal(view[0] + view[1], end.X + end.Width, 0.5);
        Assert.True(await number.EvaluateAsync<bool>("cell => document.elementFromPoint(cell.getBoundingClientRect()"
            + ".left + 4, cell.getBoundingClientRect().top + 4)?.closest('td') === cell"));
        Assert.NotEqual("rgba(0, 0, 0, 0)", await number.EvaluateAsync<string>(
            "cell => getComputedStyle(cell).backgroundColor"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PinnedColumns_ShouldScrollAFocusedControlClearOfThem_WhenItWasUnderThem(string browser)
    {
        var page = await OpenAsync(browser, "/pinned-grid");
        var table = Table(page, "Numbers");
        await Expect(table).ToHaveAttributeAsync("style", new Regex("--tb-data-grid-pin-end-0"));
        await table.Locator("xpath=..").EvaluateAsync("box => box.scrollLeft = 100");
        var sort = table.Locator("th[data-column=double] [data-part=sort]");
        Assert.False(await sort.EvaluateAsync<bool>("button => { const box = button.getBoundingClientRect();"
            + " return button.contains(document.elementFromPoint(box.left + 2, box.top + box.height / 2)); }"));

        await sort.FocusAsync();

        Assert.True(await sort.EvaluateAsync<bool>("button => { const box = button.getBoundingClientRect();"
            + " return button.contains(document.elementFromPoint(box.left + 2, box.top + box.height / 2)); }"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PinnedColumns_ShouldStickPastTheColumnsBefore_WhenAColumnIsPinnedAfterLoading(string browser)
    {
        var page = await OpenAsync(browser, "/pinned-grid");
        var table = Table(page, "Numbers");
        await Expect(table).ToHaveAttributeAsync("style", new Regex("--tb-data-grid-pin-end-0"));

        await page.GetByRole(AriaRole.Button, new() { Name = "Pin tripled" }).ClickAsync();
        await Expect(table).ToHaveAttributeAsync("style", new Regex("--tb-data-grid-pin-2"));
        await table.Locator("xpath=..").EvaluateAsync("box => box.scrollLeft = box.scrollWidth");

        var number = (await table.Locator("tbody td[data-column=Number]").First.BoundingBoxAsync())!;
        var triple = (await table.Locator("tbody td[data-column=triple]").First.BoundingBoxAsync())!;
        Assert.Equal(number.X + number.Width, triple.X, 0.5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PinnedColumns_ShouldLeaveNoScrollPadding_WhenTheLastIsUnpinned(string browser)
    {
        var page = await OpenAsync(browser, "/pinned-grid");
        var table = Table(page, "Numbers");
        var scroll = table.Locator("xpath=..");
        await Expect(scroll).ToHaveAttributeAsync("style", new Regex("scroll-padding"));

        await page.GetByRole(AriaRole.Button, new() { Name = "Unpin all" }).ClickAsync();

        await Expect(table.Locator("[data-pinned]")).ToHaveCountAsync(0);
        await Expect(scroll).Not.ToHaveAttributeAsync("style", new Regex("scroll-padding"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PinnedColumns_ShouldPassUnderTheHeader_WhenTheRowsScrollDown(string browser)
    {
        var page = await OpenAsync(browser, "/pinned-grid");
        var table = Table(page, "Numbers");
        await Expect(table).ToHaveAttributeAsync("style", new Regex("--tb-data-grid-pin-end-0"));

        await table.Locator("xpath=..").EvaluateAsync("box => box.scrollTop = 100");

        foreach (var column in new[] { "Number", "Quadruple" })
        {
            Assert.True(await table.Locator($"thead th[data-column={column}]").EvaluateAsync<bool>(
                "header => { const box = header.getBoundingClientRect();"
                + " return document.elementFromPoint(box.left + 4, box.bottom - 4)?.closest('th') === header; }"));
        }
    }

    private static ILocator Table(IPage page, string name) =>
        page.GetByRole(AriaRole.Table, new() { Name = name, Exact = true });

    private static ILocator Names(IPage page, string grid = "Trails") =>
        Table(page, grid).Locator("tbody td[data-column=Name]");
}
