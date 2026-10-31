using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TreeReorderTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Plan = "Trip plan";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldMoveARowBeforeAnotherAndSaySo_WhenItIsDraggedOntoTheOthersTopEdge(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");

        await page.Tb().Tree(Plan).DragBeforeAsync(["Day 1", "Lake camp"], ["Day 1", "Pine Hollow"]);

        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Trailhead", "Lake camp", "Pine Hollow", "Day 2",
            "Eagle Ridge", "Summit Gate"]);
        await Expect(page.Locator("[data-part=announcer] [role=status] p"))
            .ToHaveTextAsync("Lake camp moved before Pine Hollow.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldMoveARowIntoAnother_WhenItIsDraggedOntoTheOthersMiddle(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");

        await page.Tb().Tree(Plan).DragIntoAsync(["Day 1", "Trailhead"], ["Day 2"]);

        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Pine Hollow", "Lake camp", "Day 2", "Eagle Ridge",
            "Summit Gate", "Trailhead"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldMoveARowAboveTheFirstChild_WhenItIsDraggedOntoAnOpenRowsBottomEdge(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");

        await page.Tb().Tree(Plan).DragAfterAsync(["Day 1", "Lake camp"], ["Day 2"]);

        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Trailhead", "Pine Hollow", "Day 2", "Lake camp",
            "Eagle Ridge", "Summit Gate"]);
        await Expect(page.Locator("[data-part=announcer] [role=status] p"))
            .ToHaveTextAsync("Lake camp moved before Eagle Ridge.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldLetTheNextKeyAndClickThrough_WhenATouchDragEndsWithoutAClick(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        await Expect(page.GetByRole(AriaRole.Tree, new() { Name = Plan })).ToHaveAttributeAsync("data-ready", "");

        // A finger holds still on Lake camp, then moves onto Trailhead's top edge and lifts; a touch sends no click.
        await Row(page, "Lake camp").EvaluateAsync("""
            async (row, target) => {
                const fire = (type, x, y) => document.elementFromPoint(x, y).dispatchEvent(new PointerEvent(type, {
                    bubbles: true, pointerId: 7, pointerType: 'touch', isPrimary: true, clientX: x, clientY: y,
                    button: type === 'pointermove' ? -1 : 0 }));
                const from = row.getBoundingClientRect();
                const to = target.getBoundingClientRect();
                const x = from.left + from.width / 2;
                fire('pointerdown', x, from.top + from.height / 2);
                await new Promise(resolve => setTimeout(resolve, 400));
                fire('pointermove', x, to.top + 2);
                await new Promise(resolve => setTimeout(resolve, 100));
                fire('pointerup', x, to.top + 2);
            }
            """, await Row(page, "Trailhead").ElementHandleAsync());
        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Lake camp", "Trailhead", "Pine Hollow", "Day 2",
            "Eagle Ridge", "Summit Gate"]);
        await page.GetByRole(AriaRole.Button, new() { Name = "Add a stop to Day 2 at 1" }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Lake camp", "Trailhead", "Pine Hollow", "Day 2",
            "Stop 8", "Eagle Ridge", "Summit Gate"]);
        await AddAsync(page, "Day 2", "Add a stop to Day 2 at 1");

        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Lake camp", "Trailhead", "Pine Hollow", "Day 2",
            "Stop 9", "Stop 8", "Eagle Ridge", "Summit Gate"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldShowWhyAndMoveNothing_WhenARowIsDraggedIntoOneThatCantHoldIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = Plan });
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        var from = (await Row(page, "Pine Hollow").BoundingBoxAsync())!;
        var to = (await Row(page, "Trailhead").BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height / 2, new() { Steps = 10 });

        await Expect(Row(page, "Trailhead")).ToHaveAttributeAsync("data-drop-refused", "");
        await Expect(tree.Locator("[data-part=drop-reason]")).ToHaveTextAsync("A stop can't hold stops.");
        await page.Mouse.UpAsync();
        await Expect(tree.Locator("[data-drop]")).ToHaveCountAsync(0);
        await Expect(page.Locator("[data-part=announcer] [role=status] p"))
            .ToHaveTextAsync("A stop can't hold stops.");
        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Trailhead", "Pine Hollow", "Lake camp", "Day 2",
            "Eagle Ridge", "Summit Gate"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldDropOnTheRowUnderAnInsertSlot_WhenTheSlotsTakeThePointerAsOnATouchScreen(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        // A touch screen keeps the slots taking the pointer; a rule in the library's own layer stands in for it here.
        await page.AddStyleTagAsync(new()
        {
            Content = "@layer tb.components { .tb-tree__insert { opacity: 1; pointer-events: auto; } }",
        });
        await Expect(page.GetByRole(AriaRole.Tree, new() { Name = Plan })).ToHaveAttributeAsync("data-ready", "");
        await Row(page, "Pine Hollow").EvaluateAsync("row => row.scrollIntoView({ block: 'center' })");
        var slot = (await page.GetByRole(AriaRole.Button, new() { Name = "Add a stop to Day 1 at 2" })
            .BoundingBoxAsync())!;
        var from = (await Row(page, "Lake camp").BoundingBoxAsync())!;
        var to = (await Row(page, "Pine Hollow").BoundingBoxAsync())!;

        // The slot below Trailhead lies over Pine Hollow's top edge, where the drag ends.
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(slot.X + slot.Width / 2, to.Y + 3, new() { Steps = 10 });
        await page.Mouse.UpAsync();

        await Expect(page.Locator("[data-part=announcer] [role=status] p").Last)
            .ToHaveTextAsync("Lake camp moved before Pine Hollow.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldDropNothing_WhenEscapeIsPressedDuringADrag(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        await Expect(page.GetByRole(AriaRole.Tree, new() { Name = Plan })).ToHaveAttributeAsync("data-ready", "");
        var from = (await Row(page, "Lake camp").BoundingBoxAsync())!;
        var to = (await Row(page, "Trailhead").BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + 2, new() { Steps = 10 });
        await Expect(Row(page, "Trailhead")).ToHaveAttributeAsync("data-drop", "before");
        await page.Keyboard.PressAsync("Escape");
        await page.Mouse.UpAsync();
        await Expect(page.Locator("[data-drop], [data-dragging]")).ToHaveCountAsync(0);
        // A later move the server applies after it shows the cancelled drag never dropped.
        await page.Tb().Tree(Plan).DragIntoAsync(["Day 1", "Trailhead"], ["Day 2"]);

        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Pine Hollow", "Lake camp", "Day 2", "Eagle Ridge",
            "Summit Gate", "Trailhead"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldMoveTheFocusedRowAndKeepFocusOnIt_WhenAltShiftAndAnArrowArePressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        var pine = Row(page, "Pine Hollow");
        await Expect(page.GetByRole(AriaRole.Tree, new() { Name = Plan })).ToHaveAttributeAsync("data-ready", "");
        await pine.FocusAsync();

        await page.Keyboard.PressAsync("Alt+Shift+ArrowDown");
        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Trailhead", "Lake camp", "Pine Hollow", "Day 2",
            "Eagle Ridge", "Summit Gate"]);
        await Expect(pine).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Alt+Shift+ArrowLeft");

        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Trailhead", "Lake camp", "Pine Hollow", "Day 2",
            "Eagle Ridge", "Summit Gate"]);
        await Expect(page.Locator("[data-part=announcer] [role=status] p").Last)
            .ToHaveTextAsync("A stop belongs in a day.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldMirrorTheArrowKeys_WhenItIsLaidOutRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = Plan });
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        await tree.EvaluateAsync("tree => tree.dir = 'rtl'");
        var day = Row(page, "Day 1");
        await day.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(day).ToHaveAttributeAsync("aria-expanded", "false");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(day).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Row(page, "Trailhead")).ToBeFocusedAsync();
        await Row(page, "Pine Hollow").FocusAsync();
        await page.Keyboard.PressAsync("Alt+Shift+ArrowRight");

        await Expect(page.Locator("[data-part=announcer] [role=status] p").Last)
            .ToHaveTextAsync("A stop belongs in a day.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldAddAStopWhereItsSlotSays_WhenTheSlotsButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");

        await AddAsync(page, "Trailhead", "Add a stop to Day 1 at 2");

        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Trailhead", "Stop 8", "Pine Hollow", "Lake camp",
            "Day 2", "Eagle Ridge", "Summit Gate"]);
    }

    // A slot takes the pointer while its row is hovered, so the pointer comes to its button from the row.
    private static async Task AddAsync(IPage page, string row, string slot)
    {
        await Row(page, row).HoverAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = slot })
            .ClickAsync(new() { Position = new() { X = 4, Y = 4 } });
    }

    private static ILocator Rows(IPage page) =>
        page.GetByRole(AriaRole.Tree, new() { Name = Plan }).Locator("[role=treeitem] .tb-tree__text");

    private static ILocator Row(IPage page, string text) =>
        page.GetByRole(AriaRole.Tree, new() { Name = Plan }).Locator($"[role=treeitem][data-text='{text}']");
}
