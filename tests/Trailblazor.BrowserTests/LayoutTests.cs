namespace Trailblazor.BrowserTests;

public class LayoutTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridItem_ShouldTakeTheFullWidth_WhenViewportIsBelowTheMediumBreakpoint(string browser)
    {
        var page = await OpenAtWidthAsync(browser, "/examples/grid", width: 600);

        var first = await BoxAsync(page, "half-first");
        var second = await BoxAsync(page, "half-second");

        Assert.Equal(first.X, second.X, tolerance: 1);
        Assert.True(second.Y > first.Y + first.Height, "The second item should sit below the first.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridItem_ShouldTakeItsMediumSpan_WhenViewportIsInTheMediumRange(string browser)
    {
        var page = await OpenAtWidthAsync(browser, "/examples/grid", width: 900);

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
        var page = await OpenAtWidthAsync(browser, "/examples/grid", width: 900);

        var nestedGrid = await BoxAsync(page, "nested-grid");
        var nestedItem = await BoxAsync(page, "nested-item");

        Assert.Equal(nestedGrid.Width, nestedItem.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridItem_ShouldEndAtTheLastColumn_WhenItStartsInColumnNineAndSpansFour(string browser)
    {
        var page = await OpenAtWidthAsync(browser, "/examples/grid", width: 1100);

        var grid = await BoxAsync(page, "grid");
        var end = await BoxAsync(page, "end");

        Assert.Equal(grid.X + grid.Width, end.X + end.Width, tolerance: 1);
        Assert.True(end.Width < grid.Width / 2, "A four-column item should be narrower than half the grid.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Spacer_ShouldPushTheNextItemToTheEnd_WhenInARowStack(string browser)
    {
        var page = await OpenAtWidthAsync(browser, "/examples/spacer", width: 1100);

        var toolbar = await BoxAsync(page, "toolbar");
        var end = await BoxAsync(page, "toolbar-end");

        Assert.Equal(toolbar.X + toolbar.Width, end.X + end.Width, tolerance: 1);
    }

    private Task<IPage> OpenAtWidthAsync(string browser, string path, int width) =>
        OpenAsync(browser, path, new() { ViewportSize = new() { Width = width, Height = 900 } });

    private static async Task<LocatorBoundingBoxResult> BoxAsync(IPage page, string testId) =>
        await page.GetByTestId(testId).BoundingBoxAsync()
        ?? throw new InvalidOperationException($"{testId} is not visible.");
}
