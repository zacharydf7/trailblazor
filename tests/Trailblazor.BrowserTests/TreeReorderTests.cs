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
    public async Task Tree_ShouldMoveARowAfterTheBottomHalf_WhenOnlyBeforeAndAfterAreAllowed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = "Packing order" });
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        await tree.EvaluateAsync("tree => tree.scrollIntoView({ block: 'center' })");
        var from = (await tree.GetByRole(AriaRole.Treeitem, new() { Name = "Map" }).BoundingBoxAsync())!;
        var to = (await tree.GetByRole(AriaRole.Treeitem, new() { Name = "Tent" }).BoundingBoxAsync())!;

        // Below the middle but well above the bottom edge, which without Inside goes after the row.
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2 - 8, new() { Steps = 2 });
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height * 0.7f, new() { Steps = 10 });
        await page.Mouse.UpAsync();

        await Expect(tree.GetByRole(AriaRole.Treeitem)).ToHaveTextAsync(["Tent", "Map", "Stove", "Water"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldFileARowIntoAnotherFromItsEdgeAndSaySo_WhenOnlyInsideIsAllowed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");

        await page.Tb().Tree("Trail files").DragBeforeAsync(["Maps", "Lake map"], ["Permits"]);

        await Expect(page.GetByRole(AriaRole.Tree, new() { Name = "Trail files" }).GetByRole(AriaRole.Treeitem))
            .ToHaveTextAsync(["Maps", "Ridge map", "Permits", "Fishing permit", "Lake map"]);
        await Expect(page.Locator("[data-part=announcer] [role=status] p"))
            .ToHaveTextAsync("Lake map filed under Permits.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldMoveARowWithinABranch_WhenDraggedByPathsInsideIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");

        await page.Tb().Tree(Plan).Branch("Day 1").DragBeforeAsync(["Lake camp"], ["Pine Hollow"]);

        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Trailhead", "Lake camp", "Pine Hollow", "Day 2",
            "Eagle Ridge", "Summit Gate"]);
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
        await PlanReadyAsync(page);

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
        await AddAsync(page, "Add a stop to Day 2 at 1");

        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Lake camp", "Trailhead", "Pine Hollow", "Day 2",
            "Stop 9", "Stop 8", "Eagle Ridge", "Summit Gate"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldOpenAClosedRow_WhenADragRestsOnIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        await PlanReadyAsync(page);
        await Row(page, "Day 2").FocusAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Row(page, "Day 2")).ToHaveAttributeAsync("aria-expanded", "false");
        var from = (await Row(page, "Pine Hollow").BoundingBoxAsync())!;
        var to = (await Row(page, "Day 2").BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height / 2, new() { Steps = 10 });

        await Expect(Row(page, "Day 2")).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("Escape");
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldLeaveAClosedRowClosed_WhenADragOnlyPassesOverIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        await PlanReadyAsync(page);
        await Row(page, "Day 2").FocusAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Row(page, "Day 2")).ToHaveAttributeAsync("aria-expanded", "false");
        var from = (await Row(page, "Pine Hollow").BoundingBoxAsync())!;
        var to = (await Row(page, "Day 2").BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height / 2, new() { Steps = 10 });
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y - to.Height, new() { Steps = 4 });
        await page.WaitForTimeoutAsync(800);

        Assert.Equal("false", await Row(page, "Day 2").GetAttributeAsync("aria-expanded"));
        await page.Keyboard.PressAsync("Escape");
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldShowWhyAndMoveNothing_WhenARowIsDraggedIntoOneThatCantHoldIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        var tree = await PlanReadyAsync(page);
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
    public async Task Tree_ShouldShowWhyAMoveIsRefused_WhenItHadNoRowsAsItsScriptAttached(string browser)
    {
        var page = await OpenAsync(browser, "/tree-starts-empty");
        await Expect(page.Locator(".tb-tree")).ToHaveAttributeAsync("data-ready", "");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add stops" }).ClickAsync();
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = "Stops" });
        var from = (await tree.GetByRole(AriaRole.Treeitem, new() { Name = "Summit" }).BoundingBoxAsync())!;
        var to = (await tree.GetByRole(AriaRole.Treeitem, new() { Name = "Trailhead" }).BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height / 2, new() { Steps = 10 });

        await Expect(tree.Locator("[data-part=drop-reason]")).ToHaveTextAsync("These stops stay put.");
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldDropOnTheRowUnderAnInsertSlot_WhenTheSlotsTakeThePointerAsOnATouchScreen(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        // A touch screen keeps the slots taking the pointer; a rule in the library's own layer stands in for it here.
        await page.AddStyleTagAsync(new()
        {
            Content = "@layer tb.components { .tb-tree__insert { opacity: 1; clip-path: none; } }",
        });
        await PlanReadyAsync(page);
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
        await PlanReadyAsync(page);
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
        await PlanReadyAsync(page);
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
        var tree = await PlanReadyAsync(page);
        await tree.EvaluateAsync("tree => tree.dir = 'rtl'");
        var day = Row(page, "Day 1");
        await day.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(day).ToHaveAttributeAsync("aria-expanded", "false");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(day).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Row(page, "Trailhead")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(day).ToBeFocusedAsync();
        await Row(page, "Pine Hollow").FocusAsync();
        await page.Keyboard.PressAsync("Alt+Shift+ArrowRight");

        await Expect(page.Locator("[data-part=announcer] [role=status] p").Last)
            .ToHaveTextAsync("A stop belongs in a day.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldAddAStopWhereItsSlotSays_WhenTheSlotsButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");

        await AddAsync(page, "Add a stop to Day 1 at 2");

        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Trailhead", "Stop 8", "Pine Hollow", "Lake camp",
            "Day 2", "Eagle Ridge", "Summit Gate"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldSelectTheRowAndAddNothing_WhenTheRowsTextIsClickedAtItsLowerEdge(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        await PlanReadyAsync(page);
        var text = (await Row(page, "Trailhead").Locator(".tb-tree__text").BoundingBoxAsync())!;

        // The slot below Trailhead starts where its text does.
        await page.Mouse.MoveAsync(text.X + 2, text.Y + text.Height / 2);
        await page.Mouse.ClickAsync(text.X + 2, text.Y + text.Height - 1);

        await Expect(Row(page, "Trailhead")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Trailhead", "Pine Hollow", "Lake camp", "Day 2",
            "Eagle Ridge", "Summit Gate"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldShowAnInsertSlotOnlyOnTheRowsEdge_WhenThePointerIsOverTheRow(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view", new() { ReducedMotion = ReducedMotion.Reduce });
        await PlanReadyAsync(page);
        var row = (await Row(page, "Trailhead").BoundingBoxAsync())!;
        var button = page.GetByRole(AriaRole.Button, new() { Name = "Add a stop to Day 1 at 2" });
        var slot = page.Locator("[data-part=insert]").Filter(new() { Has = button });
        var box = (await button.BoundingBoxAsync())!;
        var middle = box.X + box.Width / 2;
        var buttonTop = box.Y + 2;

        await page.Mouse.MoveAsync(middle, row.Y + row.Height / 2);
        Assert.False(await page.EvaluateAsync<bool>(FormattableString.Invariant(
            $"() => !!document.elementFromPoint({middle}, {buttonTop}).closest('[data-part=insert]')")),
            "Over the row, the hidden button's top shouldn't take the pointer.");
        await Expect(slot).ToHaveCSSAsync("opacity", "0");
        await page.Mouse.MoveAsync(middle, row.Y + row.Height);

        await Expect(slot).ToHaveCSSAsync("opacity", "1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldHideAnInsertSlot_WhenThePointerLeavesAfterItsButtonWasClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view", new() { ReducedMotion = ReducedMotion.Reduce });
        Assert.SkipWhen(await page.EvaluateAsync<bool>("() => matchMedia('(any-pointer: coarse)').matches"),
            "This machine has a touch screen, so the focused row's slots show.");
        await PlanReadyAsync(page);

        await AddAsync(page, "Add a stop to Day 1 at 2");
        await page.Mouse.MoveAsync(1, 1);

        await WaitForFadesAsync(page);
        Assert.Empty(await page.Locator("[data-part=insert]").EvaluateAllAsync<string[]>(
            "slots => slots.filter(slot => getComputedStyle(slot).opacity !== '0').map(slot => slot.textContent)"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldShowAnInsertSlot_WhenItsButtonHasKeyboardFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view", new() { ReducedMotion = ReducedMotion.Reduce });
        await PlanReadyAsync(page);
        var button = page.GetByRole(AriaRole.Button, new() { Name = "Add a stop to Day 1 at 2" });

        // With no press on the page yet, a script's focus counts as keyboard focus.
        await button.FocusAsync();

        Assert.True(await button.EvaluateAsync<bool>("button => button.matches(':focus-visible')"));
        await Expect(page.Locator("[data-part=insert]").Filter(new() { Has = button })).ToHaveCSSAsync("opacity", "1");
    }

    [Fact]
    public async Task Tree_ShouldSelectTheRowAndAddNothing_WhenAHiddenSlotsEdgeIsTappedOnATouchScreen()
    {
        var page = await OpenAsync("chromium", "/examples/tree-view", new() { IsMobile = true, HasTouch = true });
        await PlanReadyAsync(page);
        var row = (await Row(page, "Trailhead").BoundingBoxAsync())!;
        var box = (await page.GetByRole(AriaRole.Button, new() { Name = "Add a stop to Day 1 at 2" })
            .BoundingBoxAsync())!;

        await page.Touchscreen.TapAsync(box.X + box.Width / 2, row.Y + row.Height - 2);

        await Expect(Row(page, "Trailhead")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Rows(page)).ToHaveTextAsync(["Day 1", "Trailhead", "Pine Hollow", "Lake camp", "Day 2",
            "Eagle Ridge", "Summit Gate"]);
    }

    private static Task AddAsync(IPage page, string slot) =>
        page.GetByRole(AriaRole.Button, new() { Name = slot }).ClickAsync();

    // The page holds every section, so the pointer has to bring this one into view to reach it.
    private static async Task<ILocator> PlanReadyAsync(IPage page)
    {
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = Plan });
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        await tree.ScrollIntoViewIfNeededAsync();
        return tree;
    }

    private static ILocator Rows(IPage page) =>
        page.GetByRole(AriaRole.Tree, new() { Name = Plan }).Locator("[role=treeitem] .tb-tree__text");

    private static ILocator Row(IPage page, string text) =>
        page.GetByRole(AriaRole.Tree, new() { Name = Plan }).Locator($"[role=treeitem][data-text='{text}']");
}
