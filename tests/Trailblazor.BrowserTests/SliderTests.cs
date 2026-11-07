namespace Trailblazor.BrowserTests;

public class SliderTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Slider(IPage page, string name) =>
        page.GetByRole(AriaRole.Slider, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slider_ShouldStepJumpAndReachTheEnds_WhenTheKeysAreUsed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider");
        var brightness = Slider(page, "Map brightness");
        await brightness.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(brightness).ToHaveValueAsync("61");
        await page.Keyboard.PressAsync("Home");
        await Expect(brightness).ToHaveValueAsync("0");
        await page.Keyboard.PressAsync("End");

        await Expect(brightness).ToHaveValueAsync("100");
        await Expect(brightness).ToHaveAttributeAsync("aria-valuetext", "100%");
        await Expect(page.GetByText("Saved at")).ToHaveTextAsync("Saved at 100%");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slider_ShouldMoveItsThumbAndFillToThePointer_WhenTheTrackIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider");
        var grade = Slider(page, "Steepest grade");
        var box = (await grade.BoundingBoxAsync())!;

        await page.Mouse.ClickAsync(box.X + box.Width - 2, box.Y + box.Height / 2);

        // The fill moves in the render that sets aria-valuetext.
        await Expect(grade).ToHaveAttributeAsync("aria-valuetext", "30");
        var fill = page.Locator(".tb-slider").Filter(new() { Has = grade }).Locator("[data-part=fill]");
        var track = page.Locator(".tb-slider").Filter(new() { Has = grade }).Locator("[data-part=track]");
        Assert.True((await fill.BoundingBoxAsync())!.Width > (await track.BoundingBoxAsync())!.Width * 0.9);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldMoveEachThumbAndStopItAtTheOther_WhenUsedWithTheKeys(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider");
        var minimum = Slider(page, "Elevation Minimum");
        var maximum = Slider(page, "Elevation Maximum");

        await minimum.FocusAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(minimum).ToHaveValueAsync("2400");
        await page.Keyboard.PressAsync("Tab");
        await Expect(maximum).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");

        await Expect(maximum).ToHaveValueAsync("2400");
        await Expect(page.GetByText("Between")).ToHaveTextAsync("Between 2400 and 2400 m");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slider_ShouldBeSetByFill_WhenATestFillsIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider");

        await Slider(page, "Map brightness").FillAsync("80");

        await Expect(page.Locator(".tb-slider [data-part=value]").First).ToHaveTextAsync("80%");
        await Expect(page.GetByText("Saved at")).ToHaveTextAsync("Saved at 80%");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slider_ShouldPostItsValues_WhenUsedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-slider");

        await Slider(page, "Brightness").FillAsync("70");
        await Slider(page, "Elevation Maximum").FillAsync("3000");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("70 | 800-3000");
        await Expect(Slider(page, "Elevation Maximum")).ToHaveValueAsync("3000");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slider_ShouldDrawItsTrackAndFill_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider", new() { ForcedColors = ForcedColors.Active });

        // Forced colors turn an author background into the page's own, so the parts are compared with system colors.
        var colors = await page.EvaluateAsync<string[]>("""
            () => {
                const probe = color => {
                    const element = document.body.appendChild(document.createElement('div'));
                    element.style.cssText = `forced-color-adjust: none; background: ${color}`;
                    return getComputedStyle(element).backgroundColor;
                };
                const slider = document.querySelector('.tb-slider');
                return [probe('CanvasText'), probe('Highlight'),
                    getComputedStyle(slider.querySelector('[data-part=track]')).backgroundColor,
                    getComputedStyle(slider.querySelector('[data-part=fill]')).backgroundColor];
            }
            """);

        Assert.Equal(colors[..2], colors[2..]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slider_ShouldFillFromTheRightAndCenterItsValue_WhenTheTextIsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider");
        var slider = page.Locator(".tb-slider").Filter(new() { Has = Slider(page, "Trail length") });

        var track = (await slider.Locator("[data-part=track]").BoundingBoxAsync())!;
        var fill = (await slider.Locator("[data-part=fill]").BoundingBoxAsync())!;
        var bubble = (await slider.Locator("[data-part=value]").BoundingBoxAsync())!;
        var thumb = track.X + track.Width - 9 - (track.Width - 18) * 0.3;

        Assert.Equal(track.X + track.Width, fill.X + fill.Width, tolerance: 1);
        Assert.Equal(thumb, bubble.X + bubble.Width / 2, tolerance: 2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slider_ShouldWrapItsLabel_WhenTheLabelIsLongerThanTheSliderIsWide(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider");

        var longLabel = await page.GetByText("How far from the trailhead").BoundingBoxAsync();
        var shortLabel = await page.GetByText("Steepest grade").BoundingBoxAsync();

        Assert.True(longLabel!.Height > shortLabel!.Height * 1.5, $"The label is {longLabel.Height}px tall.");
    }
}

// Dragging a thumb captures the pointer, which Firefox can leak into parallel pages, so it runs with the other drags.
[Collection(nameof(PointerDrags))]
public class SliderDragTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldLetEachThumbBeDragged_WhenTheyShareATrack(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider");
        var minimum = page.GetByRole(AriaRole.Slider, new() { Name = "Elevation Minimum", Exact = true });
        var box = (await minimum.BoundingBoxAsync())!;

        // The start thumb sits at 800 of 4000; dragging it to the track's start takes it to 0.
        var thumb = box.X + 9 + (box.Width - 18) * 0.2f;
        await page.Mouse.MoveAsync(thumb, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X, box.Y + box.Height / 2, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        await Expect(minimum).ToHaveValueAsync("0");
        await Expect(page.GetByRole(AriaRole.Slider, new() { Name = "Elevation Maximum", Exact = true }))
            .ToHaveValueAsync("2400");
    }

    private static ILocator Thumb(IPage page, string slider, string thumb) =>
        page.GetByRole(AriaRole.Slider, new() { Name = $"{slider} {thumb}", Exact = true });

    // Where a value of 0 to 100 sits on the track, allowing for the fixture's 18px thumbs, at the track's mid-height.
    private static async Task<(float X, float Y)> PointAsync(IPage page, string slider, int value)
    {
        var box = (await Thumb(page, slider, "Minimum").BoundingBoxAsync())!;
        return (box.X + 9 + (box.Width - 18) * value / 100, box.Y + box.Height / 2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldMoveTheNearerThumbToThePointer_WhenTheTrackIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");
        var (low, y) = await PointAsync(page, "Parted", 30);
        var (high, _) = await PointAsync(page, "Parted", 70);

        await page.Mouse.ClickAsync(low, y);
        await Expect(Thumb(page, "Parted", "Minimum")).ToHaveValueAsync("30");
        await Expect(Thumb(page, "Parted", "Minimum")).ToBeFocusedAsync();
        await page.Mouse.ClickAsync(high, y);

        await Expect(Thumb(page, "Parted", "Maximum")).ToHaveValueAsync("70");
        await Expect(Thumb(page, "Parted", "Maximum")).ToBeFocusedAsync();
        await Expect(Thumb(page, "Parted", "Minimum")).ToHaveValueAsync("30");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldHoldBackTheRingUntilAKey_WhenTheTrackIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");
        var (x, y) = await PointAsync(page, "Parted", 30);
        var thumb = Thumb(page, "Parted", "Minimum");

        await page.Mouse.ClickAsync(x, y);
        await Expect(thumb).ToBeFocusedAsync();
        var withoutRing = await thumb.ScreenshotAsync();

        // Shift moves nothing, so the two pictures differ only by the ring.
        await page.Keyboard.PressAsync("Shift");
        await Expect(thumb).Not.ToHaveAttributeAsync("data-pointer-focus", "");

        Assert.True(await thumb.EvaluateAsync<bool>("thumb => thumb.matches(':focus-visible')"));
        Assert.NotEqual(withoutRing, await thumb.ScreenshotAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldStopHoldingBackTheRing_WhenFocusLeavesTheThumb(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");
        var (x, y) = await PointAsync(page, "Parted", 30);
        var thumb = Thumb(page, "Parted", "Minimum");
        await page.Mouse.ClickAsync(x, y);
        await Expect(thumb).ToHaveAttributeAsync("data-pointer-focus", "");

        await page.Mouse.ClickAsync(1, 1);

        await Expect(thumb).Not.ToBeFocusedAsync();
        await Expect(thumb).Not.ToHaveAttributeAsync("data-pointer-focus", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldMoveTheNearerThumb_WhenTheTrackIsClickedInRightToLeftText(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");
        var box = (await Thumb(page, "Flipped", "Minimum").BoundingBoxAsync())!;

        await page.Mouse.ClickAsync(box.X + box.Width - 9 - (box.Width - 18) * 0.3f, box.Y + box.Height / 2);

        await Expect(Thumb(page, "Flipped", "Minimum")).ToHaveValueAsync("30");
        await Expect(Thumb(page, "Flipped", "Maximum")).ToHaveValueAsync("80");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldFollowThePointer_WhenATrackPressIsHeldAndDragged(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");
        var (from, y) = await PointAsync(page, "Parted", 30);
        var (past, _) = await PointAsync(page, "Parted", 95);

        await page.Mouse.MoveAsync(from, y);
        await page.Mouse.DownAsync();
        await Expect(Thumb(page, "Parted", "Minimum")).ToHaveValueAsync("30");
        await page.Mouse.MoveAsync(past, y, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        // It stops at the other thumb.
        await Expect(Thumb(page, "Parted", "Minimum")).ToHaveValueAsync("80");
        await Expect(Thumb(page, "Parted", "Maximum")).ToHaveValueAsync("80");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldPartItsThumbsTheWayThePointerGoes_WhenTheyShareAValue(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");

        foreach (var (slider, shared, to, minimum, maximum) in new[]
        {
            ("Middle", 50, 20, "20", "50"), ("Rising", 50, 80, "50", "80"), ("Top", 100, 70, "70", "100"),
            ("Bottom", 0, 30, "0", "30"),
        })
        {
            var (from, y) = await PointAsync(page, slider, shared);
            var (target, _) = await PointAsync(page, slider, to);

            await page.Mouse.MoveAsync(from, y);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(target, y, new() { Steps = 5 });
            await page.Mouse.UpAsync();

            await Expect(Thumb(page, slider, "Minimum")).ToHaveValueAsync(minimum);
            await Expect(Thumb(page, slider, "Maximum")).ToHaveValueAsync(maximum);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldChangeNothing_WhenAPressOnSharedThumbsDoesntMove(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");
        var (x, y) = await PointAsync(page, "Middle", 50);

        await page.Mouse.ClickAsync(x + 3, y);

        await Expect(Thumb(page, "Middle", "Maximum")).ToBeFocusedAsync();
        await Expect(Thumb(page, "Middle", "Minimum")).ToHaveValueAsync("50");
        await Expect(Thumb(page, "Middle", "Maximum")).ToHaveValueAsync("50");
        await Expect(page.Locator("output")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldCommitOnce_WhenSharedThumbsAreDraggedApart(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");
        var (from, y) = await PointAsync(page, "Middle", 50);
        var (target, _) = await PointAsync(page, "Middle", 20);

        await page.Mouse.MoveAsync(from, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(target, y, new() { Steps = 5 });
        await Expect(Thumb(page, "Middle", "Minimum")).ToHaveValueAsync("20");
        await Expect(page.Locator("output")).ToHaveTextAsync("0");
        await page.Mouse.UpAsync();

        await Expect(page.Locator("output")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldChangeNothing_WhenItsTrackIsPressedWhileReadOnlyOrDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");
        var (lockedX, lockedY) = await PointAsync(page, "Locked", 40);
        var (offX, offY) = await PointAsync(page, "Off", 40);
        var (fencedX, fencedY) = await PointAsync(page, "Fenced", 40);

        await page.Mouse.ClickAsync(lockedX, lockedY);
        await page.Mouse.ClickAsync(offX, offY);
        await page.Mouse.ClickAsync(fencedX, fencedY);

        await Expect(Thumb(page, "Locked", "Minimum")).ToHaveValueAsync("20");
        await Expect(Thumb(page, "Off", "Minimum")).ToHaveValueAsync("20");
        await Expect(Thumb(page, "Fenced", "Minimum")).ToHaveValueAsync("20");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldMoveTheThumbOnThatSide_WhenTheTrackIsClickedAwayFromSharedThumbs(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");

        foreach (var (slider, to, thumb, other, value, stays) in new[]
        {
            ("Middle", 80, "Maximum", "Minimum", "80", "50"), ("Rising", 20, "Minimum", "Maximum", "20", "50"),
            ("Top", 60, "Minimum", "Maximum", "60", "100"), ("Bottom", 30, "Maximum", "Minimum", "30", "0"),
        })
        {
            var (x, y) = await PointAsync(page, slider, to);

            await page.Mouse.ClickAsync(x, y);

            await Expect(Thumb(page, slider, thumb)).ToHaveValueAsync(value);
            await Expect(Thumb(page, slider, thumb)).ToBeFocusedAsync();
            await Expect(Thumb(page, slider, other)).ToHaveValueAsync(stays);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldLetADragChangeDirection_WhileItsThumbsShareAValue(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");
        var (middle, y) = await PointAsync(page, "Middle", 50);
        var (below, _) = await PointAsync(page, "Middle", 30);
        var (above, _) = await PointAsync(page, "Middle", 80);

        await page.Mouse.MoveAsync(middle, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(below, y, new() { Steps = 3 });
        await Expect(Thumb(page, "Middle", "Minimum")).ToHaveValueAsync("30");
        await Expect(Thumb(page, "Middle", "Minimum")).ToBeFocusedAsync();
        await page.Mouse.MoveAsync(above, y, new() { Steps = 6 });
        await page.Mouse.UpAsync();

        await Expect(Thumb(page, "Middle", "Maximum")).ToBeFocusedAsync();
        await Expect(Thumb(page, "Middle", "Minimum")).ToHaveValueAsync("50");
        await Expect(Thumb(page, "Middle", "Maximum")).ToHaveValueAsync("80");

        // At the bottom, a wobble off the low end can't lock in the start thumb, which has nowhere to go.
        var (bottom, bottomY) = await PointAsync(page, "Bottom", 0);
        var (up, _) = await PointAsync(page, "Bottom", 40);
        await page.Mouse.MoveAsync(bottom, bottomY);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(bottom - 8, bottomY);
        await page.Mouse.MoveAsync(up, bottomY, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        await Expect(Thumb(page, "Bottom", "Maximum")).ToHaveValueAsync("40");
        await Expect(Thumb(page, "Bottom", "Minimum")).ToHaveValueAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldKeepDraggingAThumb_WhenItMeetsTheOtherAndComesBack(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");
        var (thumb, y) = await PointAsync(page, "Parted", 20);
        var (other, _) = await PointAsync(page, "Parted", 80);
        var (back, _) = await PointAsync(page, "Parted", 40);

        await page.Mouse.MoveAsync(thumb, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(other, y, new() { Steps = 5 });
        await Expect(Thumb(page, "Parted", "Minimum")).ToHaveValueAsync("80");
        await page.Mouse.MoveAsync(back, y, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        await Expect(Thumb(page, "Parted", "Minimum")).ToHaveValueAsync("40");
        await Expect(Thumb(page, "Parted", "Maximum")).ToHaveValueAsync("80");
    }

    [Fact]
    public async Task RangeSlider_ShouldMoveTheNearerThumb_WhenTheTrackIsTapped()
    {
        var page = await OpenAsync("chromium", "/range-slider", new() { HasTouch = true });
        var (x, y) = await PointAsync(page, "Parted", 30);

        await page.Touchscreen.TapAsync(x, y);

        await Expect(Thumb(page, "Parted", "Minimum")).ToHaveValueAsync("30");
        await Expect(Thumb(page, "Parted", "Minimum")).ToBeFocusedAsync();
        await Expect(page.Locator("output")).ToHaveTextAsync("1");
    }

    [Fact]
    public async Task RangeSlider_ShouldFollowAFinger_WhenItSlidesAcrossTheTrack()
    {
        var page = await OpenAsync("chromium", "/range-slider", new() { HasTouch = true });
        var (x, y) = await PointAsync(page, "Parted", 30);
        var (to, _) = await PointAsync(page, "Parted", 60);
        var touch = await page.Context.NewCDPSessionAsync(page);

        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", (x + to) / 2, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", to, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null);

        await Expect(Thumb(page, "Parted", "Minimum")).ToHaveValueAsync("60");
        await Expect(Thumb(page, "Parted", "Minimum")).ToBeFocusedAsync();
        await Expect(page.Locator("output")).ToHaveTextAsync("1");
    }

    [Fact]
    public async Task RangeSlider_ShouldSeparateItsThumbs_WhenAFingerSlidesFromThem()
    {
        var page = await OpenAsync("chromium", "/range-slider", new() { HasTouch = true });
        var (x, y) = await PointAsync(page, "Middle", 50);
        var (to, _) = await PointAsync(page, "Middle", 20);
        var touch = await page.Context.NewCDPSessionAsync(page);

        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", (x + to) / 2, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", to, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null);

        await Expect(Thumb(page, "Middle", "Minimum")).ToHaveValueAsync("20");
        await Expect(Thumb(page, "Middle", "Maximum")).ToHaveValueAsync("50");
    }

    [Fact]
    public async Task RangeSlider_ShouldStayPut_WhenAFingerStartsScrollingOnTheTrack()
    {
        var page = await OpenAsync("chromium", "/range-slider", new() { HasTouch = true });
        var (x, y) = await PointAsync(page, "Parted", 30);
        var touch = await page.Context.NewCDPSessionAsync(page);

        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        foreach (var distance in new[] { 5, 20, 40, 80 })
        {
            await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", x + 2, y - distance);
        }

        await DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null);
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");

        await Expect(Thumb(page, "Parted", "Minimum")).ToHaveValueAsync("20");
        await Expect(page.Locator("output")).ToHaveTextAsync("0");
        Assert.Equal("BODY", await page.EvaluateAsync<string>("() => document.activeElement.tagName"));
    }
}
