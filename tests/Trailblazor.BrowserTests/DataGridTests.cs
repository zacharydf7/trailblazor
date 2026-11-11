using Deque.AxeCore.Playwright;
using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DataGridTests(BrowserFixture fixture) : DataGridKeysTests(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Links_ShouldSortAndPageTheGrid_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-grid-links");
        var names = page.Locator("tbody td");
        await Expect(names).ToHaveTextAsync(["Pine Hollow", "Cedar Flats"]);

        await page.GetByRole(AriaRole.Link, new() { Name = "Name" }).ClickAsync();
        await Expect(names).ToHaveTextAsync(["Aspen Loop", "Cedar Flats"]);
        await page.GetByRole(AriaRole.Link, new() { Name = "Page 2" }).ClickAsync();

        await Expect(names).ToHaveTextAsync(["Eagle Ridge", "Pine Hollow"]);
        Assert.Contains("sort=name", page.Url);
        Assert.Contains("page=2", page.Url);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Row_ShouldBeClicked_WhenThePointerSlipsWhileClickingItsSelectedText(string browser)
    {
        var page = await OpenRowsAsync(browser);
        var cell = RowCell(page, "Ridge loop");
        await cell.DblClickAsync(new() { Position = OnTheText });
        Assert.NotEqual("", await page.EvaluateAsync<string>("() => document.getSelection().toString()"));
        var box = (await cell.BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(box.X + OnTheText.X, box.Y + OnTheText.Y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + OnTheText.X + 1, box.Y + OnTheText.Y);
        await page.Mouse.UpAsync();

        await Expect(Readout(page, "Row clicks")).ToHaveTextAsync("Ridge loop, Ridge loop");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Row_ShouldBeClicked_WhenItsSelectedTextIsClicked(string browser)
    {
        var page = await OpenRowsAsync(browser);
        var cell = RowCell(page, "Ridge loop");
        await cell.DblClickAsync(new() { Position = OnTheText });
        Assert.NotEqual("", await page.EvaluateAsync<string>("() => document.getSelection().toString()"));

        await cell.ClickAsync(new() { Position = OnTheText });

        await Expect(Readout(page, "Row clicks")).ToHaveTextAsync("Ridge loop, Ridge loop");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Row_ShouldBeClickedOnce_WhenDoubleClicked(string browser)
    {
        var page = await OpenRowsAsync(browser);

        await RowCell(page, "Ridge loop").DblClickAsync();
        await RowCell(page, "Creek path").ClickAsync();

        await Expect(Readout(page, "Row clicks")).ToHaveTextAsync("Ridge loop, Creek path");
        await Expect(Readout(page, "Selected")).ToHaveTextAsync("Ridge loop, Creek path");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Row_ShouldNotBeClicked_WhenADragSelectsItsText(string browser)
    {
        var page = await OpenRowsAsync(browser);
        var box = (await RowCell(page, "Ridge loop").BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(box.X + 12, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + 50, box.Y + box.Height / 2, new() { Steps = 4 });
        await page.Mouse.UpAsync();
        Assert.NotEqual("", await page.EvaluateAsync<string>("() => document.getSelection().toString()"));
        await RowCell(page, "Creek path").ClickAsync();

        await Expect(Readout(page, "Row clicks")).ToHaveTextAsync("Creek path");
        await Expect(Readout(page, "Selected")).ToHaveTextAsync("Creek path");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Focus_ShouldStayOnThePage_WhenAVirtualizedGridDropsTheFocusedRowAsItScrolls(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        await Expect(page.Locator("[data-part=grid]").First).ToHaveAttributeAsync("data-ready", "");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Many numbers" });
        var cell = grid.GetByRole(AriaRole.Gridcell, new() { Name = "1", Exact = true });
        await cell.FocusAsync();

        await ScrollBox(page, "Many numbers").EvaluateAsync("box => box.scrollTop = box.scrollHeight");

        await Expect(cell).ToHaveCountAsync(0);
        Assert.True(await page.EvaluateAsync<bool>("() => document.activeElement === document.body"));

        await ScrollBox(page, "Many numbers").EvaluateAsync("box => box.scrollTop = 0");
        await Expect(cell).ToHaveCountAsync(1);
        Assert.True(await page.EvaluateAsync<bool>("() => document.activeElement === document.body"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Focus_ShouldStayOnThePage_WhenARowIsRemovedAfterFocusLeftTheGrid(string browser)
    {
        var page = await OpenRowsAsync(browser);
        await RowCell(page, "Ridge loop").FocusAsync();
        await page.GetByText("Elsewhere").ClickAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Remove the first row" }).ClickAsync();

        await Expect(RowCell(page, "Ridge loop")).ToHaveCountAsync(0);
        Assert.True(await page.EvaluateAsync<bool>("() => document.activeElement === document.body"));
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

    private static ILocator ScrollBox(IPage page, string grid) => page.Locator("[data-part=scroll]")
        .Filter(new() { Has = page.Locator($"table[aria-label='{grid}']") });

    // The script measures the box on the frame after it attaches, which data-ready alone doesn't promise.
    private static async Task WaitForTheScrollStopAsync(IPage page, string grid)
    {
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = page.Locator($"table[aria-label='{grid}']") }))
            .ToHaveAttributeAsync("data-ready", "");
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");
    }
}
