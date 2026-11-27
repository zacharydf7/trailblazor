using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.BrowserTests;

public class PieChartTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // The slice under the middle of the element that has focus, by its item's number.
    private const string SliceUnderFocus = """
        () => {
            const box = document.activeElement.getBoundingClientRect();
            return document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2)?.dataset.place ?? '';
        }
        """;

    // The slice at a spot on the page, by its item's number, or an empty string where there is none.
    private const string SliceAt = "({ x, y }) => document.elementFromPoint(x, y)?.dataset.place ?? ''";

    private static ILocator Pie(IPage page, string title) => page.GetByRole(AriaRole.Figure, new() { Name = title });

    // Where the middle of an item's slice is on the page, from what the chart writes for its script.
    private static async Task<(float X, float Y)> MiddleAsync(ILocator chart, int place)
    {
        var plot = chart.Locator("[data-part=plot]");
        var points = await plot.Locator("g[data-series]").GetAttributeAsync("data-points");
        var shares = points!.Split(' ')[place].Split(',')
            .Select(share => float.Parse(share, CultureInfo.InvariantCulture) / 10000).ToArray();
        var box = await plot.BoundingBoxAsync();
        return (box!.X + box.Width * shares[0], box.Y + box.Height * shares[1]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pie_ShouldBeARoundDrawingBesideItsLegend_WhenThereIsRoom(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pie-chart/basic");
        var chart = Pie(page, "Passes sold this season");
        var slices = chart.Locator("path.tb-pie__slice");
        var plot = await chart.Locator("[data-part=plot]").BoundingBoxAsync();
        var legend = await chart.Locator("[data-part=legend]").BoundingBoxAsync();
        Task<string> SliceAtAsync(double x, double y) =>
            page.EvaluateAsync<string>(SliceAt, new { x = plot!.X + plot.Width * x, y = plot.Y + plot.Height * y });

        // The plot is a square of 16rem, and the pie reaches its edges: Day, a little over half of it, is at the
        // middle of the right edge and of the bottom one, Week at the middle of the left one and Season near the
        // top, just left of its middle. A corner of the square is outside the circle.
        Assert.InRange(plot!.Width, 255.5, 256.5);
        Assert.InRange(plot.Height, 255.5, 256.5);
        Assert.Equal(["0", "0", "1", "2", ""], [await SliceAtAsync(0.97, 0.5), await SliceAtAsync(0.52, 0.97),
            await SliceAtAsync(0.03, 0.5), await SliceAtAsync(0.4, 0.04), await SliceAtAsync(0.03, 0.03)]);
        Assert.True(legend!.X >= plot.X + plot.Width, $"The legend starts at {legend.X}, inside the pie.");
        await Expect(slices.Nth(0)).ToHaveCSSAsync("fill", await SystemColorAsync(page, "var(--tb-chart-series-1)"));
        await Expect(slices.Nth(1)).ToHaveCSSAsync("fill", await SystemColorAsync(page, "var(--tb-chart-series-2)"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pie_ShouldStaySquareAboveItsLegend_WhenThePageIsNarrow(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pie-chart/basic");
        var chart = Pie(page, "Passes sold this season");
        await page.SetViewportSizeAsync(220, 700);

        var plot = await chart.Locator("[data-part=plot]").BoundingBoxAsync();
        var legend = await chart.Locator("[data-part=legend]").BoundingBoxAsync();
        var figure = await chart.BoundingBoxAsync();

        // The pie is as wide as its container when that is under 16rem, and as high as it is wide.
        Assert.True(plot!.Width < 250 && plot.Width <= figure!.Width + 0.5, $"The pie is {plot.Width} wide.");
        Assert.InRange(plot.Width - plot.Height, -0.5, 0.5);
        Assert.True(legend!.Y >= plot.Y + plot.Height, $"The legend starts at {legend.Y}, beside the pie.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldShowTheSliceUnderThePointerAndHideOffThePie_WhenThePointerMoves(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pie-chart/basic");
        var chart = Pie(page, "Passes sold this season");
        var plot = chart.Locator("[data-part=plot]");
        var tooltip = chart.Locator("[data-part=tooltip]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var week = await MiddleAsync(chart, 1);

        await page.Mouse.MoveAsync(week.X, week.Y);

        // The tooltip names the slice, gives its value and share beside a key in its color, and is level with it.
        await Expect(tooltip.Locator(".tb-chart__tooltip-place")).ToHaveTextAsync("Week");
        await Expect(tooltip.Locator(".tb-chart__tooltip-value")).ToHaveTextAsync("260 (33%)");
        await Expect(tooltip.Locator(".tb-chart__tooltip-key"))
            .ToHaveCSSAsync("background-color", await SystemColorAsync(page, "var(--tb-chart-series-2)"));
        await Expect(plot.Locator(".tb-chart__crosshair")).ToBeHiddenAsync();
        var shown = await tooltip.BoundingBoxAsync();
        Assert.InRange(shown!.Y + shown.Height / 2 - week.Y, -2, 2);

        // The plot's corner is outside the circle, where there is no slice.
        var box = await plot.BoundingBoxAsync();
        await page.Mouse.MoveAsync(box!.X + 4, box.Y + 4);
        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldMoveBetweenNamedSlicesAndChooseOne_WhenThePieHasFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pie-chart/click");
        var chart = Pie(page, "Rescues by cause");
        var plot = chart.Locator("[data-part=plot]");
        var tooltip = chart.Locator("[data-part=tooltip]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var focused = page.Locator(":focus");

        await plot.Locator("[tabindex='0']").FocusAsync();
        await Expect(focused).ToHaveAccessibleNameAsync("Lost: 14 (44%), 1 of 4");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(focused).ToHaveAccessibleNameAsync("Injury: 9 (28%), 2 of 4");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(focused).ToHaveAccessibleNameAsync("Weather: 5 (16%), 3 of 4");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(focused).ToHaveAccessibleNameAsync("Injury: 9 (28%), 2 of 4");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(focused).ToHaveAccessibleNameAsync("Lost: 14 (44%), 1 of 4");
        await page.Keyboard.PressAsync("End");
        await Expect(focused).ToHaveAccessibleNameAsync("Exhaustion: 4 (12%), 4 of 4");
        await page.Keyboard.PressAsync("Home");
        await Expect(focused).ToHaveAccessibleNameAsync("Lost: 14 (44%), 1 of 4");
        await page.Keyboard.PressAsync("End");

        // The element that has focus sits inside its slice, the last and thinnest one, and the tooltip is for it.
        Assert.Equal("3", await page.EvaluateAsync<string>(SliceUnderFocus));
        var place = tooltip.Locator(".tb-chart__tooltip-place");
        await Expect(place).ToHaveTextAsync("Exhaustion");

        // The pointer over another slice takes the tooltip, and off the slices gives it back to the one with focus.
        var injury = await MiddleAsync(chart, 1);
        var box = await plot.BoundingBoxAsync();
        await page.Mouse.MoveAsync(injury.X, injury.Y);
        await Expect(place).ToHaveTextAsync("Injury");
        await page.Mouse.MoveAsync(box!.X + 4, box.Y + 4);
        await Expect(place).ToHaveTextAsync("Exhaustion");

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByText("Exhaustion: 4 rescues. Turn back while you still have half your water."))
            .ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Home");
        await page.Keyboard.PressAsync("Space");
        await Expect(page.GetByText("Lost: 14 rescues. Carry a map, and tell someone your route.")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");
        await Expect(tooltip).ToBeHiddenAsync();
        await Expect(focused).ToHaveAccessibleNameAsync("Lost: 14 (44%), 1 of 4");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tap_ShouldShowTheTooltipAndPickTheSlice_WhenASliceIsTouched(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pie-chart/click", new() { HasTouch = true });
        var chart = Pie(page, "Rescues by cause");
        var tooltip = chart.Locator("[data-part=tooltip]");
        await Expect(chart.Locator("[data-part=plot]")).ToHaveAttributeAsync("data-ready", "");
        var injury = await MiddleAsync(chart, 1);

        await page.Touchscreen.TapAsync(injury.X, injury.Y);

        await Expect(page.GetByText("Injury: 9 rescues. Wear boots that hold your ankles.")).ToBeVisibleAsync();
        await Expect(tooltip.Locator(".tb-chart__tooltip-place")).ToHaveTextAsync("Injury");

        // The finger left the pie as it lifted, and the tooltip stays until a tap somewhere else.
        var text = await page.GetByText("Injury: 9 rescues").BoundingBoxAsync();
        await page.Touchscreen.TapAsync(text!.X + 5, text.Y + 5);
        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Table_ShouldOpenFromTheKeyboard_WhenItsDisclosureHasFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pie-chart/basic");
        var chart = Pie(page, "Passes sold this season");
        await Expect(chart.Locator("[data-part=plot]")).ToHaveAttributeAsync("data-ready", "");

        // The pie is one tab stop, whichever slice the keys are on, and the table's disclosure is the next.
        await chart.Locator("[data-part=plot] [tabindex='0']").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("Tab");
        await Expect(chart.Locator("summary")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(chart.GetByRole(AriaRole.Row)).ToHaveCountAsync(4);
        // Back on the pie, focus is on the slice it left.
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(page.Locator(":focus")).ToHaveAccessibleNameAsync("Week: 260 (33%), 2 of 3");
        await Expect(chart.GetByRole(AriaRole.Row).Nth(2)).ToHaveTextAsync(new Regex(@"^Week\s*260\s*33%$"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldSwapLeftAndRight_WhenThePieRunsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pie-chart/basic");
        await page.EvaluateAsync("() => { document.documentElement.dir = 'rtl'; }");
        var plot = Pie(page, "Passes sold this season").Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();

        await plot.Locator("[tabindex='0']").FocusAsync();
        var first = await page.Locator(":focus").BoundingBoxAsync();
        await page.Keyboard.PressAsync("ArrowLeft");

        // The pie is mirrored, so its first slice is on the left half, and the focus ring is still inside its slice.
        await Expect(page.Locator(":focus")).ToHaveAccessibleNameAsync("Week: 260 (33%), 2 of 3");
        Assert.True(first!.X + first.Width / 2 < box!.X + box.Width / 2, $"The first slice's middle is at {first.X}.");
        Assert.Equal("1", await page.EvaluateAsync<string>(SliceUnderFocus));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Donut_ShouldShowItsContentInTheHoleAndHaveNoSliceThere_WhenDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pie-chart/donut");
        var chart = Pie(page, "Huts by booking");
        var plot = chart.Locator("[data-part=plot]");
        var tooltip = chart.Locator("[data-part=tooltip]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();
        var (middleX, middleY) = (box!.X + box.Width / 2, box.Y + box.Height / 2);

        // What the app gave is in the middle of the hole.
        var center = chart.Locator("[data-part=center]");
        await Expect(center).ToHaveTextAsync("30 huts");
        var content = await center.BoundingBoxAsync();
        Assert.InRange(content!.X + content.Width / 2 - middleX, -1, 1);
        Assert.InRange(content.Y + content.Height / 2 - middleY, -1, 1);
        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "Donut chart: Booked, Free, Closed for repair" }))
            .ToBeVisibleAsync();

        // The pointer finds a slice on the ring and none in the hole.
        var booked = await MiddleAsync(chart, 0);
        await page.Mouse.MoveAsync(booked.X, booked.Y);
        await Expect(tooltip.Locator(".tb-chart__tooltip-place")).ToHaveTextAsync("Booked");
        await page.Mouse.MoveAsync(middleX, middleY);
        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tone_ShouldColorItsSliceItsLegendKeyAndItsTooltipKey_WhenAnItemHasOne(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pie-chart/tones");
        var chart = Pie(page, "Trails by condition");
        var plot = chart.Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var danger = await SystemColorAsync(page, "var(--tb-color-danger-text)");

        await Expect(chart.Locator("path.tb-pie__slice").Nth(2)).ToHaveCSSAsync("fill", danger);
        await Expect(chart.Locator("[data-part=legend] rect").Nth(2)).ToHaveCSSAsync("fill", danger);
        var closed = await MiddleAsync(chart, 2);
        await page.Mouse.MoveAsync(closed.X, closed.Y);
        await Expect(chart.Locator(".tb-chart__tooltip-place")).ToHaveTextAsync("Closed");
        await Expect(chart.Locator(".tb-chart__tooltip-key")).ToHaveCSSAsync("background-color", danger);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slices_ShouldDifferWithoutColor_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pie-chart/click", new() { ForcedColors = ForcedColors.Active });
        var (text, surface) = (await SystemColorAsync(page, "CanvasText"), await SystemColorAsync(page, "Canvas"));
        var chart = Pie(page, "Rescues by cause");
        var slices = chart.Locator("path.tb-pie__slice");
        var keys = chart.Locator("[data-part=legend] rect");

        // The first slice is filled, the second hollow, and the third and fourth hollow with a dashed and a dotted
        // outline, as their keys are.
        await Expect(slices.Nth(0)).ToHaveCSSAsync("fill", text);
        await Expect(slices.Nth(1)).ToHaveCSSAsync("fill", surface);
        await Expect(slices.Nth(1)).ToHaveCSSAsync("stroke", text);
        await Expect(slices.Nth(2)).ToHaveCSSAsync("stroke-dasharray", "4px, 3px");
        await Expect(slices.Nth(3)).ToHaveCSSAsync("stroke-dasharray", "1px, 3px");
        await Expect(keys.Nth(0)).ToHaveCSSAsync("fill", text);
        await Expect(keys.Nth(1)).ToHaveCSSAsync("fill", surface);
        await Expect(keys.Nth(2)).ToHaveCSSAsync("stroke-dasharray", "4px, 3px");
        await Expect(keys.Nth(3)).ToHaveCSSAsync("stroke-dasharray", "1px, 3px");
    }
}
