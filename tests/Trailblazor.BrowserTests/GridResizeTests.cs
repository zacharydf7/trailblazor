using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridResizeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<IPage> OpenResizeAsync(string browser) =>
        await ReadyAsync(await OpenAsync(browser, "/examples/data-grid"));

    internal static async Task<IPage> ReadyAsync(IPage page)
    {
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = Table(page) }))
            .ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    internal static ILocator Table(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Trail widths" });

    internal static ILocator Header(IPage page, string column) => Table(page).Locator($"th[data-column={column}]");

    internal static Task<double> WidthAsync(ILocator header) =>
        header.EvaluateAsync<double>("th => Math.round(th.getBoundingClientRect().width)");

    private static async Task ChooseAsync(IPage page, string column, string item)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = $"Options for {column}", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = item }).ClickAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ColumnMenu_ShouldWidenTheColumnAndKeepTheOthers_WhenMakeWiderIsChosen(string browser)
    {
        var page = await OpenResizeAsync(browser);
        var region = Header(page, "Region");
        var name = Header(page, "Name");
        var (before, nameBefore) = (await WidthAsync(region), await WidthAsync(name));

        await ChooseAsync(page, "Region", "Make Region wider");

        await Expect(page.GetByLabel("Column widths")).ToContainTextAsync("Region");
        Assert.InRange(await WidthAsync(region), before + 30, before + 34);
        Assert.InRange(await WidthAsync(name), nameBefore - 1, nameBefore + 1);
        await Expect(Table(page)).ToHaveClassAsync(new Regex("tb-data-grid__table--fixed"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ColumnMenu_ShouldMoveTheColumnAndKeepFocusOnItsMenu_WhenMoveLaterIsChosenWithTheKeys(
        string browser)
    {
        var page = await OpenResizeAsync(browser);
        var menu = page.GetByRole(AriaRole.Button, new() { Name = "Options for Region", Exact = true });
        await menu.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Move Region earlier" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByLabel("Column order")).ToHaveTextAsync("Name, LengthKm, Region, Surface, Notes");
        var headers = Table(page).Locator("thead th[data-column]");
        Assert.Equal(["Name", "LengthKm", "Region", "Surface", "Notes"],
            await headers.EvaluateAllAsync<string[]>("cells => cells.map(cell => cell.dataset.column)"));
        await Expect(menu).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ColumnMenu_ShouldHideTheColumnAndFocusTheColumnsMenu_WhenHideIsChosenWithTheKeys(string browser)
    {
        var page = await OpenResizeAsync(browser);
        await HideRegionWithTheKeysAsync(page);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ColumnMenu_ShouldFocusTheColumnsMenuAfterAHide_WhenItRunsInWebAssembly(string browser)
    {
        var page = await ReadyAsync(await OpenDocsAsync(browser, "components/data-grid"));
        await HideRegionWithTheKeysAsync(page);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chooser_ShouldKeepTheWidthsFixed_WhenAColumnHiddenBeforeTheFirstResizeIsShown(string browser)
    {
        var page = await OpenResizeAsync(browser);
        await page.Tb().Menu("Columns").ChooseAsync("Region");
        await Expect(Header(page, "Region")).ToHaveCountAsync(0);
        await ChooseAsync(page, "Name", "Make Name wider");
        await Expect(Table(page)).ToHaveClassAsync(new Regex("tb-data-grid__table--fixed"));

        await page.Tb().Menu("Columns").ChooseAsync("Region");

        await Expect(page.GetByLabel("Column widths")).ToContainTextAsync("Region");
        await Expect(Table(page)).ToHaveClassAsync(new Regex("tb-data-grid__table--fixed"));
    }

    private static async Task HideRegionWithTheKeysAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Options for Region", Exact = true }).FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Move Region earlier" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Hide Region" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByLabel("Hidden columns")).ToHaveTextAsync("Region");
        await Expect(Header(page, "Region")).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Columns", Exact = true })).ToBeFocusedAsync();
    }

    // WebAssembly renders before the menu's own focus return runs, unlike a server circuit.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ColumnMenu_ShouldKeepFocusOnTheMovedColumnsMenu_WhenItRunsInWebAssembly(string browser)
    {
        var page = await ReadyAsync(await OpenDocsAsync(browser, "components/data-grid"));
        var menu = page.GetByRole(AriaRole.Button, new() { Name = "Options for Region", Exact = true });
        await menu.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Move Region earlier" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByLabel("Column order")).ToHaveTextAsync("Name, LengthKm, Region, Surface, Notes");
        await Expect(menu).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ColumnMenu_ShouldStopAtTheColumnsMinimum_WhenMakeNarrowerIsChosenAgain(string browser)
    {
        var page = await OpenResizeAsync(browser);
        var output = page.GetByLabel("Column widths");

        for (var step = 0; step < 6; step++)
        {
            await ChooseAsync(page, "Name", "Make Name narrower");
        }

        await Expect(output).ToContainTextAsync("Name 160px");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ColumnMenu_ShouldGiveTheColumnsBackTheirWidths_WhenResetIsChosen(string browser)
    {
        var page = await OpenResizeAsync(browser);
        var region = Header(page, "Region");
        var before = await WidthAsync(region);
        await ChooseAsync(page, "Region", "Make Region wider");
        await Expect(page.GetByLabel("Column widths")).ToContainTextAsync("Region");

        await ChooseAsync(page, "Region", "Reset column widths");

        await Expect(page.GetByLabel("Column widths")).ToHaveTextAsync("Columns at their starting widths.");
        await Expect(Table(page)).Not.ToHaveClassAsync(new Regex("tb-data-grid__table--fixed"));
        Assert.InRange(await WidthAsync(region), before - 1, before + 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ColumnMenu_ShouldWidenTheColumn_WhenReachedAndChosenWithTheKeys(string browser)
    {
        var page = await OpenResizeAsync(browser);
        var region = Header(page, "Region");
        var before = await WidthAsync(region);
        await page.GetByRole(AriaRole.Button, new() { Name = "Options for Region", Exact = true }).FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Move Region earlier" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Make Region wider" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByLabel("Column widths")).ToContainTextAsync("Region");
        Assert.InRange(await WidthAsync(region), before + 30, before + 34);
    }
}

// Dragging uses pointer capture, which Firefox can leak into parallel pages, so it runs with the other drag tests.
[Collection(nameof(PointerDrags))]
public class GridResizeDragTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldResizeTheColumn_WhenItsHeadersEdgeIsDragged(string browser)
    {
        var page = await GridResizeTests.ReadyAsync(await OpenAsync(browser, "/examples/data-grid"));
        var name = GridResizeTests.Header(page, "Name");
        var region = GridResizeTests.Header(page, "Region");
        var (before, regionBefore) = (await GridResizeTests.WidthAsync(name), await GridResizeTests.WidthAsync(region));
        await name.ScrollIntoViewIfNeededAsync();
        var edge = (await name.Locator("[data-part=resize]").BoundingBoxAsync())!;
        var (x, y) = (edge.X + edge.Width / 2, edge.Y + edge.Height / 2);

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + 60, y, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        await Expect(page.GetByLabel("Column widths")).ToContainTextAsync("Name");
        Assert.InRange(await GridResizeTests.WidthAsync(name), before + 58, before + 62);
        Assert.InRange(await GridResizeTests.WidthAsync(region), regionBefore - 1, regionBefore + 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ResizeColumnAsync_ShouldDragTheEdgeAndWaitForTheWidth_WhenCalled(string browser)
    {
        var page = await GridResizeTests.ReadyAsync(await OpenAsync(browser, "/examples/data-grid"));
        var region = GridResizeTests.Header(page, "Region");
        var before = await GridResizeTests.WidthAsync(region);

        await page.Tb().Grid("Trail widths").ResizeColumnAsync("Region", before + 40);

        Assert.InRange(double.Parse((await region.GetAttributeAsync("data-width"))!), before + 38, before + 42);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ResizeColumnAsync_ShouldSayWhy_WhenTheColumnOptsOut(string browser)
    {
        var page = await GridResizeTests.ReadyAsync(await OpenAsync(browser, "/examples/data-grid"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Grid("Trail widths").ResizeColumnAsync("Notes", 200));

        Assert.Contains("leave the column's Resizable on", error.Message);
    }

    // Length (km) has a title, and Surface a custom header; both have filter menus.
    [Theory]
    [MemberData(nameof(FilterableColumns))]
    public async Task ColumnMenu_ShouldStayInReach_WhenAFilterableColumnIsAtItsMinimum(string browser, string key,
        string heading)
    {
        var page = await GridResizeTests.ReadyAsync(await OpenAsync(browser, "/examples/data-grid"));
        var header = GridResizeTests.Header(page, key);
        await page.Tb().Grid("Trail widths").ResizeColumnAsync(heading, 20);
        await Expect(header).ToHaveAttributeAsync("data-width", "128");
        var menu = page.GetByRole(AriaRole.Button, new() { Name = $"Options for {heading}", Exact = true });
        Assert.True(await header.EvaluateAsync<bool>("(header, button) => button.getBoundingClientRect().right"
            + " <= header.getBoundingClientRect().right", await menu.ElementHandleAsync()));

        await menu.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = $"Make {heading} wider" }).ClickAsync();

        await Expect(header).ToHaveAttributeAsync("data-width", "160");
    }

    public static TheoryData<string, string, string> FilterableColumns()
    {
        var columns = new TheoryData<string, string, string>();
        foreach (var browser in AllBrowsers.Names)
        {
            columns.Add(browser, "LengthKm", "Length (km)");
            columns.Add(browser, "Surface", "Surface");
        }

        return columns;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldMoveTheColumn_WhenItsHeaderIsDraggedPastAnother(string browser)
    {
        var page = await GridResizeTests.ReadyAsync(await OpenAsync(browser, "/examples/data-grid"));
        var name = GridResizeTests.Header(page, "Name");
        var region = GridResizeTests.Header(page, "Region");
        await name.ScrollIntoViewIfNeededAsync();
        var from = (await name.BoundingBoxAsync())!;
        var past = (await region.BoundingBoxAsync())!;
        var y = from.Y + from.Height / 2;

        await page.Mouse.MoveAsync(from.X + 12, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(past.X + past.Width * 0.75f, y, new() { Steps = 8 });
        await page.Mouse.UpAsync();

        await Expect(page.GetByLabel("Column order")).ToHaveTextAsync("Region, Name, LengthKm, Surface, Notes");
        Assert.Equal(["Region", "Name"], (await page.Tb().Grid("Trail widths").ColumnsAsync()).Take(2));
    }

    // Region is sortable and Notes is pinned at the end, so the drag must neither sort Region nor cross into the pinned
    // side.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldMoveWithoutSortingAndStayOffThePinnedSide_WhenASortableHeaderIsDragged(string browser)
    {
        var page = await GridResizeTests.ReadyAsync(await OpenAsync(browser, "/examples/data-grid"));
        var region = GridResizeTests.Header(page, "Region");
        var notes = GridResizeTests.Header(page, "Notes");
        await region.ScrollIntoViewIfNeededAsync();
        var from = (await region.Locator("[data-part=sort]").BoundingBoxAsync())!;
        var past = (await notes.BoundingBoxAsync())!;
        var y = from.Y + from.Height / 2;

        await page.Mouse.MoveAsync(from.X + 8, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(past.X + past.Width * 0.75f, y, new() { Steps = 8 });
        await page.Mouse.UpAsync();

        await Expect(page.GetByLabel("Column order")).ToHaveTextAsync("Name, LengthKm, Surface, Region, Notes");
        await Expect(region).Not.ToHaveAttributeAsync("aria-sort", new System.Text.RegularExpressions.Regex("."));
        await region.Locator("[data-part=sort]").ClickAsync();
        await Expect(region).ToHaveAttributeAsync("aria-sort", "ascending");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldLeaveTheColumnsWhereTheyAre_WhenEscapeIsPressedDuringAHeaderDrag(string browser)
    {
        var page = await GridResizeTests.ReadyAsync(await OpenAsync(browser, "/examples/data-grid"));
        var name = GridResizeTests.Header(page, "Name");
        await name.ScrollIntoViewIfNeededAsync();
        var from = (await name.BoundingBoxAsync())!;
        var past = (await GridResizeTests.Header(page, "Region").BoundingBoxAsync())!;
        var y = from.Y + from.Height / 2;

        await page.Mouse.MoveAsync(from.X + 12, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(past.X + past.Width * 0.75f, y, new() { Steps = 8 });
        await Expect(name).ToHaveAttributeAsync("data-moving", "");
        await page.Keyboard.PressAsync("Escape");
        await page.Mouse.UpAsync();

        await Expect(name).Not.ToHaveAttributeAsync("data-moving", "");
        await Expect(page.GetByLabel("Column order")).ToHaveTextAsync("Columns in their starting order.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldStopAtTheColumnsMinimum_WhenItsEdgeIsDraggedPastIt(string browser)
    {
        var page = await GridResizeTests.ReadyAsync(await OpenAsync(browser, "/examples/data-grid"));

        await page.Tb().Grid("Trail widths").ResizeColumnAsync("Name", 20);

        await Expect(GridResizeTests.Header(page, "Name")).ToHaveAttributeAsync("data-width", "160");
    }
}
