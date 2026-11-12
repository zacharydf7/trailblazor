using System.Diagnostics;
using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class ColorPickerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldChooseInTheGivenRow_WhenPickersShareTheirLabel(string browser)
    {
        var page = await OpenAsync(browser, "/repeated-fields");

        await page.Tb().ColorPicker(page.GetByTestId("row-2").GetByLabel("Color")).ChooseAsync("#336699");

        await Expect(page.GetByTestId("rows")).ToHaveTextAsync("- - | - #336699");
    }

    private static ILocator Box(IPage page, string label = "Accent") =>
        page.GetByRole(AriaRole.Textbox, new() { Name = label, Exact = true });

    internal static ILocator Picker(IPage page) => page.GetByRole(AriaRole.Dialog, new() { Name = "Choose a color" });

    internal static ILocator Field(IPage page, string label) =>
        page.Locator(".tb-field").Filter(new() { Has = Box(page, label) });

    private async Task<IPage> OpenPickerAsync(string browser, string label = "Accent")
    {
        var page = await OpenAsync(browser, "/color-picker");
        var trigger = Field(page, label).GetByRole(AriaRole.Button, new() { Name = "Choose a color" });
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");
        await trigger.ClickAsync();
        await Expect(Picker(page)).ToBeVisibleAsync();
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldTakeAShortHexAsTheFullColor_WhenEnterIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/color-picker");

        await Box(page).FillAsync("#C33");
        await Box(page).PressAsync("Enter");

        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("#cc3333");
        await Expect(Box(page)).ToHaveValueAsync("#cc3333");
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldExplainAndKeepTheText_WhenItIsntAColor(string browser)
    {
        var page = await OpenAsync(browser, "/color-picker");

        await Box(page).FillAsync("blue");
        await Box(page).PressAsync("Enter");

        await Expect(Box(page)).ToHaveAccessibleDescriptionAsync("Enter a color as #RRGGBB.");
        await Expect(Box(page)).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Box(page)).ToHaveValueAsync("blue");
        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("#336699");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Swatches_ShouldBeRadiosChosenWithTheArrows_WhenThePickerIsOpen(string browser)
    {
        var page = await OpenPickerAsync(browser);
        var ocean = Picker(page).GetByRole(AriaRole.Radio, new() { Name = "Ocean" });
        await Expect(ocean).ToBeCheckedAsync();

        await ocean.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("#cc3333");
        await Expect(Picker(page).GetByRole(AriaRole.Radio, new() { Name = "Brick" })).ToBeCheckedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sliders_ShouldChangeTheColorAndKeepTheHue_WhenTheBrightnessGoesToBlack(string browser)
    {
        var page = await OpenPickerAsync(browser);
        var hue = Picker(page).GetByRole(AriaRole.Slider, new() { Name = "Hue" });
        await Expect(hue).ToHaveAttributeAsync("aria-valuetext", "210°");

        await Picker(page).GetByRole(AriaRole.Slider, new() { Name = "Brightness" }).PressAsync("Home");
        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("#000000");
        await Picker(page).GetByRole(AriaRole.Slider, new() { Name = "Brightness" }).PressAsync("End");

        await Expect(hue).ToHaveAttributeAsync("aria-valuetext", "210°");
        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("#55aaff");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sliders_ShouldRaiseTheValueWithTheLeftArrow_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenPickerAsync(browser);
        var hue = Picker(page).GetByRole(AriaRole.Slider, new() { Name = "Hue" });
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        await hue.FocusAsync();

        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(hue).ToHaveAttributeAsync("aria-valuetext", "211°");
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(hue).ToHaveAttributeAsync("aria-valuetext", "209°");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Opacity_ShouldShowAndSetTheAlpha_WhenAlphaIsOn(string browser)
    {
        var page = await OpenPickerAsync(browser, "Highlight");
        var opacity = Picker(page).GetByRole(AriaRole.Slider, new() { Name = "Opacity" });
        await Expect(opacity).ToHaveAttributeAsync("aria-valuetext", "50%");

        await opacity.PressAsync("End");

        await Expect(page.GetByTestId("highlight")).ToHaveTextAsync("#ff0000");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Eyedropper_ShouldTakeTheColorFromTheScreen_WhenTheBrowserHasOne(string browser)
    {
        var page = await OpenAsync(browser, "/color-picker");
        await page.AddInitScriptAsync("""
            window.EyeDropper = class { open() { return Promise.resolve({ sRGBHex: '#123456' }); } };
            """);
        await page.ReloadAsync();
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });

        await Field(page, "Accent").GetByRole(AriaRole.Button, new() { Name = "Pick a color from the screen" })
            .ClickAsync();

        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("#123456");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Eyedropper_ShouldBeLeftOut_WhenTheBrowserHasNone(string browser)
    {
        var page = await OpenAsync(browser, "/color-picker");
        await page.AddInitScriptAsync("delete window.EyeDropper;");
        await page.ReloadAsync();
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        await Expect(Field(page, "Accent").GetByRole(AriaRole.Button, new() { Name = "Choose a color" }))
            .ToHaveAttributeAsync("data-ready", "");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Pick a color from the screen" }))
            .ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Clear_ShouldEmptyTheColorAndFocusTheBox_WhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/color-picker");

        await Field(page, "Accent").GetByRole(AriaRole.Button, new() { Name = "Clear" }).ClickAsync();

        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("none");
        await Expect(Box(page)).ToHaveValueAsync("");
        await Expect(Box(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldTypeTheColorAndWait_WhenChoosing(string browser)
    {
        var page = await OpenStaticAsync(browser, "/color-picker");

        await page.Tb().ColorPicker("Accent").ChooseAsync("#FFCC00");
        await page.Tb().ColorPicker("Highlight").ChooseAsync("#336699FF");

        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("#ffcc00");
        await Expect(page.GetByTestId("highlight")).ToHaveTextAsync("#336699");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldPostTheTypedColor_WhenUsedAsAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-color-picker");

        await page.Tb().ColorPicker("Accent").ChooseAsync("#123456");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("#123456 | none");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldRefuseToPostText_WhenAStaticFormGetsSomethingThatIsntAColor(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-color-picker");
        var accent = page.GetByLabel("Accent", new() { Exact = true });

        await accent.FillAsync("blue");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        Assert.False(await accent.EvaluateAsync<bool>("input => input.checkValidity()"));
        await Expect(page.GetByTestId("posted")).ToBeEmptyAsync();
        await accent.FillAsync("rgb(18, 52, 86)");
        Assert.False(await accent.EvaluateAsync<bool>("input => input.checkValidity()"));
        await accent.FillAsync("#123456");
        Assert.True(await accent.EvaluateAsync<bool>("input => input.checkValidity()"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldHaveNoAccessibilityViolations_WhenOpen(string browser)
    {
        var page = await OpenPickerAsync(browser);

        var result = await page.RunAxe(AxeTests.WcagAA);

        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }
}

// The pad captures the pointer, which Firefox can leak into pages running alongside, so these run on their own.
[Collection(nameof(PointerDrags))]
public class ColorPickerPadTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<(IPage Page, ILocator Pad)> OpenPadAsync(string browser)
    {
        var page = await OpenAsync(browser, "/color-picker");
        var trigger = ColorPickerTests.Field(page, "Accent")
            .GetByRole(AriaRole.Button, new() { Name = "Choose a color" });
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");
        await trigger.ClickAsync();
        // The pad's script attaches separately from the trigger's, so a ready trigger doesn't mean a drag is heard.
        // The popup is placed a frame after it opens, so hovering the pad waits for its box to stop moving.
        var pad = ColorPickerTests.Picker(page).Locator("[data-part=pad]");
        await Expect(pad).ToHaveAttributeAsync("data-ready", "");
        await pad.HoverAsync();
        return (page, pad);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pad_ShouldReportADragAFewTimesASecondAndAtItsEnd_WhenThePointerMovesAcrossIt(string browser)
    {
        var (page, pad) = await OpenPadAsync(browser);
        var box = (await pad.BoundingBoxAsync())!;
        var changes = page.GetByTestId("changes");
        var before = int.Parse(await changes.TextContentAsync() ?? "0");

        // Moves about a frame apart, from the pad's left edge to its right; color-picker.js reports every 120 ms.
        const int moves = 60, moveGap = 20, reportEvery = 120;
        await page.Mouse.MoveAsync(box.X + 1, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        var drag = Stopwatch.StartNew();
        for (var step = 1; step <= moves; step++)
        {
            await page.Mouse.MoveAsync(box.X + 1 + (box.Width - 2) * step / moves, box.Y + box.Height / 2);
            await page.WaitForTimeoutAsync(moveGap);
        }

        await page.Mouse.UpAsync();
        drag.Stop();

        await Expect(ColorPickerTests.Picker(page).GetByRole(AriaRole.Slider, new() { Name = "Saturation" }))
            .ToHaveAttributeAsync("aria-valuetext", "100%");
        // Several reports while dragging, one per interval at most, plus the one at the end.
        Assert.InRange(int.Parse(await changes.TextContentAsync() ?? "0") - before, 4,
            (int)(drag.ElapsedMilliseconds / reportEvery) + 2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pad_ShouldSetSaturationAndBrightness_WhenClicked(string browser)
    {
        var (page, pad) = await OpenPadAsync(browser);

        await pad.ClickAsync();

        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("#406080");
        await Expect(ColorPickerTests.Picker(page).GetByRole(AriaRole.Slider, new() { Name = "Saturation" }))
            .ToHaveAttributeAsync("aria-valuetext", "50%");
        await Expect(ColorPickerTests.Picker(page).GetByRole(AriaRole.Slider, new() { Name = "Brightness" }))
            .ToHaveAttributeAsync("aria-valuetext", "50%");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pad_ShouldLeaveTheColorAlone_WhenRightClickedAfterTheColorWasTyped(string browser)
    {
        var (page, pad) = await OpenPadAsync(browser);
        await pad.ClickAsync();
        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("#406080");
        var box = page.GetByRole(AriaRole.Textbox, new() { Name = "Accent", Exact = true });
        await box.FillAsync("#cc3333");
        await box.PressAsync("Enter");
        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("#cc3333");
        // Typing moved focus out of the picker, which closed it.
        await ColorPickerTests.Field(page, "Accent").GetByRole(AriaRole.Button, new() { Name = "Choose a color" })
            .ClickAsync();

        await pad.ClickAsync(new() { Button = MouseButton.Right });
        // A change to another field is a round trip later, so a stray pad report would have landed by then.
        var highlight = page.GetByRole(AriaRole.Textbox, new() { Name = "Highlight", Exact = true });
        await highlight.FillAsync("#00ff00");
        await highlight.PressAsync("Enter");
        await Expect(page.GetByTestId("highlight")).ToHaveTextAsync("#00ff00");

        await Expect(page.GetByTestId("accent")).ToHaveTextAsync("#cc3333");
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("2");
    }

    // The pad is a color space, so its left edge stays white in right-to-left text, where the click lands.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pad_ShouldPutTheThumbWhereItWasClicked_WhenTheTextIsRightToLeft(string browser)
    {
        var (page, pad) = await OpenPadAsync(browser);
        await page.EvaluateAsync("() => { document.documentElement.dir = 'rtl'; }");
        var box = (await pad.BoundingBoxAsync())!;

        await pad.ClickAsync(new() { Position = new() { X = box.Width / 4, Y = box.Height / 2 } });

        await Expect(ColorPickerTests.Picker(page).GetByRole(AriaRole.Slider, new() { Name = "Saturation" }))
            .ToHaveAttributeAsync("aria-valuetext", "25%");
        var thumb = (await pad.Locator(".tb-color-picker__pad-thumb").BoundingBoxAsync())!;
        Assert.Equal(box.X + box.Width / 4, thumb.X + thumb.Width / 2, tolerance: 1);
    }
}
