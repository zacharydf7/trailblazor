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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldKeepItsSlidOutPanelAgainstItsEdge_WhenTheEmptyPartOfItsRowIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        await page.Tb().Dock().RevealAsync("Gear");
        var row = page.Locator(".tb-dock__group--revealed");
        var before = (await row.BoundingBoxAsync())!;
        var (x, y) = (before.X + before.Width * 0.6f, before.Y + before.Height / 2);

        // A slid-out panel is drawn as a window, but the empty part of its row doesn't move it as a window's does.
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + 60, y + 40, new() { Steps = 4 });

        var during = (await row.BoundingBoxAsync())!;
        Assert.Equal((before.X, before.Y), (during.X, during.Y));
        await page.Mouse.UpAsync();
        await Expect(row).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SlideSeparator_ShouldResizeTheSlidOutPanelWithinItsLimits_WhenItIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        await dock.RevealAsync("Gear");
        var row = page.Locator(".tb-dock__group--revealed");
        var edge = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Gear", Exact = true });
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var inner = area.Width - 2;
        var before = (await row.BoundingBoxAsync())!;
        // The edge lies where the panel's box ends, a quarter of the area from the start, nine pixels wide.
        var line = (await edge.BoundingBoxAsync())!;
        Assert.Equal(before.X + before.Width, line.X + line.Width / 2, 1.0);
        Assert.Equal(9, line.Width, 0.01);
        var color = () => edge.EvaluateAsync<string>("line => getComputedStyle(line, '::before').backgroundColor");
        var plain = await color();

        // A tenth of the area further in. The panel follows at once, and stays out while its edge is pressed.
        var (x, y) = await PressLineAsync(page, edge);
        await page.Mouse.MoveAsync(x + inner / 10, y, new() { Steps = 4 });
        // The drag selects none of the text it crosses.
        Assert.Equal("", await page.EvaluateAsync<string>("() => getSelection().toString()"));
        Assert.Equal(inner * 0.35, (await row.BoundingBoxAsync())!.Width, 1.5);
        Assert.Equal(inner * 0.35, (await Panel(page, "Gear").BoundingBoxAsync())!.Width, 1.5);
        await ReleaseAsync(page);

        Assert.Equal(35, await dock.SizeAsync("Resize Gear"));
        await Expect(page.GetByTestId("layout")).ToContainTextAsync(
            "{\"panel\":\"gear\",\"edge\":\"start\",\"size\":35");
        await Expect(row).ToHaveCountAsync(1);
        // The press gave the edge the focus and marked it, and once the pointer leaves, its line has none of keyboard
        // focus's highlight. A key on the edge takes the mark away.
        await Expect(edge).ToBeFocusedAsync();
        await Expect(edge).ToHaveAttributeAsync("data-pointer-focus", "");
        await page.Mouse.MoveAsync(area.X + inner * 0.8f, y);
        await WaitForFadesAsync(page);
        Assert.Equal(plain, await color());
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(edge).Not.ToHaveAttributeAsync("data-pointer-focus", "");

        // It stops at nine tenths of the area, also while the drag runs, and at a tenth of it.
        await PressLineAsync(page, edge);
        await page.Mouse.MoveAsync(1200, 400, new() { Steps = 4 });
        Assert.Equal(inner * 0.9, (await row.BoundingBoxAsync())!.Width, 1.5);
        await ReleaseAsync(page);
        Assert.Equal(90, await dock.SizeAsync("Resize Gear"));
        await PressLineAsync(page, edge);
        await page.Mouse.MoveAsync(area.X + 2, 400, new() { Steps = 4 });
        await ReleaseAsync(page);
        Assert.Equal(10, await dock.SizeAsync("Resize Gear"));
        // The mark from that press goes when the focus leaves the edge.
        await Expect(edge).ToHaveAttributeAsync("data-pointer-focus", "");
        await Tab(page, "Gear").FocusAsync();
        await Expect(edge).Not.ToHaveAttributeAsync("data-pointer-focus", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SlideSeparator_ShouldKeepThePanelAgainstItsStrip_WhenTheEdgeOfAnEndOrBottomPanelIsDragged(
        string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        await dock.AutoHideAsync("Notes", DockPosition.End);
        await dock.RevealAsync("Notes");
        var row = page.Locator(".tb-dock__group--revealed");
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var (width, height) = (area.Width - 2, area.Height - 2);

        // The end strip's panel grows toward the start: its far side stays at the area's end while the drag runs.
        var edge = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Notes", Exact = true });
        var line = (await edge.BoundingBoxAsync())!;
        Assert.Equal((await row.BoundingBoxAsync())!.X, line.X + line.Width / 2, 1.0);
        var (x, y) = await PressLineAsync(page, edge);
        await page.Mouse.MoveAsync(x - width / 10, y, new() { Steps = 4 });
        var during = (await row.BoundingBoxAsync())!;
        Assert.Equal(width * 0.35, during.Width, 1.5);
        Assert.Equal(area.X + area.Width - 1, during.X + during.Width, 1.0);
        await ReleaseAsync(page);
        Assert.Equal(35, await dock.SizeAsync("Resize Notes"));

        // The bottom strip's panel grows upward, and its far side stays at the area's bottom.
        await dock.RevealAsync("Weather");
        edge = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Weather", Exact = true });
        (x, y) = await PressLineAsync(page, edge);
        await page.Mouse.MoveAsync(x, y - height / 10, new() { Steps = 4 });
        during = (await row.BoundingBoxAsync())!;
        var panel = (await Panel(page, "Weather").BoundingBoxAsync())!;
        Assert.Equal(area.Y + 1 + height * 0.65, during.Y, 1.5);
        Assert.Equal(area.Y + area.Height - 1, panel.Y + panel.Height, 1.0);
        await ReleaseAsync(page);
        Assert.Equal(35, await dock.SizeAsync("Resize Weather"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SlideSeparator_ShouldLieUnderThePanelAndDragByItsGrip_WhenThePanelSlidOutFromTheTop(
        string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        await dock.AutoHideAsync("Notes", DockPosition.Top);
        await dock.RevealAsync("Notes");
        var edge = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Notes", Exact = true });
        var button = page.GetByRole(AriaRole.Button, new() { Name = "Size of Notes", Exact = true });
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var height = area.Height - 2;

        // The edge lies along the bottom of the panel, a quarter of the area down and nine pixels deep around its
        // line, with its grip on its middle.
        var panel = (await Panel(page, "Notes").BoundingBoxAsync())!;
        var line = (await edge.BoundingBoxAsync())!;
        var grip = (await button.BoundingBoxAsync())!;
        Assert.Equal(area.Y + 1 + height * 0.25, panel.Y + panel.Height, 1.0);
        Assert.Equal(panel.Y + panel.Height, line.Y + line.Height / 2, 1.0);
        Assert.Equal(9, line.Height, 0.01);
        Assert.Equal(panel.Width, line.Width, 1.0);
        var (x, y) = (grip.X + grip.Width / 2, grip.Y + grip.Height / 2);
        Assert.Equal(line.X + line.Width / 2, x, 1.0);
        Assert.Equal(line.Y + line.Height / 2, y, 1.0);

        // A drag that starts on the grip resizes as one on the line does, and the grip's menu stays shut.
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x, y + height / 10, new() { Steps = 4 });
        var during = (await Panel(page, "Notes").BoundingBoxAsync())!;
        Assert.Equal(area.Y + 1 + height * 0.35, during.Y + during.Height, 1.5);
        await ReleaseAsync(page);

        Assert.Equal(35, await dock.SizeAsync("Resize Notes"));
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Make Notes larger" })).ToBeHiddenAsync();
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

    // Presses the mouse on a slid-out panel's edge a quarter of the way along it, clear of the grip on its middle.
    private static async Task<(float X, float Y)> PressLineAsync(IPage page, ILocator edge)
    {
        var box = (await edge.BoundingBoxAsync())!;
        var (x, y) = box.Height > box.Width ? (box.X + box.Width / 2, box.Y + box.Height / 4)
            : (box.X + box.Width / 4, box.Y + box.Height / 2);
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        return (x, y);
    }

    // Lets go of the mouse and waits until the dock counts the drag.
    private static Task ReleaseAsync(IPage page) => ChangeAsync(page, () => page.Mouse.UpAsync());
}
