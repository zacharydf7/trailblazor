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
}
