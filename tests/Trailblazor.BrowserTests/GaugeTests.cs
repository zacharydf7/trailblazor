using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class GaugeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // How many of the arc's 100 the filled part's one dash covers.
    private const string Filled = "indicator => parseFloat(getComputedStyle(indicator).strokeDasharray)";

    private const string FontSize = "text => parseFloat(getComputedStyle(text).fontSize)";

    // Where on the page a path starts.
    private const string Start = """
        path => {
            const start = path.getPointAtLength(0).matrixTransform(path.getScreenCTM());
            return [start.x, start.y];
        }
        """;

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Gauge_ShouldFillItsArcUpToItsValueAroundItsText_WhenDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/gauge/basic");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Trailhead parking" });
        var box = await gauge.BoundingBoxAsync();
        var dial = await gauge.Locator("[data-part=dial]").BoundingBoxAsync();
        var value = await gauge.Locator("[data-part=value]").BoundingBoxAsync();
        var label = await gauge.Locator("[data-part=label]").BoundingBoxAsync();

        // 10rem wide, with the dial across it: three quarters of a circle, so 86 high for its 100 across.
        Assert.InRange(box!.Width, 159.5, 160.5);
        Assert.InRange(dial!.Width, 159.5, 160.5);
        Assert.InRange(dial.Height / dial.Width, 0.85, 0.87);
        Assert.InRange(await gauge.Locator("[data-part=indicator]").EvaluateAsync<double>(Filled), 71.9, 72.1);
        // The value is at the arc's middle, which is halfway across the dial and 50 of its 86 down.
        await Expect(gauge.Locator("[data-part=value]")).ToHaveTextAsync("72%");
        Assert.InRange(value!.X + value.Width / 2 - (dial.X + dial.Width / 2), -1, 1);
        Assert.InRange(value.Y + value.Height / 2 - (dial.Y + dial.Height * 50 / 86), -2, 2);
        // The label is under the dial and centered on it.
        Assert.True(label!.Y >= dial.Y + dial.Height, "The label isn't under the dial.");
        Assert.InRange(label.X + label.Width / 2 - (dial.X + dial.Width / 2), -1, 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Gauge_ShouldTakeTheNameAndToneOfTheRange_WhenItsValueMovesIntoIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/gauge/ranges");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Hut beds booked tonight" });
        var indicator = gauge.Locator("[data-part=indicator]");
        var bands = gauge.Locator(".tb-gauge__band");
        var success = await SystemColorAsync(page, "var(--tb-color-success-text)");
        var warning = await SystemColorAsync(page, "var(--tb-color-warning-text)");
        var danger = await SystemColorAsync(page, "var(--tb-color-danger-text)");
        await Expect(gauge.Locator("[data-part=range]")).ToHaveTextAsync("Busy");
        await Expect(indicator).ToHaveCSSAsync("stroke", warning);

        await page.GetByRole(AriaRole.Slider, new() { Name = "Beds booked" }).PressAsync("End");

        await Expect(gauge).ToHaveAttributeAsync("aria-valuetext", "40, Nearly full");
        await Expect(gauge.Locator("[data-part=value]")).ToHaveTextAsync("40");
        await Expect(gauge.Locator("[data-part=range]")).ToHaveTextAsync("Nearly full");
        await Expect(indicator).ToHaveCSSAsync("stroke", danger);
        await Expect(indicator).ToHaveCSSAsync("stroke-dasharray", new Regex("^100(px)?, 100(px)?$"));
        // Each band keeps its own tone wherever the value is.
        await Expect(bands.Nth(0)).ToHaveCSSAsync("stroke", success);
        await Expect(bands.Nth(1)).ToHaveCSSAsync("stroke", warning);
        await Expect(bands.Nth(2)).ToHaveCSSAsync("stroke", danger);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ranges_ShouldRunAroundTheValuesArc_WhenDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/gauge/ranges");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Hut beds booked tonight" });
        var dial = await gauge.Locator("[data-part=dial]").BoundingBoxAsync();

        var band = await gauge.Locator(".tb-gauge__band").First.EvaluateAsync<double[]>(Start);
        var track = await gauge.Locator(".tb-gauge__track").EvaluateAsync<double[]>(Start);

        // Both start at the lower left, the band farther from the middle than the arc inside it.
        Assert.True(band[0] < track[0] && band[1] > track[1], $"The band starts at {band[0]}, {band[1]}.");
        Assert.InRange(band[0] - dial!.X, dial.Width * 0.14, dial.Width * 0.18);
        Assert.InRange(track[0] - dial.X, dial.Width * 0.2, dial.Width * 0.24);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ends_ShouldBeUnderTheArcsEndsWithTheLowestOnTheLeft_WhenThePageRunsEitherWay(string browser)
    {
        var page = await OpenAsync(browser, "/examples/gauge/ends");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Summit temperature (°C)" });
        await Expect(gauge.Locator("[data-part=min]")).ToHaveTextAsync("-30°");
        await Expect(gauge.Locator("[data-part=max]")).ToHaveTextAsync("30°");

        foreach (var direction in new[] { "ltr", "rtl" })
        {
            await page.EvaluateAsync("direction => { document.documentElement.dir = direction; }", direction);
            var dial = await gauge.Locator("[data-part=dial]").BoundingBoxAsync();
            var min = await gauge.Locator("[data-part=min]").BoundingBoxAsync();
            var max = await gauge.Locator("[data-part=max]").BoundingBoxAsync();
            var start = await gauge.Locator(".tb-gauge__track").EvaluateAsync<double[]>(Start);

            // A dial runs clockwise from the lower left in either direction: its arc ends 18 and 82 of its 100 across.
            Assert.InRange(start[0] - (dial!.X + dial.Width * 0.18), -1, 1);
            Assert.InRange(min!.X + min.Width / 2 - (dial.X + dial.Width * 0.18), -1.5, 1.5);
            Assert.InRange(max!.X + max.Width / 2 - (dial.X + dial.Width * 0.82), -1.5, 1.5);
            Assert.True(min.Y >= dial.Y + dial.Height && max.Y >= dial.Y + dial.Height,
                $"An end is in the dial, {direction}.");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Text_ShouldScaleWithTheGauge_WhenGivenADiameter(string browser)
    {
        var page = await OpenAsync(browser, "/examples/gauge/tones");
        var small = page.GetByRole(AriaRole.Meter, new() { Name = "Water tank" });
        var large = page.GetByRole(AriaRole.Meter, new() { Name = "Storage" });

        // 7rem and 13rem wide, with the value's text 18 hundredths of that width high.
        Assert.InRange((await small.BoundingBoxAsync())!.Width, 111.5, 112.5);
        Assert.InRange((await large.BoundingBoxAsync())!.Width, 207.5, 208.5);
        Assert.InRange(await small.Locator("[data-part=value]").EvaluateAsync<double>(FontSize), 20.1, 20.2);
        Assert.InRange(await large.Locator("[data-part=value]").EvaluateAsync<double>(FontSize), 37.4, 37.5);
        // Each takes its own tone.
        await Expect(small.Locator("[data-part=indicator]")).ToHaveCSSAsync("stroke",
            await SystemColorAsync(page, "var(--tb-color-info-text)"));
        await Expect(large.Locator("[data-part=indicator]")).ToHaveCSSAsync("stroke",
            await SystemColorAsync(page, "var(--tb-color-neutral-text)"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Gauges_ShouldTakeTheDiameterSetAroundThemAndNoMoreThanTheRoom_WhenInsideCards(string browser)
    {
        var page = await OpenAsync(browser, "/examples/gauge/dashboard");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Ridge hut, beds booked tonight" });
        await Expect(gauge).ToHaveAttributeAsync("aria-valuetext", "21, Busy");

        // The row sets every gauge in it to 8rem.
        Assert.InRange((await gauge.BoundingBoxAsync())!.Width, 127.5, 128.5);

        await gauge.EvaluateAsync("gauge => { gauge.parentElement.style.inlineSize = '5rem'; }");

        // With less room than that, a gauge is as wide as the room it has.
        Assert.InRange((await gauge.BoundingBoxAsync())!.Width, 79.5, 80.5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Gauge_ShouldTakeSystemColors_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/gauge/ranges", new() { ForcedColors = ForcedColors.Active });
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Hut beds booked tonight" });
        var text = await SystemColorAsync(page, "CanvasText");

        await Expect(gauge.Locator("[data-part=indicator]")).ToHaveCSSAsync("stroke",
            await SystemColorAsync(page, "Highlight"));
        await Expect(gauge.Locator(".tb-gauge__track")).ToHaveCSSAsync("stroke",
            await SystemColorAsync(page, "GrayText"));
        // The bands lose their tones, and the range's name still says which one the value is in.
        await Expect(gauge.Locator(".tb-gauge__band").Nth(0)).ToHaveCSSAsync("stroke", text);
        await Expect(gauge.Locator(".tb-gauge__band").Nth(2)).ToHaveCSSAsync("stroke", text);
        await Expect(gauge.Locator("[data-part=range]")).ToHaveTextAsync("Busy");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Gauge_ShouldMoveItsFilledPartAtOnce_WhenMotionIsReduced(string browser)
    {
        var moving = await OpenAsync(browser, "/examples/gauge/basic");
        await Expect(moving.Locator("[data-part=indicator]")).ToHaveCSSAsync("transition-duration", "0.2s, 0.2s");

        var still = await OpenAsync(browser, "/examples/gauge/basic", new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(still.Locator("[data-part=indicator]")).ToHaveCSSAsync("transition-duration", "0s, 0s");
    }
}
