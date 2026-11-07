using Deque.AxeCore.Playwright;
using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DataGridTests(BrowserFixture fixture) : BrowserTest(fixture)
{

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
    public async Task Grid_ShouldSelectByBoxRowAndHeaderButNotByACellsButton_WhenEachIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var trails = page.Tb().Grid("Trails");
        var chosen = Readout(page, "Chosen and planned");

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
        await Expect(Readout(page, "Chosen hike")).ToContainTextAsync("chosen");
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
    public async Task FilterMenu_ShouldFilterByAConditionAndCheckedChoices_WhenApplied(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var camps = page.Tb().Grid("Campsites");

        await camps.FilterAsync("Sites", "At least", "16");
        await Expect(Names(page, "Campsites")).ToHaveTextAsync(["Aspen Flat", "Dune Point", "Elk Meadow"]);
        await camps.FilterChoicesAsync("Region", "River valley");

        await Expect(Names(page, "Campsites")).ToHaveTextAsync(["Dune Point"]);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Filter Region, on" })).ToBeVisibleAsync();
        await Expect(Readout(page, "Camp filters"))
            .ToHaveTextAsync("Filters: Sites GreaterThanOrEqualTo 16; Region IsAnyOf River valley.");
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

    private static ILocator ScrollBox(IPage page, string grid) => page.Locator("[data-part=scroll]")
        .Filter(new() { Has = page.Locator($"table[aria-label='{grid}']") });

    // The script measures the box on the frame after it attaches, which data-ready alone doesn't promise.
    private static async Task WaitForTheScrollStopAsync(IPage page, string grid)
    {
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = page.Locator($"table[aria-label='{grid}']") }))
            .ToHaveAttributeAsync("data-ready", "");
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ScrollBox_ShouldBeANamedTabStop_WhenAGridWithNoHeightIsTooWide(string browser)
    {
        var page = await OpenAsync(browser, "/data-grid-scroll");

        var box = ScrollBox(page, "Wide trails");

        await Expect(box).ToHaveAttributeAsync("tabindex", "0");
        await Expect(box).ToHaveAttributeAsync("role", "region");
        await Expect(box).ToHaveAttributeAsync("aria-label", "Wide trails");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ScrollBox_ShouldNotBeATabStop_WhenItsHeightLeavesTheRowsRoom(string browser)
    {
        var page = await OpenAsync(browser, "/data-grid-scroll");

        var box = ScrollBox(page, "Fitting trails");

        await Expect(box).Not.ToHaveAttributeAsync("tabindex", "0");
        await Expect(box).Not.ToHaveAttributeAsync("role", "region");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ScrollBox_ShouldStopBeingATabStop_WhenItsRowsNoLongerOverflow(string browser)
    {
        var page = await OpenAsync(browser, "/data-grid-scroll");
        var box = ScrollBox(page, "Tall trails");
        await Expect(box).ToHaveAttributeAsync("tabindex", "0");

        await page.GetByRole(AriaRole.Button, new() { Name = "Keep one row" }).ClickAsync();

        await Expect(box).Not.ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ScrollBox_ShouldStayOutOfTheTabOrder_WhenAddingAHeightLeavesTheRowsRoom(string browser)
    {
        var page = await OpenAsync(browser, "/data-grid-scroll");
        var box = ScrollBox(page, "Fitting, then with a height");
        await WaitForTheScrollStopAsync(page, "Fitting, then with a height");
        await Expect(box).Not.ToHaveAttributeAsync("tabindex", "0");

        await page.GetByRole(AriaRole.Button, new() { Name = "Give it a height" }).ClickAsync();

        await Expect(box).ToHaveAttributeAsync("style", new Regex("30rem"));
        await Expect(box).Not.ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ScrollBox_ShouldKeepItsTabStop_WhenAWideGridLosesItsHeight(string browser)
    {
        var page = await OpenAsync(browser, "/data-grid-scroll");
        var box = ScrollBox(page, "Wide, then with no height");
        await WaitForTheScrollStopAsync(page, "Wide, then with no height");
        await Expect(box).ToHaveAttributeAsync("tabindex", "0");

        await page.GetByRole(AriaRole.Button, new() { Name = "Take its height away" }).ClickAsync();

        await Expect(box).Not.ToHaveAttributeAsync("style", new Regex("30rem"));
        await Expect(box).ToHaveAttributeAsync("tabindex", "0");
        await Expect(box).ToHaveAttributeAsync("role", "region");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ScrollBox_ShouldBeATabStopWithNoRegion_WhenAWideGridHasNoName(string browser)
    {
        var page = await OpenAsync(browser, "/data-grid-scroll");

        var box = page.Locator("#unnamed [data-part=scroll]");

        await Expect(box).ToHaveAttributeAsync("tabindex", "0");
        await Expect(box).Not.ToHaveAttributeAsync("role", "region");
        await Expect(box).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ScrollBox_ShouldNotBeATabStop_WhenCellNavigationScrollsIt(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        await WaitForTheScrollStopAsync(page, "Many numbers");

        var box = ScrollBox(page, "Many numbers");

        await Expect(box).Not.ToHaveAttributeAsync("tabindex", "0");
        Assert.True(await box.EvaluateAsync<bool>("box => box.scrollHeight > box.clientHeight"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ScrollBox_ShouldBecomeATabStop_WhenCellNavigationIsTurnedOffOnAWideGrid(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        await page.SetViewportSizeAsync(200, 600);
        await WaitForTheScrollStopAsync(page, "Grouped numbers");
        var box = ScrollBox(page, "Grouped numbers");
        await Expect(box).Not.ToHaveAttributeAsync("tabindex", "0");

        await page.GetByRole(AriaRole.Button, new() { Name = "Turn off keys" }).ClickAsync();

        await Expect(box).ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldHaveNoAccessibilityViolations_WhenItsBoxIsTooWide(string browser)
    {
        var page = await OpenAsync(browser, "/data-grid-scroll");
        await Expect(ScrollBox(page, "Wide trails")).ToHaveAttributeAsync("tabindex", "0");

        var results = await page.Locator("[data-part=scroll]").First.RunAxe();

        Assert.Empty(results.Violations);
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
    public async Task CellEdit_ShouldSave_WhenFocusLeavesItsField(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail edits" });
        var cell = grid.Locator("tr[data-key='2'] td[data-column=Name]");

        await cell.DblClickAsync();
        await cell.Locator("input").FillAsync("Eagle Ridge");
        await Readout(page, "Last cell saved").ClickAsync();

        await Expect(Readout(page, "Last cell saved")).ToHaveTextAsync("Saved Name of Eagle Ridge.");
        await Expect(cell).ToHaveTextAsync("Eagle Ridge");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellEdit_ShouldSaveWithoutSubmittingThePagesForm_WhenEnterIsPressedInIt(string browser)
    {
        var page = await OpenAsync(browser, "/grid-in-form");
        var cell = page.GetByRole(AriaRole.Grid, new() { Name = "Edited trails" }).Locator("tbody td").First;

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
    public async Task CellEdit_ShouldSaveAChoiceClickedInItsList_WhenTheListIsOpen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail edits" });
        var cell = grid.Locator("tr[data-key='2'] td[data-column=Difficulty]");

        await cell.DblClickAsync();
        await cell.GetByRole(AriaRole.Combobox).ClickAsync();
        await cell.GetByRole(AriaRole.Option, new() { Name = "Moderate" }).ClickAsync();

        await Expect(Readout(page, "Last cell saved")).ToHaveTextAsync("Saved Difficulty of Eagle Ridge traverse.");
        await Expect(cell).ToHaveTextAsync("Moderate");
        await Expect(grid.Locator("tr[data-key='2']")).ToHaveAttributeAsync("aria-selected", "true");
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
    public async Task CellEdit_ShouldSelectTheRowOnce_WhenADoubleClickOpensAnEdit(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail edits" });
        var row = grid.Locator("tr[data-key='1']");

        await row.Locator("td[data-column=Name]").DblClickAsync();

        await Expect(row.Locator("[data-part=editor]")).ToHaveCountAsync(1);
        await Expect(row).ToHaveAttributeAsync("aria-selected", "true");
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
    public async Task RowEdit_ShouldSave_WhenFocusLeavesTheRow(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail rows" });
        var row = grid.Locator("tr[data-key='2']");

        await row.Locator("td[data-column=LengthKm]").DblClickAsync();
        await row.GetByRole(AriaRole.Spinbutton).FillAsync("15");
        await row.Locator("td[data-column=Name] input").ClickAsync();
        await Expect(Readout(page, "Last row saved")).ToHaveTextAsync("No row saved yet.");
        await Readout(page, "Last row saved").ClickAsync();

        await Expect(Readout(page, "Last row saved")).ToHaveTextAsync("Saved Eagle Ridge traverse.");
        await Expect(row.Locator("td[data-column=LengthKm]")).ToHaveTextAsync("15.0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RowEdit_ShouldSave_WhenFocusLeavesTheRowFromACellWithNoField(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail rows" });
        var row = grid.Locator("tr[data-key='3']");

        await row.Locator("td[data-column=Name]").DblClickAsync();
        await row.Locator("td[data-column=Name] input").FillAsync("Cedar Flats loop");
        await row.Locator("td[data-column=Region]").ClickAsync();
        await Expect(Readout(page, "Last row saved")).ToHaveTextAsync("No row saved yet.");
        await Readout(page, "Last row saved").ClickAsync();

        await Expect(Readout(page, "Last row saved")).ToHaveTextAsync("Saved Cedar Flats loop.");
        await Expect(row.Locator("[data-part=editor]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RowEdit_ShouldSaveNothing_WhenCancelIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail rows" });
        var row = grid.Locator("tr[data-key='2']");

        await row.Locator("td[data-column=Name]").DblClickAsync();
        await row.Locator("td[data-column=Name] input").FillAsync("Eagle Ridge");
        // Some browsers, such as Safari, don't focus a pressed button, so pressing one mustn't take focus at all.
        await row.GetByRole(AriaRole.Button, new() { Name = "Cancel editing Eagle Ridge traverse" }).HoverAsync();
        await page.Mouse.DownAsync();
        await Expect(row.Locator("td[data-column=Name] input")).ToBeFocusedAsync();
        await page.Mouse.UpAsync();

        await Expect(row.Locator("[data-part=editor]")).ToHaveCountAsync(0);
        await Expect(row.Locator("td[data-column=Name]")).ToHaveTextAsync("Eagle Ridge traverse");
        await Expect(Readout(page, "Last row saved")).ToHaveTextAsync("No row saved yet.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RowEdit_ShouldLeaveTheRow_WhenEscapeIsPressedOnItsButtons(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail rows" });
        var row = grid.Locator("tr[data-key='1']");
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

    private static ILocator Table(IPage page, string name) =>
        page.GetByRole(AriaRole.Table, new() { Name = name, Exact = true });

    private static ILocator Names(IPage page, string grid = "Trails") =>
        Table(page, grid).Locator("tbody td[data-column=Name]");

    private static ILocator Readout(IPage page, string name) =>
        page.GetByRole(AriaRole.Status, new() { Name = name, Exact = true });
}
