using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class DragScrollTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/drag-scroll");
        await Expect(page.Locator(".tb-sortable:not([data-ready]), [data-part=grid]:not([data-ready])"))
            .ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Tree)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    private static ILocator Handle(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Move {name}", Exact = true });

    private static ILocator SortableBox(IPage page) => page.GetByTestId("sortable-box");

    private static Task<double> ScrollTopAsync(ILocator box) => box.EvaluateAsync<double>("box => box.scrollTop");

    // Starts dragging from the item, and rests the pointer just inside the bottom edge of the box.
    private static async Task HoldNearTheBottomAsync(IPage page, ILocator from, ILocator box)
    {
        var start = (await from.BoundingBoxAsync())!;
        var area = (await box.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(start.X + start.Width / 2, start.Y + start.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(start.X + start.Width / 2, area.Y + area.Height - 8, new() { Steps = 5 });
    }

    // Waits for the box to scroll with no further moves of the pointer.
    private static async Task ExpectScrollingAsync(IPage page, ILocator box)
    {
        var before = await ScrollTopAsync(box);
        await page.WaitForFunctionAsync("([box, before]) => box.scrollTop > before + 20",
            new object[] { await box.ElementHandleAsync(), before }, new() { Timeout = 3000 });
    }

    private static Task SettleAsync(IPage page) =>
        page.EvaluateAsync("() => new Promise(done => setTimeout(() => requestAnimationFrame(done), 150))");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sortable_ShouldKeepScrolling_WhenADragIsHeldStillNearTheBottom(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await HoldNearTheBottomAsync(page, Handle(page, "Rule 1"), SortableBox(page));

        await ExpectScrollingAsync(page, SortableBox(page));
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sortable_ShouldKeepScrollingUp_WhenADragIsHeldStillNearTheTop(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SortableBox(page).EvaluateAsync("box => box.scrollTop = box.scrollHeight");
        var from = Handle(page, "Rule 30");
        var start = (await from.BoundingBoxAsync())!;
        var area = (await SortableBox(page).BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(start.X + start.Width / 2, start.Y + start.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(start.X + start.Width / 2, area.Y + 8, new() { Steps = 5 });
        var scrolled = await ScrollTopAsync(SortableBox(page));

        await page.WaitForFunctionAsync("([box, before]) => box.scrollTop < before - 20",
            new object[] { await SortableBox(page).ElementHandleAsync(), scrolled }, new() { Timeout = 3000 });
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sortable_ShouldDropWhereTheScrollingBroughtIt_WhenReleasedAfterHoldingStill(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await HoldNearTheBottomAsync(page, Handle(page, "Rule 1"), SortableBox(page));
        await page.WaitForFunctionAsync("box => box.scrollTop + box.clientHeight >= box.scrollHeight",
            await SortableBox(page).ElementHandleAsync(), new() { Timeout = 15000 });
        await SettleAsync(page);

        await page.Mouse.UpAsync();

        // Its place among the thirty, well past the handful of rows in view when the drag began.
        await Expect(page.GetByTestId("moved-to")).ToHaveTextAsync(new Regex("^2[5-9]$"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sortable_ShouldDropWhereTheScrollingBroughtIt_WhenTheBoxScrollsSmoothly(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SortableBox(page).EvaluateAsync("box => box.style.scrollBehavior = 'smooth'");
        await HoldNearTheBottomAsync(page, Handle(page, "Rule 1"), SortableBox(page));
        await page.WaitForFunctionAsync("box => box.scrollTop + box.clientHeight >= box.scrollHeight",
            await SortableBox(page).ElementHandleAsync(), new() { Timeout = 15000 });
        await SettleAsync(page);

        await page.Mouse.UpAsync();

        await Expect(page.GetByTestId("moved-to")).ToHaveTextAsync(new Regex("^2[5-9]$"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sortable_ShouldCancelTheDrag_WhenTheWindowLosesFocus(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await HoldNearTheBottomAsync(page, Handle(page, "Rule 1"), SortableBox(page));
        await ExpectScrollingAsync(page, SortableBox(page));

        await page.EvaluateAsync("() => window.dispatchEvent(new Event('blur'))");
        await SettleAsync(page);
        var stopped = await ScrollTopAsync(SortableBox(page));
        await SettleAsync(page);

        Assert.Equal(stopped, await ScrollTopAsync(SortableBox(page)));
        await page.Mouse.UpAsync();
        await SettleAsync(page);
        await Expect(page.GetByTestId("moved-to")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sortable_ShouldScrollABox_WhenItStartsToOverflowDuringTheDrag(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SortableBox(page).EvaluateAsync("box => box.style.overflow = 'visible'");
        var from = (await Handle(page, "Rule 1").BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height, new() { Steps = 3 });
        await SortableBox(page).EvaluateAsync("box => box.style.overflow = 'auto'");
        var area = (await SortableBox(page).BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(from.X + from.Width / 2, area.Y + area.Height - 8, new() { Steps = 3 });

        await ExpectScrollingAsync(page, SortableBox(page));
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sortable_ShouldStopScrolling_WhenThePointerLeavesTheEdge(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await HoldNearTheBottomAsync(page, Handle(page, "Rule 1"), SortableBox(page));
        await ExpectScrollingAsync(page, SortableBox(page));
        var area = (await SortableBox(page).BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(area.X + area.Width / 2, area.Y + area.Height / 2, new() { Steps = 2 });
        await SettleAsync(page);
        var stopped = await ScrollTopAsync(SortableBox(page));
        await SettleAsync(page);

        Assert.Equal(stopped, await ScrollTopAsync(SortableBox(page)));
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sortable_ShouldStopScrolling_WhenTheDragEnds(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        await HoldNearTheBottomAsync(page, Handle(page, "Rule 1"), SortableBox(page));
        await ExpectScrollingAsync(page, SortableBox(page));

        await page.Mouse.UpAsync();
        await SettleAsync(page);
        var stopped = await ScrollTopAsync(SortableBox(page));
        await SettleAsync(page);

        Assert.Equal(stopped, await ScrollTopAsync(SortableBox(page)));
        Assert.Empty(errors);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DataGrid_ShouldKeepScrolling_WhenARowDragIsHeldStillNearTheBottom(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var box = page.Locator("[data-part=scroll]");

        await HoldNearTheBottomAsync(page, Handle(page, "Stop 1"), box);

        await ExpectScrollingAsync(page, box);
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldKeepScrolling_WhenAFingerIsHeldStillNearTheBottom(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var box = page.GetByTestId("tree-box");

        // A finger that holds still starts the drag with no move, so only where it pressed says where it is.
        await box.EvaluateAsync("""
            box => {
                const area = box.getBoundingClientRect();
                const x = area.left + area.width / 2;
                const y = area.bottom - 8;
                document.elementFromPoint(x, y).dispatchEvent(new PointerEvent('pointerdown', {
                    bubbles: true, pointerId: 7, pointerType: 'touch', isPrimary: true, clientX: x, clientY: y, button: 0 }));
            }
            """);

        await ExpectScrollingAsync(page, box);
        await page.EvaluateAsync("""
            () => document.dispatchEvent(new PointerEvent('pointerup', {
                bubbles: true, pointerId: 7, pointerType: 'touch', isPrimary: true }))
            """);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldKeepScrolling_WhenADragIsHeldStillNearTheBottom(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var box = page.GetByTestId("tree-box");
        var row = page.Locator("[role=treeitem][data-text='Day 1']");

        await HoldNearTheBottomAsync(page, row, box);

        await ExpectScrollingAsync(page, box);
        await page.Mouse.UpAsync();
    }
}
