using System.Globalization;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Dragging holds the pointer, which Firefox can leak into parallel pages, so it runs with the other drag tests.
[Collection(nameof(PointerDrags))]
public class DockManagerWindowTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldFollowThePointerAndKeepItsNewBox_WhenItsMoveButtonIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        var move = page.GetByRole(AriaRole.Button, new() { Name = "Move the Gear window" });
        var row = page.Locator(".tb-dock__group--floating");
        var before = (await row.BoundingBoxAsync())!;

        // A tenth of the dock to the right and a tenth up: 766 by 382 pixels inside its border.
        await PressAsync(page, move);
        await page.Mouse.MoveAsync(await CenterXAsync(move) + 76.6f, await CenterYAsync(move) - 38.2f,
            new() { Steps = 4 });

        // The window's row, panel and grip follow at once.
        var during = (await row.BoundingBoxAsync())!;
        Assert.Equal(before.X + 76.6, during.X, 1.0);
        Assert.Equal(before.Y - 38.2, during.Y, 1.0);
        Assert.Equal(during.X, (await Panel(page, "Gear").BoundingBoxAsync())!.X, 1.0);
        var grip = page.GetByRole(AriaRole.Button, new() { Name = "Resize the Gear window" });
        Assert.Equal(during.X + during.Width, await CenterXAsync(grip) + 12, 1.5);
        var panel = (await Panel(page, "Gear").BoundingBoxAsync())!;
        Assert.Equal(panel.Y + panel.Height, await CenterYAsync(grip) + 12, 1.5);
        await ReleaseAsync(page);

        var box = await dock.WindowAsync("Gear");
        Assert.Equal(35, box.X, 0.2);
        Assert.Equal(15, box.Y, 0.2);
        Assert.Equal((50, 50), (box.Width, box.Height));
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("\"floating\":[{\"panels\":[\"gear\"]");
        // The drag's release doesn't open the button's menu. A click does, and Left moves a tenth of the dock.
        var left = page.GetByRole(AriaRole.Menuitem, new() { Name = "Left", Exact = true });
        await Expect(left).ToBeHiddenAsync();
        await move.ClickAsync();
        await ChangeAsync(page, () => left.ClickAsync());
        Assert.Equal(25, (await dock.WindowAsync("Gear")).X, 0.2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldMoveAndStopAtTheDocksEdge_WhenTheEmptyPartOfItsRowIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        var row = (await page.Locator(".tb-dock__group--floating").BoundingBoxAsync())!;
        // Past the window's one tab and before the menu button that ends the row.
        var (x, y) = (row.X + row.Width * 0.6f, row.Y + row.Height / 2);

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        // Far past the dock's end, though still in the window, where every browser's driver delivers the release.
        await page.Mouse.MoveAsync(1200, 700, new() { Steps = 4 });
        // The window stops at the dock's edge while it is dragged, not only once the app has its box.
        var whole = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var during = (await page.Locator(".tb-dock__group--floating").BoundingBoxAsync())!;
        Assert.Equal(whole.X + whole.Width - 1, during.X + during.Width, 1.0);
        await ReleaseAsync(page);

        // The window is whole inside the dock, in the corner where it ends.
        Assert.Equal((50, 50, 50, 50), await dock.WindowAsync("Gear"));
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Tab options" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldResizeFromItsCornerWithinItsLimits_WhenItsGripIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        var grip = page.GetByRole(AriaRole.Button, new() { Name = "Resize the Gear window" });
        var row = page.Locator(".tb-dock__group--floating");
        var before = (await row.BoundingBoxAsync())!;

        // A tenth of the dock narrower and a tenth taller.
        await PressAsync(page, grip);
        await page.Mouse.MoveAsync(await CenterXAsync(grip) - 76.6f, await CenterYAsync(grip) + 38.2f,
            new() { Steps = 4 });
        Assert.Equal(before.Width - 76.6, (await row.BoundingBoxAsync())!.Width, 1.0);
        Assert.Equal(before.X, (await row.BoundingBoxAsync())!.X, 0.5);
        await ReleaseAsync(page);

        var box = await dock.WindowAsync("Gear");
        Assert.Equal((25, 25), (box.X, box.Y));
        Assert.Equal(40, box.Width, 0.2);
        Assert.Equal(60, box.Height, 0.2);
        // The drag's release doesn't open the grip's menu. A click does, and Wider adds a tenth of the dock.
        var wider = page.GetByRole(AriaRole.Menuitem, new() { Name = "Wider", Exact = true });
        await Expect(wider).ToBeHiddenAsync();
        await grip.ClickAsync();
        await ChangeAsync(page, () => wider.ClickAsync());
        Assert.Equal(50, (await dock.WindowAsync("Gear")).Width, 0.2);

        // No smaller than a tenth of the dock, or than the 64 pixels this dock leaves a group, which is more here.
        await PressAsync(page, grip);
        await page.Mouse.MoveAsync(0, 0, new() { Steps = 4 });
        await ReleaseAsync(page);
        box = await dock.WindowAsync("Gear");
        Assert.Equal(10, box.Width, 0.2);
        Assert.Equal(64.0 / 382 * 100, box.Height, 0.3);

        // And no further than the dock's far edges.
        await PressAsync(page, grip);
        await page.Mouse.MoveAsync(1200, 700, new() { Steps = 4 });
        await ReleaseAsync(page);
        box = await dock.WindowAsync("Gear");
        Assert.Equal((75, 75), (box.Width, box.Height));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldMoveAndResizeByAPercent_WhenTheHelpersPressItsArrowKeys(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");

        await dock.MoveWindowAsync("Gear", 13, -4);

        Assert.Equal((38, 21, 50, 50), await dock.WindowAsync("Gear"));
        // The arrows moved the window, and didn't open the menu of the button they were pressed on.
        var move = page.GetByRole(AriaRole.Button, new() { Name = "Move the Gear window" });
        await Expect(move).ToBeFocusedAsync();
        await Expect(move).ToHaveAttributeAsync("aria-expanded", "false");

        await dock.ResizeWindowAsync("Gear", -12, 5);

        Assert.Equal((38, 21, 38, 55), await dock.WindowAsync("Gear"));
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Resize the Gear window" }))
            .ToHaveAttributeAsync("aria-expanded", "false");
        Assert.Equal("The panel \"Trails\" isn't in a floating window.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.MoveWindowAsync("Trails", 1, 1))).Message);

        // The notes join the window. Their panel was on the page before the window was, and the grip still comes
        // after it: the Move button starts the window's row, then come the showing tab, the menu button, the panel
        // and the field it holds, and the grip ends the window.
        await dock.MoveToGroupAsync("Notes", 4);
        move = page.GetByRole(AriaRole.Button, new() { Name = "Move the Notes window" });
        await move.FocusAsync();
        for (var stop = 0; stop < 5; stop++)
        {
            await page.Keyboard.PressAsync("Tab");
        }

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Resize the Notes window" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldGoTheWayTheArrowAndThePointerGo_WhenTextRunsRightToLeft(string browser)
    {
        var page = await OpenDockAsync(browser, "?direction=rtl");
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        var row = page.Locator(".tb-dock__group--floating");
        var before = (await row.BoundingBoxAsync())!;

        // Five percent to the right of the screen, which is toward where text starts, so X drops by 5.
        await dock.MoveWindowAsync("Gear", 5, 0);

        Assert.Equal(20, (await dock.WindowAsync("Gear")).X);
        Assert.Equal(before.X + 766 * 0.05, (await row.BoundingBoxAsync())!.X, 1.0);

        // The grip is in the left corner here, so wider pulls the left edge out.
        await dock.ResizeWindowAsync("Gear", 5, 0);

        Assert.Equal((20, 55), ((await dock.WindowAsync("Gear")).X, (await dock.WindowAsync("Gear")).Width));
        Assert.Equal(before.X, (await row.BoundingBoxAsync())!.X, 1.0);
        var grip = page.GetByRole(AriaRole.Button, new() { Name = "Resize the Gear window" });
        Assert.Equal(before.X, await CenterXAsync(grip) - 12, 1.5);
        var panel = (await Panel(page, "Gear").BoundingBoxAsync())!;
        Assert.Equal(panel.Y + panel.Height, await CenterYAsync(grip) + 12, 1.5);

        // A drag to the right takes the window to the right.
        var move = page.GetByRole(AriaRole.Button, new() { Name = "Move the Gear window" });
        await PressAsync(page, move);
        await page.Mouse.MoveAsync(await CenterXAsync(move) + 76.6f, await CenterYAsync(move), new() { Steps = 4 });
        await ReleaseAsync(page);
        Assert.Equal(10, (await dock.WindowAsync("Gear")).X, 0.2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldLeaveTheOtherWindowsAlone_WhenTheAppTakesItAwayWhileItIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        await dock.FloatAsync("Notes");
        // Out of the way of Gear's Move button.
        await dock.MoveWindowAsync("Notes", 20, 20);
        var move = page.GetByRole(AriaRole.Button, new() { Name = "Move the Gear window" });
        var (x, y) = (await CenterXAsync(move), await CenterYAsync(move));
        await PressAsync(page, move);
        await page.Mouse.MoveAsync(x + 40, y + 20, new() { Steps = 4 });

        // The app closes Gear, and the notes' window takes the number Gear's had.
        await page.GetByRole(AriaRole.Button, new() { Name = "Close gear" }).DispatchEventAsync("click");
        var rows = page.Locator(".tb-dock__group--floating");
        await Expect(rows).ToHaveCountAsync(1);
        var before = (await rows.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(x + 80, y + 40, new() { Steps = 4 });

        var after = (await rows.BoundingBoxAsync())!;
        Assert.Equal((before.X, before.Y), (after.X, after.Y));
        // The release reports nothing, so the next step's report is the next one the dock numbers.
        var root = page.Locator(".tb-dock");
        var reports = int.Parse((await root.GetAttributeAsync("data-report"))!, CultureInfo.InvariantCulture);
        await page.Mouse.UpAsync();
        await dock.MoveWindowAsync("Notes", 1, 0);
        Assert.Equal((49, 48, 50, 50), await dock.WindowAsync("Notes"));
        await Expect(root).ToHaveAttributeAsync("data-report", (reports + 1).ToString(CultureInfo.InvariantCulture));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldShowItsGripOverItsOwnPanelAndUnderTheWindowsInFront_WhenWindowsOverlap(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        await dock.FloatAsync("Notes");

        // Each window floats a little further down and along, so the third covers the corner of the second.
        await dock.FloatAsync("Weather");

        Assert.Equal("Resize the Weather window", await AtGripAsync(page, "Weather"));
        Assert.Equal("weather", await AtGripAsync(page, "Notes"));
    }

    // Only Chromium can drive touch from Playwright.
    [Fact]
    public async Task Window_ShouldFollowAFinger_WhenItDragsTheEmptyPartOfItsRow()
    {
        var page = await OpenDockAsync("chromium", options: new() { HasTouch = true });
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        var row = (await page.Locator(".tb-dock__group--floating").BoundingBoxAsync())!;
        var (x, y) = (row.X + row.Width * 0.6, row.Y + row.Height / 2);
        var touch = await page.Context.NewCDPSessionAsync(page);

        // A tenth of the dock along and down. The browser would take the finger's moves for scrolling if the dock
        // let it, and the drag would end there.
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", x + 30, y + 15);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", x + 76.6, y + 38.2);
        await ChangeAsync(page, () => DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null));

        var box = await dock.WindowAsync("Gear");
        Assert.Equal(35, box.X, 0.3);
        Assert.Equal(35, box.Y, 0.3);
    }

    // What shows at the middle of a window's grip: the name of the grip there, or else the Id of the panel there.
    private static async Task<string?> AtGripAsync(IPage page, string title)
    {
        var grip = page.GetByRole(AriaRole.Button, new() { Name = $"Resize the {title} window" });
        return await page.EvaluateAsync<string?>("""
            ({ x, y }) => {
                const top = document.elementFromPoint(x, y);
                return top.closest('[data-part=window-grip]')?.querySelector('button').getAttribute('aria-label')
                    ?? top.closest('.tb-dock__panel')?.dataset.panel ?? null;
            }
            """, new { x = (double)await CenterXAsync(grip), y = (double)await CenterYAsync(grip) });
    }

    private static async Task<float> CenterXAsync(ILocator handle)
    {
        var box = (await handle.BoundingBoxAsync())!;
        return box.X + box.Width / 2;
    }

    private static async Task<float> CenterYAsync(ILocator handle)
    {
        var box = (await handle.BoundingBoxAsync())!;
        return box.Y + box.Height / 2;
    }

    // Presses the mouse on the middle of a handle and keeps it down.
    private static async Task PressAsync(IPage page, ILocator handle)
    {
        await page.Mouse.MoveAsync(await CenterXAsync(handle), await CenterYAsync(handle));
        await page.Mouse.DownAsync();
    }

    // Lets go of the mouse and waits until the app has the window's box.
    private static Task ReleaseAsync(IPage page) => ChangeAsync(page, () => page.Mouse.UpAsync());

    // Does something the dock counts as one finished action, and waits for the count.
    private static async Task ChangeAsync(IPage page, Func<Task> act)
    {
        var dock = page.Locator(".tb-dock");
        var commits = int.Parse((await dock.GetAttributeAsync("data-commits"))!, CultureInfo.InvariantCulture);
        await act();
        await Expect(dock).ToHaveAttributeAsync("data-commits", (commits + 1).ToString(CultureInfo.InvariantCulture));
    }
}
