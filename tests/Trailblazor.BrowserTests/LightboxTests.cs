using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class LightboxTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Reduced motion opens the dialog at once, so it is measured where it rests.
    private static readonly BrowserNewContextOptions Still = new() { ReducedMotion = ReducedMotion.Reduce };

    private const string First = "Eagle Ridge above the lake at noon";

    private static ILocator Thumbnail(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static ILocator Part(IPage page, string part) => page.Locator($"dialog.tb-lightbox [data-part={part}]");

    private async Task<IPage> OpenLightboxAsync(string browser, string thumbnail = First,
        BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/examples/lightbox", options ?? Still);
        await Thumbnail(page, thumbnail).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Trail photos" })).ToBeVisibleAsync();
        return page;
    }

    // How the shown image's frame sits in the stage's content box: its size, the stage's, and whether it's inside.
    private static async Task<double[]> FitAsync(IPage page)
    {
        await Expect(Part(page, "frame")).ToHaveAttributeAsync("data-measured", "");
        return await Part(page, "stage").EvaluateAsync<double[]>("""
            stage => {
                const style = getComputedStyle(stage);
                const width = stage.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
                const height = stage.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom);
                const frame = stage.querySelector('[data-part=frame]').getBoundingClientRect();
                return [frame.width, frame.height, width, height];
            }
            """);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lightbox_ShouldFitTheImageToTheWindow_WhenShownOrTurned(string browser)
    {
        var page = await OpenLightboxAsync(browser);

        // Whether the image is wider or taller than the stage's shape, it fills one side and stays inside the other.
        var (fit, turned, tall) = (await FitAsync(page), (double[]?)null, (double[]?)null);
        await page.Keyboard.PressAsync("r");
        await Expect(Part(page, "stage")).ToHaveAttributeAsync("data-turns", "1");
        turned = await FitAsync(page);
        await page.Tb().Lightbox().PreviousAsync();
        tall = await FitAsync(page);

        foreach (var (box, ratio) in new[] { (fit, 1.5), (turned, 2 / 3d), (tall, 2 / 3d) })
        {
            Assert.InRange(box[0] / box[1], ratio - 0.01, ratio + 0.01);
            Assert.True(box[0] <= box[2] + 1 && box[1] <= box[3] + 1, $"{box[0]}x{box[1]} overflows {box[2]}x{box[3]}");
            Assert.True(Math.Abs(box[0] - box[2]) < 1 || Math.Abs(box[1] - box[3]) < 1,
                $"{box[0]}x{box[1]} doesn't fill {box[2]}x{box[3]} on either side");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lightbox_ShouldMoveBetweenImagesWithTheArrowKeys_WhenFocusIsInIt(string browser)
    {
        var page = await OpenLightboxAsync(browser);
        var lightbox = page.Tb().Lightbox("Trail photos");

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Part(page, "frame")).ToHaveAttributeAsync("data-index", "1");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Part(page, "frame")).ToHaveAttributeAsync("data-index", "0");
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(Part(page, "frame")).ToHaveAttributeAsync("data-index", "1");
        Assert.Equal(("Cedar Flats at dusk", null), (await lightbox.AltAsync(), await lightbox.CaptionAsync()));
        await Expect(Part(page, "status")).ToHaveTextAsync("Cedar Flats at dusk, 2 of 3");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lightbox_ShouldZoomAndTurnWithKeys_WhenFocusIsInIt(string browser)
    {
        var page = await OpenLightboxAsync(browser);
        var stage = Part(page, "stage");

        await page.Keyboard.PressAsync("+");
        await Expect(stage).ToHaveAttributeAsync("data-zoom", "1.5");
        await page.Keyboard.PressAsync("Shift+R");
        await Expect(stage).ToHaveAttributeAsync("data-turns", "3");
        await page.Keyboard.PressAsync("-");
        await Expect(stage).ToHaveAttributeAsync("data-zoom", "1");
        await page.Keyboard.PressAsync("+");
        await Expect(stage).ToHaveAttributeAsync("data-zoom", "1.5");
        await page.Keyboard.PressAsync("0");

        await Expect(stage).ToHaveAttributeAsync("data-zoom", "1");
        await Expect(stage).ToHaveAttributeAsync("data-turns", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lightbox_ShouldPanAZoomedImage_WhenItsStageHasFocus(string browser)
    {
        var page = await OpenLightboxAsync(browser);
        var stage = Part(page, "stage");
        await page.Tb().Lightbox().ZoomInAsync();
        await page.Tb().Lightbox().ZoomInAsync();
        await stage.FocusAsync();
        var before = await stage.EvaluateAsync<double>("stage => stage.scrollLeft");

        await page.Keyboard.PressAsync("ArrowRight");

        await page.WaitForFunctionAsync(
            "before => document.querySelector('dialog.tb-lightbox [data-part=stage]').scrollLeft > before", before);
        await Expect(Part(page, "frame")).ToHaveAttributeAsync("data-index", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lightbox_ShouldKeepFocusOnTheStage_WhenItsImageIsZoomedBackOut(string browser)
    {
        var page = await OpenLightboxAsync(browser);
        var stage = Part(page, "stage");
        await page.Keyboard.PressAsync("+");
        await Expect(stage).ToHaveAttributeAsync("data-zoom", "1.5");
        await stage.FocusAsync();

        await page.Keyboard.PressAsync("-");
        await Expect(stage).ToHaveAttributeAsync("data-zoom", "1");

        await Expect(stage).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("+");
        await Expect(stage).ToHaveAttributeAsync("data-zoom", "1.5");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lightbox_ShouldPanAZoomedImage_WhenDraggedWithTheMouse(string browser)
    {
        var page = await OpenLightboxAsync(browser);
        var stage = Part(page, "stage");
        await page.Tb().Lightbox().ZoomInAsync();
        await page.Tb().Lightbox().ZoomInAsync();
        // Zooming in keeps the middle of the image in view, so it starts scrolled partway.
        var start = await stage.EvaluateAsync<double[]>("stage => [stage.scrollLeft, stage.scrollTop]");
        Assert.True(start[0] > 0 && start[1] > 0, $"Zooming didn't keep the middle in view: {start[0]}, {start[1]}");
        var box = (await stage.BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x - 100, y - 50, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        var end = await stage.EvaluateAsync<double[]>("stage => [stage.scrollLeft, stage.scrollTop]");
        Assert.InRange(end[0] - start[0], 99, 101);
        Assert.InRange(end[1] - start[1], 49, 51);
    }

    // Only Chromium can drive touch from Playwright.
    [Fact]
    public async Task Lightbox_ShouldMoveBetweenImages_WhenSwiped()
    {
        var page = await OpenLightboxAsync("chromium",
            options: new() { HasTouch = true, ReducedMotion = ReducedMotion.Reduce });
        var box = (await Part(page, "stage").BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);
        var touch = await page.Context.NewCDPSessionAsync(page);

        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", x - 120, y + 5);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null);

        await Expect(Part(page, "frame")).ToHaveAttributeAsync("data-index", "1");
    }

    // A touch the browser takes over, such as a scroll or a pinch, ends in a cancel and leaves the next swipe alone.
    [Fact]
    public async Task Lightbox_ShouldMoveOneImage_WhenSwipedAfterACanceledTouch()
    {
        var page = await OpenLightboxAsync("chromium",
            options: new() { HasTouch = true, ReducedMotion = ReducedMotion.Reduce });
        var box = (await Part(page, "stage").BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);
        var touch = await page.Context.NewCDPSessionAsync(page);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchCancel", null, null);

        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", x - 120, y + 5);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null);

        await Expect(Part(page, "frame")).ToHaveAttributeAsync("data-index", "1");
        await page.WaitForTimeoutAsync(300);
        await Expect(Part(page, "frame")).ToHaveAttributeAsync("data-index", "1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lightbox_ShouldGiveFocusBack_WhenClosedWithEscape(string browser)
    {
        var page = await OpenLightboxAsync(browser, "Cedar Flats at dusk");
        await Expect(Part(page, "frame")).ToHaveAttributeAsync("data-index", "1");

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await Expect(Thumbnail(page, "Cedar Flats at dusk")).ToBeFocusedAsync();
    }

    // The script watches data-zoom across the page, which other elements, such as a viewport, carry too.
    [Fact]
    public async Task Lightbox_ShouldLeaveAnotherElementsScroll_WhenItsDataZoomChanges()
    {
        var page = await OpenAsync("chromium", "/examples/lightbox");

        var scrolled = await page.EvaluateAsync<double>("""
            async () => {
                const box = document.createElement('div');
                box.style.cssText = 'overflow: auto; inline-size: 100px; block-size: 100px';
                box.innerHTML = '<div style="inline-size: 1000px; block-size: 1000px"></div>';
                document.body.append(box);
                box.dataset.zoom = '1';
                await new Promise(requestAnimationFrame);
                box.dataset.zoom = '2';
                await new Promise(requestAnimationFrame);
                return box.scrollLeft + box.scrollTop;
            }
            """);

        Assert.Equal(0, scrolled);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldPressTheButtonsAndReadTheImage_WhenFoundByTitle(string browser)
    {
        var page = await OpenLightboxAsync(browser);
        var lightbox = page.Tb().Lightbox("Trail photos");

        await lightbox.NextAsync();
        await lightbox.ZoomInAsync();
        await lightbox.RotateLeftAsync();
        Assert.Equal((1, 1.5, 270), (await lightbox.IndexAsync(), await lightbox.ZoomAsync(),
            await lightbox.RotationAsync()));
        await lightbox.FitAsync();
        await lightbox.CloseAsync();

        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
    }
}
