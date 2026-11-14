using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

// Under load, Firefox can hand one page's pointer moves to an element holding pointer capture in another page of the
// same browser, so tests that drag with the mouse run on their own.
[CollectionDefinition(nameof(PointerDrags), DisableParallelization = true)]
public sealed class PointerDrags;

[Collection(nameof(PointerDrags))]
public class DialogMoveAndResizeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // One press of an arrow key on the move button or the resize grip, in pixels.
    private const double KeyStep = 16;

    // A positioned dialog's distance from the window edge, --tb-space-4.
    private const double EdgeGap = 16;

    // The width of a large dialog, 48rem.
    private const double LargeWidth = 768;

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldFollowThePointer_WhenItsHeaderIsDragged(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var before = await DialogTests.BoxAsync(page, "Movable");

        await DragAsync(page, Movable(page).GetByRole(AriaRole.Heading), 80, 60);

        var after = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([before.X + 80, before.Y + 60], [after.X, after.Y]);
        Assert.Equal(before.Width, after.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStayPut_WhenADragStartsOnAHeaderButtonOtherThanMove(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var before = await DialogTests.BoxAsync(page, "Movable");

        await DragAsync(page, Movable(page).GetByRole(AriaRole.Button, new() { Name = "Close" }), 80, 60);

        var after = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([before.X, before.Y], [after.X, after.Y]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldFollowThePointer_WhenItBecomesDraggableAfterOpening(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Still" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Still" });
        await Expect(dialog).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Let it move" }).ClickAsync();

        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Move" })).ToBeVisibleAsync();
        var before = await DialogTests.BoxAsync(page, "Still");
        await DragAsync(page, dialog.GetByRole(AriaRole.Heading), 80, 60);
        var after = await DialogTests.BoxAsync(page, "Still");
        AssertNear([before.X + 80, before.Y + 60], [after.X, after.Y]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStayPut_WhenADragStartsOnItsHeaderActions(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Annotated" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Annotated", Exact = true });
        await Expect(dialog).ToBeVisibleAsync();
        var before = await DialogTests.BoxAsync(page, "Annotated");

        await DragAsync(page, dialog.GetByTestId("draft"), 80, 60);

        var after = await DialogTests.BoxAsync(page, "Annotated");
        AssertNear([before.X, before.Y], [after.X, after.Y]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldMoveAStepPerArrowKey_WhenItsMoveButtonHasFocus(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var move = Movable(page).GetByRole(AriaRole.Button, new() { Name = "Move" });
        var before = await DialogTests.BoxAsync(page, "Movable");
        await move.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowUp");

        var after = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([before.X + 2 * KeyStep, before.Y - KeyStep], [after.X, after.Y]);
        await Expect(move).ToBeFocusedAsync();
        await Expect(move).ToHaveAccessibleDescriptionAsync("Arrow keys move the dialog.");
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Move" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldGoToThePlaceChosen_WhenOneIsPickedFromTheMoveMenuAfterADrag(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var dialog = Movable(page);
        await DragAsync(page, dialog.GetByRole(AriaRole.Heading), -100, 50);

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Move" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Top end", Exact = true }).ClickAsync();

        await Expect(dialog).ToHaveClassAsync(new Regex("tb-dialog--position-topend"));
        var (width, _) = await DialogTests.ViewportAsync(page);
        var box = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([width - EdgeGap, EdgeGap], [box.X + box.Width, box.Y]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldGoBackToItsPlace_WhenThePlaceItAlreadyHasIsPickedAfterADrag(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var dialog = Movable(page);
        var before = await DialogTests.BoxAsync(page, "Movable");
        await DragAsync(page, dialog.GetByRole(AriaRole.Heading), -100, 50);

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Move" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Center", Exact = true }).ClickAsync();

        await page.WaitForFunctionAsync("() => !document.querySelector('dialog[open]').hasAttribute('data-placed')");
        var after = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([before.X, before.Y], [after.X, after.Y]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStayPut_WhenADragStartsInsideTheOpenMoveMenu(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var dialog = Movable(page);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Move" }).ClickAsync();
        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Move" });
        await Expect(menu).ToBeVisibleAsync();
        var before = await DialogTests.BoxAsync(page, "Movable");
        var list = (await menu.BoundingBoxAsync())!;

        // The middle of the list's top padding, which no item covers and the rounded corners don't cut off.
        var (x, y) = (list.X + list.Width / 2, list.Y + 2);
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + 80, y, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        var after = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([before.X, before.Y], [after.X, after.Y]);
    }

    // Only Chromium can drive touch from Playwright. A touch drag, unlike a mouse drag, ends without a click, so
    // nothing may be left waiting to swallow one; Enter's click on Move would lose its default, opening the menu.
    [Fact]
    public async Task MoveMenu_ShouldOpenWithEnter_WhenATouchDragOnTheMoveButtonEndedWithoutAClick()
    {
        var page = await OpenMovableAsync("chromium", options: new() { HasTouch = true });
        var dialog = Movable(page);
        var before = await DialogTests.BoxAsync(page, "Movable");
        var move = dialog.GetByRole(AriaRole.Button, new() { Name = "Move" });
        var start = (await move.BoundingBoxAsync())!;
        var (x, y) = (start.X + start.Width / 2, start.Y + start.Height / 2);
        var touch = await page.Context.NewCDPSessionAsync(page);
        await TouchAsync(touch, "touchStart", x, y);
        await TouchAsync(touch, "touchMove", x - 40, y + 20);
        await TouchAsync(touch, "touchMove", x - 80, y + 40);
        await TouchAsync(touch, "touchEnd", null, null);
        var after = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([before.X - 80, before.Y + 40], [after.X, after.Y]);

        await move.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Move" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldGrowToTheWindowsCorner_WhenDraggedToTheTopLeftAndResizedAllTheWay(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var dialog = Movable(page);
        var (width, height) = await DialogTests.ViewportAsync(page);
        var title = (await dialog.GetByRole(AriaRole.Heading).BoundingBoxAsync())!;
        await DragAsync(page, dialog.GetByRole(AriaRole.Heading), 1 - (title.X + title.Width / 2),
            1 - (title.Y + title.Height / 2));

        // The pointer stops just inside the window's bottom-right corner, past where the grip can follow.
        var grip = (await dialog.GetByRole(AriaRole.Button, new() { Name = "Resize" }).BoundingBoxAsync())!;
        await DragAsync(page, dialog.GetByRole(AriaRole.Button, new() { Name = "Resize" }),
            width - 1 - (grip.X + grip.Width / 2), height - 1 - (grip.Y + grip.Height / 2));

        var box = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([0, 0, width, height], [box.X, box.Y, box.X + box.Width, box.Y + box.Height]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStayInsideANarrowerWindow_WhenResizedAndThenPlacedFromTheMoveMenu(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var dialog = Movable(page);
        await DragAsync(page, dialog.GetByRole(AriaRole.Button, new() { Name = "Resize" }), 300, 0);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Move" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Bottom end", Exact = true }).ClickAsync();
        await Expect(dialog).ToHaveClassAsync(new Regex("tb-dialog--position-bottomend"));
        var sized = await DialogTests.BoxAsync(page, "Movable");

        await page.SetViewportSizeAsync((int)sized.Width - 100, 720);

        var (width, _) = await DialogTests.ViewportAsync(page);
        var box = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([EdgeGap, width - EdgeGap], [box.X, box.X + box.Width]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldTakeTheSizeChosen_WhenOneIsPickedFromTheResizeMenuAfterADrag(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var dialog = Movable(page);
        var grip = dialog.GetByRole(AriaRole.Button, new() { Name = "Resize" });
        await DragAsync(page, grip, 40, 30);

        await grip.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Large", Exact = true }).ClickAsync();

        await Expect(dialog).ToHaveClassAsync(new Regex("tb-dialog--large"));
        Assert.Equal(LargeWidth, (await DialogTests.BoxAsync(page, "Movable")).Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldOpenAtItsWidthAndTakeASizeChosenLater_WhenOpenedWithAWidth(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Wide", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Wide" });
        await Expect(dialog).ToBeVisibleAsync();
        Assert.Equal(600, (await DialogTests.BoxAsync(page, "Wide")).Width, tolerance: 1);

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Resize" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Large", Exact = true }).ClickAsync();

        await Expect(dialog).ToHaveClassAsync(new Regex("tb-dialog--large"));
        Assert.Equal(LargeWidth, (await DialogTests.BoxAsync(page, "Wide")).Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldTakeTheNewWidthInPlaceOfTheDraggedSize_WhenItsWidthIsSetWhileOpen(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Wide", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Wide" });
        await Expect(dialog).ToBeVisibleAsync();
        await DragAsync(page, dialog.GetByRole(AriaRole.Button, new() { Name = "Resize" }), 60, 0);
        Assert.Equal(660, (await DialogTests.BoxAsync(page, "Wide")).Width, tolerance: 1);

        await page.GetByRole(AriaRole.Button, new() { Name = "Narrow it" }).ClickAsync();

        await Expect(dialog).Not.ToHaveAttributeAsync("data-sized", new Regex(".*"));
        Assert.Equal(400, (await DialogTests.BoxAsync(page, "Wide")).Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldTakeTheSizeItsContentSets_WhenItWasOpenedWithAWidth(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = "Sized", Exact = true }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Sized" });
        await Expect(dialog).ToHaveClassAsync(new Regex("tb-dialog--large"));
        await Expect(dialog).Not.ToHaveAttributeAsync("style", new Regex("--tb-dialog-width"));
        Assert.Equal(LargeWidth, (await DialogTests.BoxAsync(page, "Sized")).Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MoveMenu_ShouldStayClosed_WhenTheDialogIsDraggedByTheMoveButton(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var before = await DialogTests.BoxAsync(page, "Movable");

        await DragAsync(page, Movable(page).GetByRole(AriaRole.Button, new() { Name = "Move" }), 80, 0);

        Assert.Equal(before.X + 80, (await DialogTests.BoxAsync(page, "Movable")).X, tolerance: 1);
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Move" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldMoveRight_WhenTheRightArrowIsPressedOnARightToLeftPage(string browser)
    {
        var page = await OpenMovableAsync(browser, rightToLeft: true);
        var before = await DialogTests.BoxAsync(page, "Movable");
        await Movable(page).GetByRole(AriaRole.Button, new() { Name = "Move" }).FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        Assert.Equal(before.X + KeyStep, (await DialogTests.BoxAsync(page, "Movable")).X, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStayInsideTheWindow_WhenDraggedPastItsEdges(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var (width, height) = await DialogTests.ViewportAsync(page);
        var title = Movable(page).GetByRole(AriaRole.Heading);

        // The pointer stops just inside the window, beyond where the dialog can follow it.
        var start = (await title.BoundingBoxAsync())!;
        await DragAsync(page, title, 1 - (start.X + start.Width / 2), 1 - (start.Y + start.Height / 2));
        var topLeft = await DialogTests.BoxAsync(page, "Movable");
        start = (await title.BoundingBoxAsync())!;
        await DragAsync(page, title, width - 1 - (start.X + start.Width / 2),
            height - 1 - (start.Y + start.Height / 2));
        var bottomRight = await DialogTests.BoxAsync(page, "Movable");

        AssertNear([0, 0], [topLeft.X, topLeft.Y]);
        AssertNear([width, height], [bottomRight.X + bottomRight.Width, bottomRight.Y + bottomRight.Height]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldGrowFromItsBottomEndCorner_WhenTheGripIsDragged(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var before = await DialogTests.BoxAsync(page, "Movable");

        await DragAsync(page, Movable(page).GetByRole(AriaRole.Button, new() { Name = "Resize" }), 50, 40);

        var after = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([before.X, before.Y], [after.X, after.Y]);
        AssertNear([before.Width + 50, before.Height + 40], [after.Width, after.Height]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldGrowToTheLeftAndKeepItsRightEdge_WhenTheGripIsDraggedOnARightToLeftPage(
        string browser)
    {
        var page = await OpenMovableAsync(browser, rightToLeft: true);
        var before = await DialogTests.BoxAsync(page, "Movable");

        await DragAsync(page, Movable(page).GetByRole(AriaRole.Button, new() { Name = "Resize" }), -50, 0);

        var after = await DialogTests.BoxAsync(page, "Movable");
        Assert.Equal(before.Width + 50, after.Width, tolerance: 1);
        Assert.Equal(before.X + before.Width, after.X + after.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldGrowAStepPerArrowKey_WhenTheGripHasFocus(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var grip = Movable(page).GetByRole(AriaRole.Button, new() { Name = "Resize" });
        var before = await DialogTests.BoxAsync(page, "Movable");
        await grip.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowDown");

        var after = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([before.Width + KeyStep, before.Height + KeyStep], [after.Width, after.Height]);
        await Expect(grip).ToHaveAccessibleDescriptionAsync("Arrow keys resize the dialog.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldComeBackToWhereItWasMovedAndSized_WhenRestoredFromMaximized(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var dialog = Movable(page);
        await DragAsync(page, dialog.GetByRole(AriaRole.Heading), -100, 50);
        await DragAsync(page, dialog.GetByRole(AriaRole.Button, new() { Name = "Resize" }), 40, 30);
        var placed = await DialogTests.BoxAsync(page, "Movable");

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Maximize" }).ClickAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Move" })).ToHaveCountAsync(0);
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Resize" })).ToBeHiddenAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Restore" }).ClickAsync();
        await Expect(dialog).ToHaveAttributeAsync("data-window-state", "normal");

        var restored = await DialogTests.BoxAsync(page, "Movable");
        AssertNear([placed.X, placed.Y, placed.Width, placed.Height],
            [restored.X, restored.Y, restored.Width, restored.Height]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldKeepItsSizeAndComeBackInside_WhenTheWindowShrinks(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var dialog = Movable(page);
        await DragAsync(page, dialog.GetByRole(AriaRole.Button, new() { Name = "Resize" }), 40, 0);
        await DragAsync(page, dialog.GetByRole(AriaRole.Heading), 300, 0);
        var placed = await DialogTests.BoxAsync(page, "Movable");

        await page.SetViewportSizeAsync((int)(placed.X + placed.Width) - 200, 720);

        // The window's resize event arrives after the viewport changes, and the dialog follows it.
        await page.WaitForFunctionAsync(
            "() => document.querySelector('dialog[open]').getBoundingClientRect().right <= innerWidth");
        var (width, _) = await DialogTests.ViewportAsync(page);
        var inside = await DialogTests.BoxAsync(page, "Movable");
        Assert.Equal(width, inside.X + inside.Width, tolerance: 1);
        Assert.Equal(placed.Width, inside.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldComeBackInsideTheWindow_WhenRestoredAfterTheWindowShrank(string browser)
    {
        var page = await OpenMovableAsync(browser);
        var dialog = Movable(page);
        await DragAsync(page, dialog.GetByRole(AriaRole.Heading), 300, 0);
        var placed = await DialogTests.BoxAsync(page, "Movable");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Maximize" }).ClickAsync();
        await Expect(dialog).ToHaveAttributeAsync("data-window-state", "maximized");

        await page.SetViewportSizeAsync((int)(placed.X + placed.Width) - 200, 720);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Restore" }).ClickAsync();

        await Expect(dialog).ToHaveAttributeAsync("data-window-state", "normal");
        var (width, _) = await DialogTests.ViewportAsync(page);
        var restored = await DialogTests.BoxAsync(page, "Movable");
        Assert.Equal(width, restored.X + restored.Width, tolerance: 1);
        Assert.Equal(placed.Width, restored.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldKeepTheGripClearOfTheActions_WhenResizable(string browser)
    {
        var page = await OpenMovableAsync(browser);

        var save = (await Movable(page).GetByRole(AriaRole.Button, new() { Name = "Save" }).BoundingBoxAsync())!;
        var grip = (await Movable(page).GetByRole(AriaRole.Button, new() { Name = "Resize" }).BoundingBoxAsync())!;

        Assert.True(grip.Y >= save.Y + save.Height || grip.X >= save.X + save.Width,
            $"The grip at ({grip.X}, {grip.Y}) covers the Save button ending at ({save.X + save.Width}, "
            + $"{save.Y + save.Height}).");
    }

    private async Task<IPage> OpenMovableAsync(string browser, bool rightToLeft = false,
        BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/dialogs", options);
        if (rightToLeft)
        {
            await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Movable" }).ClickAsync();
        await Expect(Movable(page)).ToBeVisibleAsync();
        return page;
    }

    // Within a pixel, since layouts round.
    private static void AssertNear(double[] expected, double[] actual) =>
        Assert.All(expected.Zip(actual), pair => Assert.Equal(pair.First, pair.Second, tolerance: 1));

    private static ILocator Movable(IPage page) => page.GetByRole(AriaRole.Dialog, new() { Name = "Movable" });

    internal static Task TouchAsync(ICDPSession session, string type, double? x, double? y) =>
        session.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["touchPoints"] = x is null ? Array.Empty<object>() : new object[] { new { x, y } },
        });

    // Presses the middle of the element, moves by the distance in a few steps, and lets go.
    private static async Task DragAsync(IPage page, ILocator handle, double dx, double dy)
    {
        var box = (await handle.BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.MoveAsync((float)x, (float)y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)(x + dx), (float)(y + dy), new() { Steps = 5 });
        await page.Mouse.UpAsync();
    }
}
