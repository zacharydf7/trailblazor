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
    public async Task Grid_ShouldStopAtTheColumnsMinimum_WhenItsEdgeIsDraggedPastIt(string browser)
    {
        var page = await GridResizeTests.ReadyAsync(await OpenAsync(browser, "/examples/data-grid"));

        await page.Tb().Grid("Trail widths").ResizeColumnAsync("Name", 20);

        await Expect(GridResizeTests.Header(page, "Name")).ToHaveAttributeAsync("data-width", "160");
    }
}
