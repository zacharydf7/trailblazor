namespace Trailblazor.BrowserTests;

public class LayoutTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridItem_ShouldTakeTheFullWidth_WhenViewportIsBelowTheMediumBreakpoint(string browser)
    {
        var page = await OpenAtWidthAsync(browser, "/layout-grid", width: 600);

        var first = await BoxAsync(page, "half-first");
        var second = await BoxAsync(page, "half-second");

        Assert.Equal(first.X, second.X, tolerance: 1);
        Assert.True(second.Y > first.Y + first.Height, "The second item should sit below the first.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridItem_ShouldTakeItsMediumSpan_WhenViewportIsInTheMediumRange(string browser)
    {
        var page = await OpenAtWidthAsync(browser, "/layout-grid", width: 900);

        var grid = await BoxAsync(page, "grid");
        var first = await BoxAsync(page, "half-first");
        var second = await BoxAsync(page, "half-second");

        Assert.Equal(first.Y, second.Y, tolerance: 1);
        Assert.Equal(first.Width, second.Width, tolerance: 1);
        Assert.True(first.Width < grid.Width / 2, "Each half should be narrower than half the grid.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridItem_ShouldSpanItsOwnGrid_WhenNestedInAnItemWithAnotherSpan(string browser)
    {
        // 900px is in the medium range, where the parent item spans 6 columns.
        var page = await OpenAtWidthAsync(browser, "/layout-grid", width: 900);

        var nestedGrid = await BoxAsync(page, "nested-grid");
        var nestedItem = await BoxAsync(page, "nested-item");

        Assert.Equal(nestedGrid.Width, nestedItem.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridItem_ShouldEndAtTheLastColumn_WhenItStartsInColumnNineAndSpansFour(string browser)
    {
        var page = await OpenAtWidthAsync(browser, "/layout-grid", width: 1100);

        var grid = await BoxAsync(page, "grid");
        var end = await BoxAsync(page, "end");

        Assert.Equal(grid.X + grid.Width, end.X + end.Width, tolerance: 1);
        Assert.True(end.Width < grid.Width / 2, "A four-column item should be narrower than half the grid.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridItem_ShouldFillTheRestOfItsRow_WhenItGrowsBetweenItemsSizedToTheirContent(string browser)
    {
        var page = await OpenAtWidthAsync(browser, "/layout-grid", width: 1100);

        var grid = await BoxAsync(page, "toolbar");
        var label = await BoxAsync(page, "auto");
        var grow = await BoxAsync(page, "grow");
        var find = await page.GetByRole(AriaRole.Button, new() { Name = "Find" }).BoundingBoxAsync();

        Assert.Equal(grid.X, label.X, tolerance: 1);
        Assert.True(label.Width < grid.Width / 6, "The label should be only as wide as its text.");
        Assert.True(grow.X > label.X + label.Width, "The growing item should follow the label.");
        Assert.Equal(grid.X + grid.Width, find!.X + find.Width, tolerance: 1);
        Assert.Equal(label.Y, grow.Y, tolerance: grow.Height);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridItem_ShouldTakeItsOffsetFromTheMediumBreakpoint_WhenOnlyOffsetMdIsSet(string browser)
    {
        var narrow = await OpenAtWidthAsync(browser, "/layout-grid", width: 600);
        var wide = await OpenAtWidthAsync(browser, "/layout-grid", width: 900);

        var narrowGrid = await BoxAsync(narrow, "grid");
        var narrowItem = await BoxAsync(narrow, "centered");
        var grid = await BoxAsync(wide, "grid");
        var item = await BoxAsync(wide, "centered");

        Assert.Equal(narrowGrid.X, narrowItem.X, tolerance: 1);
        Assert.Equal(item.X - grid.X, grid.X + grid.Width - (item.X + item.Width), tolerance: 1);
        Assert.True(item.X > grid.X + 1, "From medium up the item should leave two columns before it.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridItem_ShouldTakeItsSpanAndOffset_WhenTheGridHasNoGap(string browser)
    {
        var page = await OpenAtWidthAsync(browser, "/zero-gap-grid", width: 1100);

        var grid = await BoxAsync(page, "grid");
        var first = await BoxAsync(page, "first");
        var second = await BoxAsync(page, "second");

        Assert.Equal(grid.Width / 2, first.Width, tolerance: 1);
        Assert.Equal(grid.Width / 4, second.Width, tolerance: 1);
        Assert.Equal(grid.X + grid.Width, second.X + second.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Stack_ShouldBeAColumnBelowTheBreakpointAndARowFromIt_WhenRowFromIsSet(string browser)
    {
        var narrow = await OpenAtWidthAsync(browser, "/layout-stack", width: 600);
        var wide = await OpenAtWidthAsync(browser, "/layout-stack", width: 900);

        var (narrowFirst, narrowLast) = await FirstAndLastLinkAsync(narrow);
        var (first, last) = await FirstAndLastLinkAsync(wide);

        Assert.True(narrowLast.Y > narrowFirst.Y + narrowFirst.Height, "Below medium the links should stack.");
        Assert.Equal(first.Y, last.Y, tolerance: 1);
        Assert.True(last.X > first.X + first.Width, "From medium up the links should sit in a row.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Layouts_ShouldBeListsOfItems_WhenRenderedAsListElements(string browser)
    {
        var page = await OpenAsync(browser, "/layout-grid");
        var stack = await OpenAsync(browser, "/layout-stack");

        await Expect(page.GetByRole(AriaRole.List, new() { Name = "Trails nearby" }).GetByRole(AriaRole.Listitem))
            .ToHaveCountAsync(3);
        await Expect(page.Locator("ul.tb-grid > li.tb-grid-item")).ToHaveCountAsync(3);
        await Expect(page.GetByRole(AriaRole.Article)).ToHaveCountAsync(3);
        await Expect(stack.GetByRole(AriaRole.Navigation, new() { Name = "Trail sections" })
            .GetByRole(AriaRole.List).GetByRole(AriaRole.Listitem)).ToHaveCountAsync(3);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Spacer_ShouldPushTheNextItemToTheEnd_WhenInARowStack(string browser)
    {
        var page = await OpenAtWidthAsync(browser, "/layout-spacer", width: 1100);

        var toolbar = await BoxAsync(page, "toolbar");
        var end = await BoxAsync(page, "toolbar-end");

        Assert.Equal(toolbar.X + toolbar.Width, end.X + end.Width, tolerance: 1);
    }

    private Task<IPage> OpenAtWidthAsync(string browser, string path, int width) =>
        OpenAsync(browser, path, new() { ViewportSize = new() { Width = width, Height = 900 } });

    private static async Task<(LocatorBoundingBoxResult First, LocatorBoundingBoxResult Last)> FirstAndLastLinkAsync(
        IPage page)
    {
        var links = page.GetByTestId("sections").GetByRole(AriaRole.Link);
        return ((await links.First.BoundingBoxAsync())!, (await links.Last.BoundingBoxAsync())!);
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(IPage page, string testId) =>
        await page.GetByTestId(testId).BoundingBoxAsync()
        ?? throw new InvalidOperationException($"{testId} is not visible.");
}
