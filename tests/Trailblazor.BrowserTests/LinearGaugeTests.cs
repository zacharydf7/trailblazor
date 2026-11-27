namespace Trailblazor.BrowserTests;

public class LinearGaugeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Gauge_ShouldFillItsBarUpToItsValueUnderItsLabelAndText_WhenDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/linear-gauge/basic");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Trailhead parking" });
        var bar = await gauge.Locator("[data-part=bar]").BoundingBoxAsync();
        var filled = await gauge.Locator("[data-part=indicator]").BoundingBoxAsync();
        var label = await gauge.Locator("[data-part=label]").BoundingBoxAsync();
        var value = await gauge.Locator("[data-part=value]").BoundingBoxAsync();

        // The bar is as wide as the room the gauge has, 24rem here, and 0.5rem high, and 72 hundredths are filled.
        Assert.InRange(bar!.Width, 383.5, 384.5);
        Assert.InRange(bar.Height, 7.5, 8.5);
        Assert.InRange(filled!.X - bar.X, -0.5, 0.5);
        Assert.InRange(filled.Width - bar.Width * 0.72, -0.5, 0.5);
        // The label is above the bar's start and the value's text above its end.
        await Expect(gauge.Locator("[data-part=value]")).ToHaveTextAsync("72%");
        Assert.InRange(label!.X - bar.X, -0.5, 0.5);
        Assert.InRange(value!.X + value.Width - (bar.X + bar.Width), -0.5, 0.5);
        Assert.True(label.Y + label.Height <= bar.Y && value.Y + value.Height <= bar.Y,
            "The text isn't above the bar.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Value_ShouldStayAboveTheBarsEnd_WhenTheGaugeHasNoLabel(string browser)
    {
        var page = await OpenAsync(browser, "/examples/linear-gauge/table");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Ridge hut, beds booked tonight" });
        await Expect(gauge.Locator("[data-part=label]")).ToHaveCountAsync(0);
        var bar = await gauge.Locator("[data-part=bar]").BoundingBoxAsync();
        var range = await gauge.Locator("[data-part=range]").BoundingBoxAsync();

        // The value and its range's name end where the bar ends, not at its start, with nothing before them.
        await Expect(gauge.Locator("[data-part=range]")).ToHaveTextAsync("Busy");
        Assert.InRange(range!.X + range.Width - (bar!.X + bar.Width), -0.5, 0.5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Label_ShouldWrapBesideTheValuesText_WhenItIsTooLongForOneLine(string browser)
    {
        var page = await OpenAsync(browser, "/examples/linear-gauge/basic");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Trailhead parking" });
        var line = (await gauge.Locator("[data-part=value]").BoundingBoxAsync())!.Height;

        await gauge.Locator("[data-part=label]").EvaluateAsync("label => { label.textContent = "
            + "'Trailhead parking at the north end of the valley, where the road from the village ends'; }");

        var bar = await gauge.Locator("[data-part=bar]").BoundingBoxAsync();
        var label = await gauge.Locator("[data-part=label]").BoundingBoxAsync();
        var value = await gauge.Locator("[data-part=value]").BoundingBoxAsync();
        // The label takes more lines, and the value's text keeps its one line at the bar's end, clear of the label.
        Assert.True(label!.Height > line * 1.8, $"The label is {label.Height} high.");
        Assert.InRange(value!.Height - line, -0.5, 0.5);
        Assert.InRange(value.X + value.Width - (bar!.X + bar.Width), -0.5, 0.5);
        Assert.True(label.X + label.Width <= value.X, "The label runs under the value's text.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeName_ShouldStayBesideTheValue_WhenALongLabelWrapsAndThereIsRoomForBoth(string browser)
    {
        var page = await OpenAsync(browser, "/examples/linear-gauge/ranges");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Hut beds booked tonight" });
        var line = (await gauge.Locator("[data-part=value]").BoundingBoxAsync())!.Height;
        await page.GetByRole(AriaRole.Slider, new() { Name = "Beds booked" }).PressAsync("End");
        await Expect(gauge.Locator("[data-part=range]")).ToHaveTextAsync("Nearly full");

        await gauge.Locator("[data-part=label]").EvaluateAsync("label => { label.textContent = "
            + "'Hut beds booked tonight across the ridge loop, the lake path and the summit trail'; }");

        // The label gives way, and the value and the range's name keep their one line together.
        var label = await gauge.Locator("[data-part=label]").BoundingBoxAsync();
        var (value, range) = (await gauge.Locator("[data-part=value]").BoundingBoxAsync(),
            await gauge.Locator("[data-part=range]").BoundingBoxAsync());
        Assert.True(label!.Height > line * 1.8, $"The label is {label.Height} high.");
        Assert.InRange(range!.Y - value!.Y, -2, 2);
        Assert.True(range.X > value.X, "The range's name isn't after the value.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeName_ShouldGoUnderTheValue_WhenTheGaugeIsTooNarrowForBoth(string browser)
    {
        var page = await OpenAsync(browser, "/examples/linear-gauge/table");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Summit hut, beds booked tonight" });
        var value = gauge.Locator("[data-part=value]");
        var range = gauge.Locator("[data-part=range]");
        var line = (await value.BoundingBoxAsync())!.Height;

        await gauge.EvaluateAsync("gauge => { gauge.style.inlineSize = '5rem'; }");

        // Each keeps its one line, and the name is under the value, both at the bar's end, and inside the gauge.
        var (box, bar) = (await gauge.BoundingBoxAsync(), await gauge.Locator("[data-part=bar]").BoundingBoxAsync());
        var (above, below) = (await value.BoundingBoxAsync(), await range.BoundingBoxAsync());
        Assert.InRange(above!.Height - line, -0.5, 0.5);
        Assert.InRange(below!.Height - line, -0.5, 0.5);
        Assert.True(below.Y >= above.Y + above.Height - 0.5, "The range's name isn't under the value.");
        Assert.InRange(above.X + above.Width - (bar!.X + bar.Width), -0.5, 0.5);
        Assert.InRange(below.X + below.Width - (bar.X + bar.Width), -0.5, 0.5);
        Assert.True(below.X >= box!.X - 0.5, "The range's name runs out of the gauge.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Gauge_ShouldTakeTheNameAndToneOfTheRange_WhenItsValueMovesIntoIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/linear-gauge/ranges");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Hut beds booked tonight" });
        var filled = gauge.Locator("[data-part=indicator]");
        var bands = gauge.Locator(".tb-linear-gauge__band");
        var success = await SystemColorAsync(page, "var(--tb-color-success-text)");
        var warning = await SystemColorAsync(page, "var(--tb-color-warning-text)");
        var danger = await SystemColorAsync(page, "var(--tb-color-danger-text)");
        await Expect(gauge.Locator("[data-part=range]")).ToHaveTextAsync("Busy");
        await Expect(filled).ToHaveCSSAsync("background-color", warning);

        await page.GetByRole(AriaRole.Slider, new() { Name = "Beds booked" }).PressAsync("End");

        await Expect(gauge).ToHaveAttributeAsync("aria-valuetext", "40, Nearly full");
        await Expect(gauge.Locator("[data-part=range]")).ToHaveTextAsync("Nearly full");
        await Expect(filled).ToHaveCSSAsync("background-color", danger);
        // Each band keeps its own tone wherever the value is.
        await Expect(bands.Nth(0)).ToHaveCSSAsync("background-color", success);
        await Expect(bands.Nth(1)).ToHaveCSSAsync("background-color", warning);
        await Expect(bands.Nth(2)).ToHaveCSSAsync("background-color", danger);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Bands_ShouldRunBetweenTheirValuesUnderTheBar_WhenDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/linear-gauge/ranges");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Hut beds booked tonight" });
        var bar = await gauge.Locator("[data-part=bar]").BoundingBoxAsync();
        var bands = gauge.Locator(".tb-linear-gauge__band");
        var (quiet, busy, full) = (await bands.Nth(0).BoundingBoxAsync(), await bands.Nth(1).BoundingBoxAsync(),
            await bands.Nth(2).BoundingBoxAsync());

        // 24 and 34 of 40 beds are 60 and 85 hundredths of the bar.
        Assert.InRange(quiet!.X - bar!.X, -0.5, 0.5);
        Assert.InRange(busy!.X - (bar.X + bar.Width * 0.6), -0.5, 0.5);
        Assert.InRange(full!.X - (bar.X + bar.Width * 0.85), -0.5, 0.5);
        Assert.InRange(full.X + full.Width - (bar.X + bar.Width), -0.5, 0.5);
        Assert.True(quiet.Y >= bar.Y + bar.Height, "The bands aren't under the bar.");
        // A clear line on each side of a band leaves the surface showing where two meet.
        await Expect(bands.Nth(1)).ToHaveCSSAsync("border-left-width", "1px");
        await Expect(bands.Nth(1)).ToHaveCSSAsync("border-left-color", "rgba(0, 0, 0, 0)");
        await Expect(bands.Nth(1)).ToHaveCSSAsync("background-clip", "padding-box");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Target_ShouldBeMarkedAcrossTheBarAtItsValue_WhenThePageRunsEitherWay(string browser)
    {
        var page = await OpenAsync(browser, "/examples/linear-gauge/target");
        var gauge = page.GetByRole(AriaRole.Meter, new() { Name = "Volunteer hours this season (goal 400)" });
        await Expect(gauge).ToHaveAttributeAsync("aria-valuetext", "310, target 400");

        foreach (var (direction, across) in new[] { ("ltr", 0.8), ("rtl", 0.2) })
        {
            await page.EvaluateAsync("direction => { document.documentElement.dir = direction; }", direction);
            var bar = await gauge.Locator("[data-part=bar]").BoundingBoxAsync();
            var mark = await gauge.Locator("[data-part=target]").BoundingBoxAsync();
            var filled = await gauge.Locator("[data-part=indicator]").BoundingBoxAsync();
            var min = await gauge.Locator("[data-part=min]").BoundingBoxAsync();

            // 400 of 500 is four fifths along the bar from its start, which is on the right when right to left.
            Assert.InRange(mark!.X + mark.Width / 2 - (bar!.X + bar.Width * across), -1, 1);
            Assert.True(mark.Y < bar.Y && mark.Y + mark.Height > bar.Y + bar.Height,
                $"The mark is {mark.Height} high.");
            // The filled part and the lowest value start at the same end.
            var start = direction == "ltr" ? bar.X : bar.X + bar.Width;
            Assert.InRange((direction == "ltr" ? filled!.X : filled!.X + filled.Width) - start, -0.5, 0.5);
            Assert.InRange((direction == "ltr" ? min!.X : min!.X + min.Width) - start, -0.5, 0.5);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Gauge_ShouldFillNothing_WhenItsValueIsBelowItsLowestOrMissing(string browser)
    {
        var page = await OpenAsync(browser, "/examples/linear-gauge/ends");
        var cold = page.GetByRole(AriaRole.Meter, new() { Name = "Summit temperature" });

        // A value below the lowest is drawn at that end, and its text still says the value.
        await Expect(cold.Locator("[data-part=value]")).ToHaveTextAsync("-34°C");
        Assert.InRange((await cold.Locator("[data-part=indicator]").BoundingBoxAsync())!.Width, 0, 0.5);
        await Expect(cold.Locator("[data-part=min]")).ToHaveTextAsync("-30°C");

        var empty = await OpenAsync(browser, "/examples/linear-gauge/empty");
        var offline = empty.GetByRole(AriaRole.Meter, new() { Name = "Summit wind" });

        // A gauge without a value has no filled part, and says so in muted text.
        await Expect(offline.Locator("[data-part=value]")).ToHaveTextAsync("Offline");
        await Expect(offline.Locator("[data-part=value]")).ToHaveCSSAsync("color", await MutedColorAsync(empty));
        await Expect(offline.Locator("[data-part=indicator]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Bar_ShouldTakeTheHeightSetForIt_WhenTheTokenIsGiven(string browser)
    {
        var page = await OpenAsync(browser, "/examples/linear-gauge/tones");
        var tank = page.GetByRole(AriaRole.Meter, new() { Name = "Water tank" });
        var battery = page.GetByRole(AriaRole.Meter, new() { Name = "Battery" });

        Assert.InRange((await tank.Locator("[data-part=bar]").BoundingBoxAsync())!.Height, 7.5, 8.5);
        Assert.InRange((await battery.Locator("[data-part=bar]").BoundingBoxAsync())!.Height, 15.5, 16.5);
        await Expect(tank.Locator("[data-part=indicator]")).ToHaveCSSAsync("background-color",
            await SystemColorAsync(page, "var(--tb-color-info-text)"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Gauge_ShouldTakeSystemColors_WhenColorsAreForced(string browser)
    {
        var ranges = await OpenAsync(browser, "/examples/linear-gauge/ranges",
            new() { ForcedColors = ForcedColors.Active });
        var text = await SystemColorAsync(ranges, "CanvasText");
        var gauge = ranges.GetByRole(AriaRole.Meter, new() { Name = "Hut beds booked tonight" });

        await Expect(gauge.Locator("[data-part=indicator]")).ToHaveCSSAsync("background-color",
            await SystemColorAsync(ranges, "Highlight"));
        await Expect(gauge.Locator("[data-part=bar]")).ToHaveCSSAsync("border-top-color", text);
        await Expect(gauge.Locator(".tb-linear-gauge__band").Nth(0)).ToHaveCSSAsync("background-color", text);
        await Expect(gauge.Locator(".tb-linear-gauge__band").Nth(2)).ToHaveCSSAsync("background-color", text);

        var target = await OpenAsync(browser, "/examples/linear-gauge/target",
            new() { ForcedColors = ForcedColors.Active });

        await Expect(target.Locator("[data-part=target]").First).ToHaveCSSAsync("background-color", text);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Gauge_ShouldMoveItsFilledPartAtOnce_WhenMotionIsReduced(string browser)
    {
        var moving = await OpenAsync(browser, "/examples/linear-gauge/basic");
        await Expect(moving.Locator("[data-part=indicator]")).ToHaveCSSAsync("transition-duration", "0.2s, 0.2s");

        var still = await OpenAsync(browser, "/examples/linear-gauge/basic",
            new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(still.Locator("[data-part=indicator]")).ToHaveCSSAsync("transition-duration", "0s, 0s");
    }
}
