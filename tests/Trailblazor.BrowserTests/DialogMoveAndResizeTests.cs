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

    private async Task<IPage> OpenMovableAsync(string browser, bool rightToLeft = false)
    {
        var page = await OpenAsync(browser, "/dialogs");
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
