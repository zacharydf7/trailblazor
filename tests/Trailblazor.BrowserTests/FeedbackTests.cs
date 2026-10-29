namespace Trailblazor.BrowserTests;

public class FeedbackTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Alert_ShouldClose_WhenItsCloseButtonIsActivatedByKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/examples/alert");
        var alert = page.GetByTestId("closable");
        await alert.GetByRole(AriaRole.Button, new() { Name = "Close" }).FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(alert).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Progress_ShouldDrawItsBarAndRingInTheHighlightColor_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/progress", new() { ForcedColors = ForcedColors.Active });

        var colors = await page.EvaluateAsync<string[]>("""
            () => {
                const probe = document.body.appendChild(document.createElement('div'));
                probe.style.cssText = 'forced-color-adjust: none; background: Highlight';
                const bar = document.querySelector('[aria-label="Upload"]');
                return [getComputedStyle(probe).backgroundColor,
                    getComputedStyle(bar.querySelector('[data-part=indicator]')).backgroundColor,
                    getComputedStyle(document.querySelector('[aria-label="Export"] [data-part=indicator]')).stroke,
                    getComputedStyle(bar).backgroundColor];
            }
            """);

        Assert.Equal([colors[0], colors[0]], colors[1..3]);
        Assert.NotEqual(colors[0], colors[3]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Progress_ShouldFillItsShareOfTheTrack_WhenGivenAValue(string browser)
    {
        var page = await OpenAsync(browser, "/examples/progress");
        var track = page.GetByTestId("quarter");

        var trackBox = await track.BoundingBoxAsync();
        var indicatorBox = await track.Locator("[data-part=indicator]").BoundingBoxAsync();

        Assert.Equal(trackBox!.Width / 4, indicatorBox!.Width, tolerance: 1);
    }

    [Theory]
    [MemberData(nameof(BrowsersAndDirections))]
    public async Task Progress_ShouldSlideInTheReadingDirection_WhenIndeterminate(string browser, string direction)
    {
        var page = await OpenAsync(browser, "/examples/progress");
        await page.EvaluateAsync("direction => document.documentElement.dir = direction", direction);
        var indicator = page.GetByTestId("indeterminate").Locator("[data-part=indicator]");

        var early = await LeftEdgeAtAsync(indicator, 0.2);
        var late = await LeftEdgeAtAsync(indicator, 0.6);

        Assert.True(direction == "rtl" ? late < early : late > early, $"Moved from {early} to {late} in {direction}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Progress_ShouldFillTheTrackAndOnlyPulse_WhenIndeterminateAndMotionIsReduced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/progress", new() { ReducedMotion = ReducedMotion.Reduce });
        var track = page.GetByTestId("indeterminate");
        var bar = track.Locator("[data-part=indicator]");
        var spinner = page.GetByTestId("spinner");

        var trackBox = await track.BoundingBoxAsync();
        var barBox = await bar.BoundingBoxAsync();

        Assert.Equal(trackBox!.Width, barBox!.Width, tolerance: 1);
        Assert.Equal(["tb-progress-pulse"], await AnimationNamesAsync(bar));
        Assert.Equal(["tb-progress-pulse"], await AnimationNamesAsync(spinner.Locator("[data-part=indicator]")));
        Assert.Empty(await AnimationNamesAsync(spinner.Locator("svg")));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Progress_ShouldSpinWithoutAJump_WhenCircularAndIndeterminate(string browser)
    {
        var page = await OpenAsync(browser, "/examples/progress");
        var ring = page.GetByTestId("spinner").Locator("svg");

        // A seamless loop ends a cycle at the same angle it starts the next one at.
        var start = await AngleAtAsync(ring, 0);
        var end = await AngleAtAsync(ring, 0.999);

        var gap = Math.Abs(start - end) % 360;
        Assert.True(Math.Min(gap, 360 - gap) < 1, $"The cycle ends at {end} degrees but starts at {start}.");
    }

    public static TheoryData<string, string> BrowsersAndDirections()
    {
        var data = new TheoryData<string, string>();
        foreach (var browser in AllBrowsers.Names)
        {
            data.Add(browser, "ltr");
            data.Add(browser, "rtl");
        }

        return data;
    }

    private static Task<string[]> AnimationNamesAsync(ILocator element) =>
        element.EvaluateAsync<string[]>("element => element.getAnimations().map(animation => animation.animationName)");

    // Pauses the spin at a fraction of its cycle and reads the ring's angle in degrees, from 0 to 360.
    private static Task<double> AngleAtAsync(ILocator ring, double fraction) =>
        ring.EvaluateAsync<double>("""
            (ring, fraction) => {
                const spin = ring.getAnimations()[0];
                spin.pause();
                spin.currentTime = fraction * spin.effect.getComputedTiming().duration;
                const matrix = new DOMMatrix(getComputedStyle(ring).transform);
                return (Math.atan2(matrix.b, matrix.a) * 180 / Math.PI + 360) % 360;
            }
            """, fraction);

    // Pauses the slide at a fraction of its cycle and reads where the indicator is.
    private static Task<double> LeftEdgeAtAsync(ILocator indicator, double fraction) =>
        indicator.EvaluateAsync<double>("""
            (indicator, fraction) => {
                const slide = indicator.getAnimations()[0];
                slide.pause();
                slide.currentTime = fraction * slide.effect.getComputedTiming().duration;
                return indicator.getBoundingClientRect().left;
            }
            """, fraction);
}
