using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DialogTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldAnswerAConfirmAndFillOrCancelADialog_WhenGivenItsTitle(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        var delete = page.GetByRole(AriaRole.Button, new() { Name = "Delete report" });

        await delete.ClickAsync();
        await page.Tb().Dialog("Delete report?").ChooseAsync("Delete");
        await Expect(page.GetByText("Deleted", new() { Exact = true })).ToBeVisibleAsync();
        await delete.ClickAsync();
        await page.Tb().Dialog("Delete report?").CancelAsync();
        await Expect(page.GetByText("Kept", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Rename report" }).ClickAsync();
        var dialog = await page.Tb().Dialog("Rename report").WaitForAsync();
        await dialog.GetByLabel("Name").FillAsync("Annual report");
        await page.Tb().Dialog("Rename report").ChooseAsync("Save");

        await Expect(page.GetByText("Renamed to Annual report")).ToBeVisibleAsync();
    }

    // A positioned dialog's distance from the window edge, --tb-space-4.
    private const double EdgeGap = 16;

    // The gap between minimized dialogs in the tray, --tb-space-2.
    private const double TrayGap = 8;

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
    public async Task Dialog_ShouldKeepItsTitleAsItsNameWithoutAddingAHeading_WhenShownWithoutAHeader(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = "Headless" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Headless" });
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.GetByRole(AriaRole.Heading)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldCollapseToItsHeaderInTheCornerAndKeepFocus_WhenMinimized(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Notes", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Notes 1" });
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Minimize" }).FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(dialog).ToHaveAttributeAsync("data-window-state", "minimized");
        await Expect(dialog.GetByLabel("Name")).ToBeHiddenAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Restore" })).ToBeFocusedAsync();
        var box = await BoxAsync(page, "Notes 1");
        var (width, height) = await ViewportAsync(page);
        Assert.Equal(width - EdgeGap, box.X + box.Width, tolerance: 1);
        Assert.Equal(height - EdgeGap, box.Y + box.Height, tolerance: 1);
        await page.GetByTestId("page-button").ClickAsync();
        await Expect(page.GetByTestId("page-clicks")).ToHaveTextAsync("1");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Restore" }).ClickAsync();
        await Expect(dialog.GetByLabel("Name")).ToBeVisibleAsync();
    }

    public static TheoryData<string, bool> ModalOrNot()
    {
        var data = new TheoryData<string, bool>();
        foreach (var browser in AllBrowsers.Names)
        {
            data.Add(browser, true);
            data.Add(browser, false);
        }

        return data;
    }

    [Theory, MemberData(nameof(ModalOrNot))]
    public async Task Dialog_ShouldOpenInTheCornerAndLeaveThePageUsable_WhenItStartsMinimized(string browser, bool modal)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = modal ? "Tucked" : "Tucked window", Exact = true })
            .ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Tucked" });
        await Expect(dialog).ToHaveAttributeAsync("data-window-state", "minimized");
        await Expect(dialog.GetByLabel("Name")).ToBeHiddenAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Restore" })).ToBeFocusedAsync();
        var box = await BoxAsync(page, "Tucked");
        var (width, height) = await ViewportAsync(page);
        Assert.Equal(width - EdgeGap, box.X + box.Width, tolerance: 1);
        Assert.Equal(height - EdgeGap, box.Y + box.Height, tolerance: 1);
        await page.GetByTestId("page-button").ClickAsync();
        await Expect(page.GetByTestId("page-clicks")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldSitBesideTheOtherMinimizedDialogAndMoveOver_WhenTheOtherCloses(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        var notes = page.GetByRole(AriaRole.Button, new() { Name = "Notes", Exact = true });
        var first = page.GetByRole(AriaRole.Dialog, new() { Name = "Notes 1" });
        var second = page.GetByRole(AriaRole.Dialog, new() { Name = "Notes 2" });
        // Each opens in the middle, over the last, so it's minimized before the next opens.
        await notes.ClickAsync();
        await first.GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();
        await notes.ClickAsync();
        await second.GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();
        await Expect(second).ToHaveAttributeAsync("data-window-state", "minimized");

        var firstBox = await BoxAsync(page, "Notes 1");
        var secondBox = await BoxAsync(page, "Notes 2");
        Assert.Equal(firstBox.X - TrayGap, secondBox.X + secondBox.Width, tolerance: 1);
        await first.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
        await Expect(first).ToHaveCountAsync(0);

        var (width, _) = await ViewportAsync(page);
        var moved = await BoxAsync(page, "Notes 2");
        Assert.Equal(width - EdgeGap, moved.X + moved.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStackMinimizedDialogsUpward_WhenARowHasNoRoomForTheNext(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs", new() { ViewportSize = new() { Width = 400, Height = 800 } });
        var notes = page.GetByRole(AriaRole.Button, new() { Name = "Notes", Exact = true });
        var first = page.GetByRole(AriaRole.Dialog, new() { Name = "Notes 1" });
        var second = page.GetByRole(AriaRole.Dialog, new() { Name = "Notes 2" });
        await notes.ClickAsync();
        await first.GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();
        await notes.ClickAsync();

        await second.GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();

        await Expect(second).ToHaveAttributeAsync("data-window-state", "minimized");
        var firstBox = await BoxAsync(page, "Notes 1");
        var secondBox = await BoxAsync(page, "Notes 2");
        Assert.Equal(firstBox.Y - TrayGap, secondBox.Y + secondBox.Height, tolerance: 1);
        Assert.Equal(firstBox.X, secondBox.X, tolerance: 1);
        Assert.True(secondBox.X >= 0, $"The second minimized dialog starts off screen, at {secondBox.X}px.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldSpaceTheTrayWithTheThemesSpacing_WhenTheRootFontIsLarger(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs", new() { ViewportSize = new() { Width = 400, Height = 800 } });
        // --tb-space-2 and --tb-space-4 are rem, so a 20px root font makes them 10px and 20px.
        await page.EvaluateAsync("() => document.documentElement.style.fontSize = '20px'");
        var notes = page.GetByRole(AriaRole.Button, new() { Name = "Notes", Exact = true });
        var first = page.GetByRole(AriaRole.Dialog, new() { Name = "Notes 1" });
        var second = page.GetByRole(AriaRole.Dialog, new() { Name = "Notes 2" });
        await notes.ClickAsync();
        await first.GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();
        await notes.ClickAsync();

        await second.GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();

        await Expect(second).ToHaveAttributeAsync("data-window-state", "minimized");
        var firstBox = await BoxAsync(page, "Notes 1");
        var secondBox = await BoxAsync(page, "Notes 2");
        Assert.Equal(firstBox.Y - 10, secondBox.Y + secondBox.Height, tolerance: 1);
        Assert.Equal((await ViewportAsync(page)).Width - 20, firstBox.X + firstBox.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldCollapseToItsHeader_WhenAFullSizeDialogIsMinimized(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Full notes" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Notes 1" });

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();

        await Expect(dialog).ToHaveAttributeAsync("data-window-state", "minimized");
        var header = (await dialog.Locator("[data-part=header]").BoundingBoxAsync())!;
        var box = await BoxAsync(page, "Notes 1");
        Assert.Equal(header.Y + header.Height, box.Y + box.Height, tolerance: 2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldFillTheWindow_WhenItsSizeIsFull(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = "Full notes" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Notes 1" })).ToBeVisibleAsync();
        var (width, height) = await ViewportAsync(page);
        var box = await BoxAsync(page, "Notes 1");
        Assert.Equal((0d, 0d, width, height), ((double)box.X, (double)box.Y, (double)box.Width, (double)box.Height));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldTakeItsWidth_WhenItsSizeIsFullAndItHasAWidth(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = "Full sized" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Full sized" })).ToBeVisibleAsync();
        Assert.Equal(600, (await BoxAsync(page, "Full sized")).Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldFillTheWindowAndComeBack_WhenMaximizedAndRestored(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Notes", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Notes 1" });
        var normal = await BoxAsync(page, "Notes 1");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Maximize" }).FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(dialog).ToHaveAttributeAsync("data-window-state", "maximized");
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Restore" })).ToBeFocusedAsync();
        var (width, height) = await ViewportAsync(page);
        var maximized = await BoxAsync(page, "Notes 1");
        Assert.Equal((width, height), (maximized.Width, maximized.Height));
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Restore" }).ClickAsync();
        await Expect(dialog).ToHaveAttributeAsync("data-window-state", "normal");
        Assert.Equal(normal.Width, (await BoxAsync(page, "Notes 1")).Width, tolerance: 1);
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
    public async Task Dialog_ShouldFocusTheGroupsCheckedItem_WhenARadioGroupHasAutoFocus(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = "Pace", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Radio, new() { Name = "Fast" })).ToBeFocusedAsync();
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
    public async Task Dialog_ShouldLetThePageTakeInputAndKeepItsFocus_WhenAModalFadesOut(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        // A long fade, so the test acts while the dialog is still leaving.
        await page.AddStyleTagAsync(new() { Content = ":root { --tb-motion-duration-normal: 3s; }" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Rename" }).ClickAsync();
        var dialog = page.Locator("dialog.tb-dialog");
        await Expect(page.GetByRole(AriaRole.Dialog).GetByLabel("Name")).ToBeFocusedAsync();
        // Counts focus moving onto the closing dialog's controls, which would raise their focus and blur events.
        await dialog.EvaluateAsync("""
            dialog => dialog.addEventListener('focusin', event => {
                window.focusedWhileClosing = (window.focusedWhileClosing ?? 0) + (event.target === dialog ? 0 : 1);
            })
            """);

        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveAttributeAsync("data-leaving", "");
        Assert.NotEqual("rgba(0, 0, 0, 0)",
            await dialog.EvaluateAsync<string>("dialog => getComputedStyle(dialog, '::backdrop').backgroundColor"));
        // The dialog and its backdrop both fade as the dialog lets the page go; WebKit lists no ::backdrop animation.
        var fading = await dialog.EvaluateAsync<bool[]>("""
            dialog => [false, true].map(backdrop => dialog.getAnimations({ subtree: true })
                .some(animation => (animation.effect.pseudoElement === '::backdrop') === backdrop))
            """);
        Assert.Equal(new[] { true, browser != "webkit" }, fading);
        var note = page.GetByLabel("Page note");
        await note.FocusAsync();
        await page.Keyboard.TypeAsync("hi");

        await Expect(note).ToHaveValueAsync("hi");
        Assert.True(await dialog.EvaluateAsync<bool>("dialog => dialog.hasAttribute('data-leaving')"),
            "The dialog finished leaving before the page was used.");
        await Expect(dialog).ToBeHiddenAsync(new() { Timeout = 10_000 });
        await Expect(note).ToBeFocusedAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("() => window.focusedWhileClosing ?? 0"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldRefocusTheOpener_WhenFocusWasOnThePageBodyAsItClosed(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        var rename = page.GetByRole(AriaRole.Button, new() { Name = "Rename" });
        await rename.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog).GetByLabel("Name")).ToBeFocusedAsync();

        // As when the focused control is removed, focus falls to the body.
        await page.EvaluateAsync("() => document.activeElement.blur()");
        await page.Keyboard.PressAsync("Escape");

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
    public async Task Dialog_ShouldHideTheTooltipFirstAndCancelNext_WhenEscapeIsPressedWithATooltipOpen(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Hinted", Exact = true }).ClickAsync();
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Your full name" });
        await Expect(tooltip).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");
        await Expect(tooltip).ToBeHiddenAsync();
        await page.WaitForTimeoutAsync(300);
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Hinted" })).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("result")).ToHaveTextAsync("hinted cancelled");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldCloseTheSuggestionsFirstAndCancelNext_WhenEscapeIsPressedWithThemOpen(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Suggest" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Suggest" });
        var trail = dialog.GetByRole(AriaRole.Combobox, new() { Name = "Trail" });
        await trail.FillAsync("Ri");
        await Expect(dialog.GetByRole(AriaRole.Option, new() { Name = "Ridge loop" })).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");
        await Expect(trail).ToHaveAttributeAsync("aria-expanded", "false");
        await page.WaitForTimeoutAsync(300);
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(trail).ToHaveValueAsync("Ri");
        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("result")).ToHaveTextAsync("suggest cancelled");
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
    internal static async Task<(double Width, double Height)> ViewportAsync(IPage page)
    {
        var size = await page.EvaluateAsync<double[]>(
            "() => [document.documentElement.clientWidth, document.documentElement.clientHeight]");
        return (size[0], size[1]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldBeAsWideAsItsContentUpToItsWidth_WhenItsSizeIsFit(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = "Fit", Exact = true }).ClickAsync();
        var fit = await BoxAsync(page, "Fit");
        await page.Keyboard.PressAsync("Escape");
        await page.GetByRole(AriaRole.Button, new() { Name = "Capped", Exact = true }).ClickAsync();
        var capped = await BoxAsync(page, "Capped");

        // Medium, the default, is 32rem (512px); 6rem is 96px.
        Assert.InRange(fit.Width, 97, 511);
        Assert.Equal(96, capped.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldFitItsContentUpToTheWindow_WhenItIsInsideASmallerDialog(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Small host" }).ClickAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Fitted" }).ClickAsync();

        // Small is 24rem (384px); the fitted dialog's words are wider.
        Assert.True((await BoxAsync(page, "Fitted")).Width > 385);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drawer_ShouldStopAtItsWidth_WhenItsSizeIsFit(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");

        await page.GetByRole(AriaRole.Button, new() { Name = "Fit drawer" }).ClickAsync();

        Assert.Equal(160, (await BoxAsync(page, "Fit drawer")).Width, tolerance: 1);
    }

    internal static async Task<LocatorBoundingBoxResult> BoxAsync(IPage page, string title)
    {
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = title });
        await Expect(dialog).ToBeVisibleAsync();
        return (await dialog.BoundingBoxAsync())!;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldOpenFromACommandAndCloseFromItsButton_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-dialog");
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Returns" });

        await page.GetByRole(AriaRole.Button, new() { Name = "Returns policy" }).ClickAsync();

        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog).ToContainTextAsync("Returns are free for a month.");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
        await Expect(dialog).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReturn_WhenTheDialogsButtonReloadsThePage(string browser)
    {
        var page = await OpenAsync(browser, "/dialog-reload");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open sign out" }).ClickAsync();

        await page.Tb().Dialog("Sign out?").ChooseAsync("Sign out");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReturnOnceTheDialogCloses_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-dialog");
        var open = page.GetByRole(AriaRole.Button, new() { Name = "Returns policy" });

        await open.ClickAsync();
        await page.Tb().Dialog("Returns").ChooseAsync("Close");
        await open.ClickAsync();
        await page.Tb().Dialog("Returns").CancelAsync();

        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
    }
}
