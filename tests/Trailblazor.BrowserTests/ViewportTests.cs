using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class ViewportTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldZoomInOutAndBackToActualSize_WhenPressedOnTheBox(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Map(page).Surface.FocusAsync();

        await page.Keyboard.PressAsync("+");
        await Expect(Readout(page, "Zoom")).ToHaveTextAsync("1.25");
        await page.Keyboard.PressAsync("-");
        await Expect(Readout(page, "Zoom")).ToHaveTextAsync("1");
        await page.Keyboard.PressAsync("-");
        await Expect(Readout(page, "Zoom")).ToHaveTextAsync("0.8");
        await page.Keyboard.PressAsync("0");

        await Expect(Readout(page, "Zoom")).ToHaveTextAsync("1");
        Assert.Equal(1, await Map(page).ZoomAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Wheel_ShouldZoomAroundThePointer_WhenCtrlIsHeld(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var marker = page.Locator("#marker");
        var before = (await marker.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(before.X + 5, before.Y + 5);

        await page.Keyboard.DownAsync("Control");
        await page.Mouse.WheelAsync(0, -200);
        await page.Keyboard.UpAsync("Control");

        await Expect(Readout(page, "Zoom")).Not.ToHaveTextAsync("1");
        Assert.True(await Map(page).ZoomAsync() > 1);
        var after = (await marker.BoundingBoxAsync())!;
        Assert.Equal(before.X + 5, after.X + after.Width / 2, tolerance: 2);
        Assert.Equal(before.Y + 5, after.Y + after.Height / 2, tolerance: 2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drag_ShouldPanTheContent_WhenTheMouseDragsTheBox(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var surface = Map(page).Surface;
        var box = (await surface.BoundingBoxAsync())!;
        var (left, top) = await ScrollAsync(surface);

        await page.Mouse.MoveAsync(box.X + 200, box.Y + 150);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + 100, box.Y + 100, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        Assert.Equal((left + 100, top + 50), await ScrollAsync(surface));
        await Expect(surface).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Hover_ShouldLeaveTheContent_WhenAPressWasLetGoOutsideTheBoxBeforeItPanned(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var surface = Map(page).Surface;
        var box = (await surface.BoundingBoxAsync())!;
        var before = await ScrollAsync(surface);

        await page.Mouse.MoveAsync(box.X + box.Width - 30, box.Y + 40);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width + 40, box.Y + 40);
        await page.Mouse.UpAsync();
        await page.Mouse.MoveAsync(box.X + 100, box.Y + 100, new() { Steps = 5 });

        Assert.Equal(before, await ScrollAsync(surface));
        await Expect(page.Locator("[data-part=viewport]")).Not.ToHaveAttributeAsync("data-panning", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drag_ShouldPanTheContent_WhenItStartsOnAnImage(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var image = (await page.Locator("#photo").BoundingBoxAsync())!;
        var (left, top) = await ScrollAsync(Map(page).Surface);

        await page.Mouse.MoveAsync(image.X + 60, image.Y + 45);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(image.X + 20, image.Y + 15, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        Assert.Equal((left + 40, top + 30), await ScrollAsync(Map(page).Surface));
        // A pen or a finger could start the image's own drag, which the viewport cancels.
        Assert.True(await page.Locator("#photo").EvaluateAsync<bool>("""
            image => {
                const drag = new DragEvent('dragstart', { bubbles: true, cancelable: true });
                image.dispatchEvent(drag);
                return drag.defaultPrevented;
            }
            """));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Wheel_ShouldScrollWithoutZooming_WhenCtrlIsNotHeld(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var box = (await Map(page).Surface.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + 100, box.Y + 100);

        await page.Mouse.WheelAsync(0, 100);

        await Expect(Map(page).Surface).Not.ToHaveJSPropertyAsync("scrollTop", 0);
        Assert.Equal(1, await Map(page).ZoomAsync());
        Assert.Equal("1", await Readout(page, "Zoom").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Click_ShouldReachAButtonInTheContent_WhenItIsPressed(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Summit" }).ClickAsync();

        await Expect(Readout(page, "Clicks")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Click_ShouldReachContentThatCantTakeFocus_WhenItIsPressedWithoutDragging(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.Locator("#marker").ClickAsync();

        await Expect(Readout(page, "Marker clicks")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Fit_ShouldFitTheContentAndFollowTheBox_WhenPickedAndTheBoxResizes(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var map = Map(page);

        await map.FitAsync(ViewportFit.Page);
        Assert.Equal(await FitAsync(map.Surface, "Page"), await map.ZoomAsync(), tolerance: 0.001);
        await map.FitAsync(ViewportFit.Width);
        Assert.Equal(await FitAsync(map.Surface, "Width"), await map.ZoomAsync(), tolerance: 0.001);
        Assert.Equal(ViewportFit.Width, await map.FitAsync());
        var fitted = await Readout(page, "Zoom").TextContentAsync();
        await page.Locator("#frame").EvaluateAsync("frame => frame.style.inlineSize = '600px'");

        await Expect(Readout(page, "Zoom")).Not.ToHaveTextAsync(fitted!);
        Assert.Equal(await FitAsync(map.Surface, "Width"), await map.ZoomAsync(), tolerance: 0.001);
        await Expect(Readout(page, "Fit")).ToHaveTextAsync("Width");
        await map.ZoomInAsync();
        await Expect(Readout(page, "Fit")).ToHaveTextAsync("None");
        await map.ActualSizeAsync();
        await Expect(Readout(page, "Zoom")).ToHaveTextAsync("1");
    }

    [Fact]
    public async Task Pinch_ShouldZoomTheContent_WhenTwoFingersSpreadApart()
    {
        var page = await OpenReadyAsync("chromium", new() { HasTouch = true });
        var box = (await Map(page).Surface.BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);
        var touch = await page.Context.NewCDPSessionAsync(page);

        await PinchAsync(touch, "touchStart", x, y, 20);
        await PinchAsync(touch, "touchMove", x, y, 40);
        await PinchAsync(touch, "touchMove", x, y, 60);
        await touch.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchEnd", ["touchPoints"] = Array.Empty<object>(),
        });

        await Expect(Readout(page, "Zoom")).ToHaveTextAsync("3");
    }

    // Two fingers apart by twice the spread, on either side of the point.
    private static Task PinchAsync(ICDPSession session, string type, double x, double y, double spread) =>
        session.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["touchPoints"] = new object[] { new { x = x - spread, y, id = 1 }, new { x = x + spread, y, id = 2 } },
        });

    private static Task<double> FitAsync(ILocator surface, string fit) => surface.EvaluateAsync<double>(
        "(box, fit) => fit === 'Width' ? box.clientWidth / 800 : Math.min(box.clientWidth / 800, box.clientHeight / 600)",
        fit);

    private static async Task<(double Left, double Top)> ScrollAsync(ILocator surface)
    {
        var scroll = await surface.EvaluateAsync<double[]>("box => [box.scrollLeft, box.scrollTop]");
        return (scroll[0], scroll[1]);
    }

    private static TbViewportLocator Map(IPage page) => page.Tb().Viewport("Trail map");

    private static ILocator Readout(IPage page, string name) =>
        page.GetByRole(AriaRole.Status, new() { Name = name, Exact = true });

    private async Task<IPage> OpenReadyAsync(string browser, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/viewport", options);
        await Expect(page.Locator("[data-part=viewport]")).ToHaveAttributeAsync("data-ready", "");
        return page;
    }
}
