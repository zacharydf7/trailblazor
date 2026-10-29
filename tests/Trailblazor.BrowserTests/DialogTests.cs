namespace Trailblazor.BrowserTests;

public class DialogTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // A positioned dialog's distance from the window edge, --tb-space-4.
    private const double EdgeGap = 16;

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldFocusTheFirstFieldAndKeepFocusOffThePage_WhenModal(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = "Rename" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Rename" });
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.GetByLabel("Name")).ToBeFocusedAsync();
        // Tab may leave for the browser's own controls, but it must never reach the page behind the modal.
        for (var press = 0; press < 5; press++)
        {
            await page.Keyboard.PressAsync("Tab");
            Assert.True(await dialog.EvaluateAsync<bool>(
                "dialog => dialog.contains(document.activeElement) || document.activeElement === document.body"),
                "Focus reached the page behind the modal dialog.");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldFocusItself_WhenItsInitialFocusIsTheDialog(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = "About" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "About" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldFocusTheFieldMarkedAutofocus_WhenOneIsInside(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = "Address", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Dialog).GetByLabel("Floor")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldFocusTheFirstField_WhenTheFieldMarkedAutofocusIsDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = "Address locked" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Dialog).GetByLabel("Street")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldReturnItsValueAndRefocusTheOpener_WhenSaved(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        var rename = page.GetByRole(AriaRole.Button, new() { Name = "Rename" });
        await rename.ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Rename" });

        await dialog.GetByLabel("Name").FillAsync("Grace");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(page.GetByTestId("result")).ToHaveTextAsync("saved Grace");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(rename).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldCancelAndRefocusTheOpener_WhenEscapeIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        var rename = page.GetByRole(AriaRole.Button, new() { Name = "Rename" });
        await rename.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog).GetByLabel("Name")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("result")).ToHaveTextAsync("cancelled");
        await Expect(rename).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldCancel_WhenTheBackdropIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Rename" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();

        await page.Mouse.ClickAsync(5, 5);

        await Expect(page.GetByTestId("result")).ToHaveTextAsync("cancelled");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStayOpen_WhenEscapeIsPressedTwiceOrTheBackdropIsClickedWhileLocked(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Locked", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Locked" });
        await Expect(dialog.GetByLabel("Name")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Escape");
        await page.Mouse.ClickAsync(5, 5);
        await page.WaitForTimeoutAsync(300);

        await Expect(dialog).ToBeVisibleAsync();
        await Expect(page.GetByTestId("result")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldCancel_WhenEscapeIsPressedWithATooltipOpen(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Hinted", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Your full name" })).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("result")).ToHaveTextAsync("hinted cancelled");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStayOpen_WhenEscapeIsPressedWithATooltipOpenWhileLocked(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Hinted locked" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Your full name" })).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Escape");
        await page.WaitForTimeoutAsync(300);

        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Hinted" })).ToBeVisibleAsync();
        await Expect(page.GetByTestId("result")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStayOpen_WhenADragFromAFieldEndsOverTheBackdrop(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Rename" }).ClickAsync();
        var field = page.GetByRole(AriaRole.Dialog).GetByLabel("Name");
        var box = await field.BoundingBoxAsync();

        await page.Mouse.MoveAsync(box!.X + 5, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(5, 5, new() { Steps = 5 });
        await page.Mouse.UpAsync();
        await page.WaitForTimeoutAsync(300);

        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Rename" })).ToBeVisibleAsync();
        await Expect(page.GetByTestId("result")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldCloseOnlyTheInnerDialog_WhenEscapeIsPressedInANestedOne(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Nested" }).ClickAsync();
        var outer = page.GetByRole(AriaRole.Dialog, new() { Name = "Outer" });
        var openInner = outer.GetByRole(AriaRole.Button, new() { Name = "Open inner" });
        await openInner.ClickAsync();
        var inner = page.GetByRole(AriaRole.Dialog, new() { Name = "Inner" });
        await Expect(inner.GetByLabel("Name")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(inner).ToHaveCountAsync(0);
        await Expect(outer).ToBeVisibleAsync();
        await Expect(openInner).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldLetEscapeCloseAMenuFirst_WhenAMenuIsOpenInside(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Nested" }).ClickAsync();
        var outer = page.GetByRole(AriaRole.Dialog, new() { Name = "Outer" });
        await outer.GetByRole(AriaRole.Button, new() { Name = "Options" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Archive" })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Menu)).ToBeHiddenAsync();
        await Expect(outer).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldLockPageScroll_WhenModal(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        var overflow = "() => getComputedStyle(document.documentElement).overflow";
        Assert.NotEqual("hidden", await page.EvaluateAsync<string>(overflow));

        await page.GetByRole(AriaRole.Button, new() { Name = "Rename" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();

        Assert.Equal("hidden", await page.EvaluateAsync<string>(overflow));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldLeaveThePageUsable_WhenNotModal(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Panel" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Panel" })).ToBeVisibleAsync();

        await page.GetByTestId("page-button").ClickAsync();

        await Expect(page.GetByTestId("page-clicks")).ToHaveTextAsync("1");
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Panel" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldSitInTheBottomEndCorner_WhenPositionedThereWithoutAModal(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Docked" }).ClickAsync();

        var box = await BoxAsync(page, "Placed BottomEnd");

        var (width, height) = await ViewportAsync(page);
        Assert.Equal(width - EdgeGap, box.X + box.Width, tolerance: 1);
        Assert.Equal(height - EdgeGap, box.Y + box.Height, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldSitAtTheTopCentered_WhenPositionedAtTheTop(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Top", Exact = true }).ClickAsync();

        var box = await BoxAsync(page, "Placed Top");

        Assert.Equal(EdgeGap, box.Y, tolerance: 1);
        Assert.Equal((await ViewportAsync(page)).Width / 2, box.X + box.Width / 2, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldSitAtTheRightEdge_WhenPositionedAtTheStartOfARightToLeftPage(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        await page.GetByRole(AriaRole.Button, new() { Name = "Start", Exact = true }).ClickAsync();

        var box = await BoxAsync(page, "Placed Start");

        Assert.Equal((await ViewportAsync(page)).Width - EdgeGap, box.X + box.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Confirm_ShouldReturnTrue_WhenTheConfirmButtonIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Delete?" });
        await Expect(dialog).ToHaveAccessibleDescriptionAsync("This can't be undone.");
        await Expect(dialog).ToContainTextAsync("This can't be undone.");

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Expect(page.GetByTestId("result")).ToHaveTextAsync("deleted");
    }

    // A dialog sits within the page's own area, which leaves out a scrollbar that takes up room.
    private static async Task<(double Width, double Height)> ViewportAsync(IPage page)
    {
        var size = await page.EvaluateAsync<double[]>(
            "() => [document.documentElement.clientWidth, document.documentElement.clientHeight]");
        return (size[0], size[1]);
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(IPage page, string title)
    {
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = title });
        await Expect(dialog).ToBeVisibleAsync();
        return (await dialog.BoundingBoxAsync())!;
    }
}
