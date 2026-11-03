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
        await Expect(scroll).Not.ToHaveCSSAsync("scroll-padding-left", "auto");

        await page.GetByRole(AriaRole.Button, new() { Name = "Unpin all" }).ClickAsync();

        await Expect(table.Locator("[data-pinned]")).ToHaveCountAsync(0);
        await Expect(scroll).ToHaveCSSAsync("scroll-padding-left", "auto");
        await Expect(scroll).ToHaveCSSAsync("scroll-padding-right", "auto");
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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldShadeEverySelectedRowAlike_WhenStripedRowsAreSelected(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var trails = page.Tb().Grid("Trails");

        await trails.SelectAsync("Pine Hollow loop");
        await trails.SelectAsync("Eagle Ridge traverse");
        await page.Mouse.MoveAsync(0, 0);

        var rows = Table(page, "Trails").Locator("tbody tr[data-part=row]");
        var odd = await rows.Nth(0).EvaluateAsync<string>("row => getComputedStyle(row).backgroundColor");
        await Expect(rows.Nth(1)).ToHaveCSSAsync("background-color", odd);
        Assert.NotEqual(odd, await rows.Nth(3).EvaluateAsync<string>("row => getComputedStyle(row).backgroundColor"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldScrollAFocusedControlOutFromUnderItsHeader_WhenTheRowsScrollInAHeight(string browser)
    {
        var page = await OpenAsync(browser, "/pinned-grid");
        var table = Table(page, "Numbers");
        await Expect(table).ToHaveAttributeAsync("style", new Regex("--tb-data-grid-pin-end-0"));
        var box = table.Locator("tbody tr[data-part=row]").Nth(1).Locator("input[type=checkbox]");
        await box.EvaluateAsync("box => { const scroll = box.closest('.tb-data-grid__scroll');"
            + " const header = box.closest('table').tHead.getBoundingClientRect();"
            + " scroll.scrollTop += box.getBoundingClientRect().top - header.top - 4; }");
        Assert.False(await box.EvaluateAsync<bool>("box => { const at = box.getBoundingClientRect();"
            + " return document.elementFromPoint(at.left + at.width / 2, at.top + at.height / 2) === box; }"));

        await box.FocusAsync();

        Assert.True(await box.EvaluateAsync<bool>("box => { const at = box.getBoundingClientRect();"
            + " return document.elementFromPoint(at.left + at.width / 2, at.top + at.height / 2) === box; }"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualRows_ShouldDrawOnlyTheRowsInViewAndCountThemAll_WhenThousandsAreHeld(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var table = Table(page, "Readings");
        await Expect(table.Locator("tbody tr[data-part=row]").First).ToHaveAttributeAsync("aria-rowindex", "3");
        await Expect(table).ToHaveAttributeAsync("aria-rowcount", "10002");
        Assert.InRange(await table.Locator("tbody tr[data-part=row]").CountAsync(), 1, 60);

        await table.Locator("xpath=..").EvaluateAsync("box => box.scrollTop = box.scrollHeight");

        await Expect(table.Locator("tr[aria-rowindex='10002']")).ToContainTextAsync("10000");
        Assert.InRange(await table.Locator("tbody tr[data-part=row]").CountAsync(), 1, 60);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualRows_ShouldScrollToARowClearOfTheHeader_WhenItIsntDrawnYet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var table = Table(page, "Readings");
        await Expect(table.Locator("tbody tr[data-part=row]").First).ToBeVisibleAsync();
        await Expect(table.Locator("tr[data-key='5000']")).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Button, new() { Name = "Go to reading 5000" }).ClickAsync();

        var row = table.Locator("tr[data-key='5000']");
        await Expect(row).ToBeInViewportAsync();
        Assert.True(await row.EvaluateAsync<bool>("row => { const box = row.getBoundingClientRect();"
            + " return row.contains(document.elementFromPoint(box.left + 4, box.top + 4)); }"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualRows_ShouldLoadOnlyTheRowsInViewAndAgainWhenSorted_WhenAProviderGivesThem(string browser)
    {
        var page = await OpenAsync(browser, "/virtual-grid");
        var table = Table(page, "Numbers");
        var asked = page.GetByTestId("asked");
        await Expect(table.Locator("tbody tr[data-part=row]").First).ToContainTextAsync("1");
        Assert.InRange(int.Parse((await asked.InnerTextAsync()).Split(' ')[1]), 1, 50);
        await Expect(table.Locator("[data-part=select-all]")).ToHaveCountAsync(0);

        await table.Locator("xpath=..").EvaluateAsync("box => box.scrollTop = box.scrollHeight");

        await Expect(table.Locator("tr[aria-rowindex='1002']")).ToContainTextAsync("1000");
        await table.Locator("xpath=..").EvaluateAsync("box => box.scrollTop = 0");
        await page.Tb().Grid("Numbers").SortAsync("Number", descending: true);
        await Expect(table.Locator("tr[aria-rowindex='3']")).ToContainTextAsync("1000");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualRows_ShouldShowTheRowsAFilterLeaves_WhenFilteredWhileScrolledDown(string browser)
    {
        var page = await OpenAsync(browser, "/virtual-grid");
        var table = Table(page, "Numbers");
        await Expect(table.Locator("tbody tr[data-part=row]").First).ToContainTextAsync("1");
        await table.Locator("xpath=..").EvaluateAsync("box => box.scrollTop = box.scrollHeight");
        await Expect(table.Locator("tr[aria-rowindex='1002']")).ToContainTextAsync("1000");

        await page.Tb().Grid("Numbers").FilterAsync("Number", "5");

        await Expect(table.Locator("tbody tr[data-part=row]")).ToHaveTextAsync(["5"]);
        await Expect(table.Locator("[data-part=empty]")).ToHaveCountAsync(0);
        await Expect(table).ToHaveAttributeAsync("aria-rowcount", "3");
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
        await Expect(page.GetByTestId("keys")).ToHaveTextAsync("Chosen: 1. Map of Cedar Flats.");
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
        await Expect(page.GetByTestId("keys")).ToHaveTextAsync("Chosen: 1.");
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
    public async Task CellNavigation_ShouldKeepAStopForTab_WhenTheButtonHoldingItTurnsItselfOff(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Stops in order" });
        await Expect(page.Locator("[data-part=grid]").Nth(2)).ToHaveAttributeAsync("data-ready", "");
        var visit = grid.GetByRole(AriaRole.Button, new() { Name = "Visit Lake camp" });

        await visit.ClickAsync();

        await Expect(visit).ToBeDisabledAsync();
        await Expect(grid.Locator("[tabindex='0']:not(:disabled)")).ToHaveCountAsync(1);
        await Expect(grid.Locator("[tabindex='0']")).ToHaveCountAsync(1);
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
    public async Task CellNavigation_ShouldGiveBackEveryTabStop_WhenTurnedOff(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        await Expect(page.Locator("[data-part=grid]").Nth(1)).ToHaveAttributeAsync("data-ready", "");
        await Expect(page.GetByRole(AriaRole.Grid, new() { Name = "Grouped numbers" }).Locator("button[tabindex='-1']"))
            .Not.ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Button, new() { Name = "Turn off keys" }).ClickAsync();

        var table = Table(page, "Grouped numbers");
        await Expect(table.Locator("[tabindex]")).ToHaveCountAsync(0);
        await Expect(table.Locator("[data-tb-tab]")).ToHaveCountAsync(0);
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

    private static ILocator Table(IPage page, string name) =>
        page.GetByRole(AriaRole.Table, new() { Name = name, Exact = true });

    private static ILocator Names(IPage page, string grid = "Trails") =>
        Table(page, grid).Locator("tbody td[data-column=Name]");
}
