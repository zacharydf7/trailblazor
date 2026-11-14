using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class ContextMenuTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RightClick_ShouldOpenTheMenuAtThePointerWithTheFirstItemFocused_WhenOnTheTarget(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var box = (await page.GetByText("Trails", new() { Exact = true }).BoundingBoxAsync())!;
        var (x, y) = (box.X + 10, box.Y + box.Height / 2);

        await page.Mouse.ClickAsync(x, y, new() { Button = MouseButton.Right });

        var menu = Menu(page, "Trail actions");
        await Expect(menu).ToBeVisibleAsync();
        var placed = (await menu.BoundingBoxAsync())!;
        Assert.Equal(x, placed.X, tolerance: 1);
        Assert.Equal(y, placed.Y, tolerance: 1);
        await Expect(menu.GetByRole(AriaRole.Menuitem).First).ToBeFocusedAsync();
        await Expect(menu.GetByRole(AriaRole.Menuitem).First).ToHaveTextAsync("Rename all");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RightClick_ShouldShowOnlyTheKeysItems_WhenOnAnElementWithAKey(string browser)
    {
        var page = await OpenReadyAsync(browser);
        // What the first item reads as each opening begins, before anything paints.
        await page.EvaluateAsync("""
            () => {
                window.shownItems = [];
                const menu = document.querySelector('[role=menu][aria-label="Trail actions"]');
                menu.addEventListener('beforetoggle', event => event.newState === 'open'
                    && window.shownItems.push(menu.querySelector('[role=menuitem]').textContent.trim()));
            }
            """);

        await page.GetByRole(AriaRole.Button, new() { Name = "Ridge loop" }).ClickAsync(
            new() { Button = MouseButton.Right });
        await Expect(Menu(page, "Trail actions")).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await page.GetByRole(AriaRole.Button, new() { Name = "Lake path" }).ClickAsync(
            new() { Button = MouseButton.Right });
        await Expect(Menu(page, "Trail actions").GetByRole(AriaRole.Menuitem).First).ToHaveTextAsync(
            "Rename Lake path");

        Assert.Equal(["Rename Ridge loop", "Rename Lake path"],
            await page.EvaluateAsync<string[]>("() => window.shownItems"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Item_ShouldRunCloseAndReturnFocus_WhenChosenWithEnter(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var ridge = page.GetByRole(AriaRole.Button, new() { Name = "Ridge loop" });
        await ridge.FocusAsync();
        await page.Keyboard.PressAsync("Shift+F10");
        await Expect(Menu(page, "Trail actions").GetByRole(AriaRole.Menuitem, new() { Name = "Rename Ridge loop" }))
            .ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("chosen")).ToHaveTextAsync("Rename Ridge loop");
        await Expect(Menu(page, "Trail actions")).ToBeHiddenAsync();
        await Expect(ridge).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldChooseThroughASubmenu_WhenGivenTheElementAndAPath(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var lake = page.GetByRole(AriaRole.Button, new() { Name = "Lake path" });

        await page.Tb().ContextMenu().On(page.GetByRole(AriaRole.Button, new() { Name = "Ridge loop" }))
            .ChooseAsync("Rename Ridge loop");
        await page.Tb().ContextMenu().On(lake).ChooseAsync("Move to", "Archive");

        await Expect(page.GetByTestId("chosen")).ToHaveTextAsync("Archive Lake path");
    }

    [Theory]
    [MemberData(nameof(OpeningKeys))]
    public async Task Key_ShouldOpenTheMenuBelowTheFocusedElementAndEscapeReturnFocus_WhenPressed(string browser,
        string key)
    {
        var page = await OpenReadyAsync(browser);
        var ridge = page.GetByRole(AriaRole.Button, new() { Name = "Ridge loop" });
        await ridge.FocusAsync();

        await page.Keyboard.PressAsync(key);

        var menu = Menu(page, "Trail actions");
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Rename Ridge loop" })).ToBeFocusedAsync();
        var (button, placed) = ((await ridge.BoundingBoxAsync())!, (await menu.BoundingBoxAsync())!);
        Assert.Equal(button.X, placed.X, tolerance: 1);
        Assert.Equal(button.Y + button.Height, placed.Y, tolerance: 1);
        await page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(ridge).ToBeFocusedAsync();
    }

    public static TheoryData<string, string> OpeningKeys()
    {
        var data = new TheoryData<string, string>();
        foreach (var browser in AllBrowsers.Names)
        {
            data.Add(browser, "Shift+F10");
            data.Add(browser, "ContextMenu");
        }

        return data;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RightClick_ShouldLeaveTheBrowsersMenu_WhenTheContextMenuIsDisabled(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.EvaluateAsync("""
            () => document.addEventListener('contextmenu', event => {
                window.prevented = event.defaultPrevented;
                event.preventDefault();
            })
            """);

        await page.GetByText("Disabled area").ClickAsync(new() { Button = MouseButton.Right });

        Assert.False(await page.EvaluateAsync<bool>("() => window.prevented"));
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Off", IncludeHidden = true })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Key_ShouldBeLeftToTheBrowser_WhenTheContextMenuIsDisabled(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.EvaluateAsync("""
            () => document.addEventListener('keydown', event => window.prevented = event.defaultPrevented)
            """);
        await page.GetByRole(AriaRole.Button, new() { Name = "Off button" }).FocusAsync();

        await page.Keyboard.PressAsync("Shift+F10");

        Assert.False(await page.EvaluateAsync<bool>("() => window.prevented"));
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Off", IncludeHidden = true })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RightClick_ShouldShowTheLastKeysItems_WhenARightClickFollowsBeforeTheServerAnswers(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        var trails = page.GetByText("Trails", new() { Exact = true });
        var menu = Menu(page, "Trail actions");

        // The second right-click lands on the key already drawn, before the first one's items are.
        await page.GetByRole(AriaRole.Button, new() { Name = "Ridge loop" }).ClickAsync(
            new() { Button = MouseButton.Right });
        await trails.ClickAsync(new() { Button = MouseButton.Right });

        await Expect(menu).ToBeVisibleAsync();
        await page.WaitForTimeoutAsync(500);
        await Expect(menu.GetByRole(AriaRole.Menuitem).First).ToHaveTextAsync("Rename all");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Click_ShouldKeepTheMenuClosed_WhenItLandsOutsideBeforeTheServerAnswers(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var note = page.GetByRole(AriaRole.Textbox, new() { Name = "Note" });

        await page.GetByRole(AriaRole.Button, new() { Name = "Ridge loop" }).ClickAsync(
            new() { Button = MouseButton.Right });
        await note.ClickAsync();

        await page.WaitForTimeoutAsync(500);
        await Expect(Menu(page, "Trail actions")).ToBeHiddenAsync();
        await Expect(note).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scroll_ShouldKeepTheMenuClosed_WhenThePageScrollsBeforeTheServerAnswers(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Ridge loop" }).ClickAsync(
            new() { Button = MouseButton.Right });
        await page.EvaluateAsync("() => window.scrollBy(0, 100)");

        await page.WaitForTimeoutAsync(500);
        await Expect(Menu(page, "Trail actions")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scroll_ShouldCloseTheMenu_WhenThePageScrolls(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.GetByText("Trails", new() { Exact = true }).ClickAsync(new() { Button = MouseButton.Right });
        await Expect(Menu(page, "Trail actions")).ToBeVisibleAsync();

        await page.EvaluateAsync("() => window.scrollBy(0, 100)");

        await Expect(Menu(page, "Trail actions")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RightClick_ShouldOpenOnlyTheInnerMenu_WhenOnANestedContextMenusArea(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.GetByText("Inner area").ClickAsync(new() { Button = MouseButton.Right });
        await Expect(Menu(page, "Inner")).ToBeVisibleAsync();
        await Expect(Menu(page, "Outer")).ToBeHiddenAsync();
        await page.Keyboard.PressAsync("Escape");
        await page.GetByText("Outer area").ClickAsync(new() { Button = MouseButton.Right });

        await Expect(Menu(page, "Outer")).ToBeVisibleAsync();
        await Expect(Menu(page, "Inner")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RightClick_ShouldKeepTheMenuInsideTheWindow_WhenNearItsBottomRightCorner(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var area = page.Locator(".tb-context-menu:has(> [role=menu][aria-label='Trail actions'])");
        var before = (await area.BoundingBoxAsync())!;
        await page.SetViewportSizeAsync(800, (int)(before.Y + before.Height) + 4);
        var box = (await area.BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width - 4, box.Y + box.Height - 4);

        await page.Mouse.ClickAsync(x, y, new() { Button = MouseButton.Right });

        var placed = (await Menu(page, "Trail actions").BoundingBoxAsync())!;
        Assert.Equal(x, placed.X + placed.Width, tolerance: 1);
        Assert.Equal(y, placed.Y + placed.Height, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RightClick_ShouldMoveTheMenu_WhenAnotherKeyIsRightClickedWhileItIsOpen(string browser)
    {
        var page = await OpenReadyAsync(browser);
        // The menu opens to the right of the pointer, so the button to the left of it stays uncovered.
        await page.GetByRole(AriaRole.Button, new() { Name = "Lake path" }).ClickAsync(
            new() { Button = MouseButton.Right });
        await Expect(Menu(page, "Trail actions")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Ridge loop" }).ClickAsync(
            new() { Button = MouseButton.Right });

        await Expect(Menu(page, "Trail actions").GetByRole(AriaRole.Menuitem).First).ToHaveTextAsync(
            "Rename Ridge loop");
        await page.WaitForTimeoutAsync(300);
        await Expect(Menu(page, "Trail actions")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Click_ShouldCloseTheMenuAndLeaveFocusWhereItWent_WhenOutsideIt(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.GetByText("Trails", new() { Exact = true }).ClickAsync(new() { Button = MouseButton.Right });
        await Expect(Menu(page, "Trail actions")).ToBeVisibleAsync();
        var note = page.GetByRole(AriaRole.Textbox, new() { Name = "Note" });

        await note.ClickAsync();

        await Expect(Menu(page, "Trail actions")).ToBeHiddenAsync();
        await Expect(note).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Escape_ShouldCloseASubmenuFirstThenTheMenu_WhenPressedTwice(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var ridge = page.GetByRole(AriaRole.Button, new() { Name = "Ridge loop" });
        await ridge.FocusAsync();
        await page.Keyboard.PressAsync("Shift+F10");
        var menu = Menu(page, "Trail actions");
        await Expect(menu.GetByRole(AriaRole.Menuitem).First).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Archive" })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");
        await Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Move to" })).ToBeFocusedAsync();
        await Expect(menu).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(menu).ToBeHiddenAsync();
        await Expect(ridge).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RightClick_ShouldOpenTheMenuTowardTheLeft_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenReadyAsync(browser);
        // The middle of the area has room on both sides, so only the direction decides the side.
        var box = (await page.GetByText("Right-to-left area").BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        await page.Mouse.ClickAsync(x, y, new() { Button = MouseButton.Right });

        var placed = (await Menu(page, "Right to left").BoundingBoxAsync())!;
        Assert.Equal(x, placed.X + placed.Width, tolerance: 1);
        Assert.Equal(y, placed.Y, tolerance: 1);
    }

    // Only Chromium can drive touch from Playwright.
    [Fact]
    public async Task LongPress_ShouldOpenTheMenuAndKeepItOpen_WhenATouchRestsOnTheTargetAndLifts()
    {
        var page = await OpenReadyAsync("chromium", new() { HasTouch = true });
        var box = (await page.GetByRole(AriaRole.Button, new() { Name = "Lake path" }).BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);
        var touch = await page.Context.NewCDPSessionAsync(page);
        var menu = Menu(page, "Trail actions");

        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null);
        await page.WaitForTimeoutAsync(700);
        await Expect(menu).ToBeHiddenAsync();
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await Expect(menu.GetByRole(AriaRole.Menuitem).First).ToHaveTextAsync("Rename Lake path");
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null);

        await page.WaitForTimeoutAsync(300);
        await Expect(menu).ToBeVisibleAsync();
    }

    [Fact]
    public async Task LongPress_ShouldOpenNothing_WhenTheTouchMovesBeforeTheDelay()
    {
        var page = await OpenReadyAsync("chromium", new() { HasTouch = true });
        var trails = page.GetByText("Trails", new() { Exact = true });
        // An area that doesn't pan, such as a map, gets no pointercancel from the browser as the touch moves.
        await trails.EvaluateAsync("text => text.style.touchAction = 'none'");
        var box = (await trails.BoundingBoxAsync())!;
        var (x, y) = (box.X + 10, box.Y + box.Height / 2);
        var touch = await page.Context.NewCDPSessionAsync(page);

        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", x + 30, y);
        await page.WaitForTimeoutAsync(700);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null);

        await Expect(Menu(page, "Trail actions")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MouseHold_ShouldOpenNothing_WhenTheLeftButtonRestsOnTheTarget(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var box = (await page.GetByText("Trails", new() { Exact = true }).BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + 10, box.Y + box.Height / 2);

        await page.Mouse.DownAsync();
        await page.WaitForTimeoutAsync(700);
        await Expect(Menu(page, "Trail actions")).ToBeHiddenAsync();
        await page.Mouse.UpAsync();
    }

    private static ILocator Menu(IPage page, string name) =>
        page.GetByRole(AriaRole.Menu, new() { Name = name, IncludeHidden = true });

    private async Task<IPage> OpenReadyAsync(string browser, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/context-menu", options);
        await Expect(page.Locator(".tb-context-menu:not([data-ready])")).ToHaveCountAsync(0);
        return page;
    }
}
