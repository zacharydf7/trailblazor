namespace Trailblazor.BrowserTests;

public class FeedbackTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Alert_ShouldClose_WhenItsCloseButtonIsActivatedByKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/examples/alert");
        var alert = page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Close this alert" });
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
                    getComputedStyle(bar.querySelector('[data-part=bar]')).backgroundColor];
            }
            """);

        Assert.Equal([colors[0], colors[0]], colors[1..3]);
        Assert.NotEqual(colors[0], colors[3]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Alert_ShouldPaintItsFillOutlineAndControlsInItsTonesColors_WhenGivenAVariant(string browser)
    {
        var page = await OpenAsync(browser, "/examples/alert");

        // The filled alert's background and text, the outlined alert's border and background, then the tokens.
        var colors = await page.EvaluateAsync<string[]>("""
            () => {
                // A probe inside an element resolves that element's tokens.
                const color = (value, parent = document.body) => {
                    const probe = parent.appendChild(document.createElement('span'));
                    probe.style.color = value;
                    return getComputedStyle(probe).color;
                };
                const byText = text =>
                    [...document.querySelectorAll('.tb-alert')].find(alert => alert.textContent.includes(text));
                const alert = byText('A filled alert.');
                const panel = alert.querySelector('[data-part=actions]').appendChild(document.createElement('div'));
                panel.popover = 'auto';
                const filled = getComputedStyle(alert);
                const outlined = getComputedStyle(byText('An outlined alert.'));
                return [filled.backgroundColor, filled.color, outlined.borderTopColor, outlined.backgroundColor,
                    color('var(--tb-color-danger)'), color('var(--tb-color-on-danger)'),
                    color('var(--tb-color-success-text)'),
                    getComputedStyle(alert.querySelector('[data-part=close]')).color,
                    getComputedStyle(alert.querySelector('[data-part=actions] button')).borderTopColor,
                    color('var(--tb-focus-ring-color)', alert.querySelector('[data-part=close]')),
                    color('var(--tb-focus-ring-color)', panel.appendChild(document.createElement('div'))),
                    color('var(--tb-focus-ring-color)')];
            }
            """);

        Assert.Equal(colors[4..6], colors[0..2]);
        Assert.Equal(colors[6], colors[2]);
        Assert.Equal("rgba(0, 0, 0, 0)", colors[3]);
        // On the fill, the close button, the action and the close button's focus ring take its text color.
        Assert.Equal([colors[5], colors[5], colors[5]], colors[7..10]);
        // A popup's panel in the alert, here a stand-in, keeps the page's focus ring.
        Assert.Equal(colors[11], colors[10]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Alert_ShouldPutItsTitleAboveAndItsActionsBelowTheMessage_WhenGivenBoth(string browser)
    {
        var page = await OpenAsync(browser, "/examples/alert");
        var alert = page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Couldn't sync" });

        var title = (await alert.Locator("[data-part=title]").BoundingBoxAsync())!;
        var message = (await alert.Locator("[data-part=message]").BoundingBoxAsync())!;
        var actions = (await alert.Locator("[data-part=actions]").BoundingBoxAsync())!;

        Assert.True(title.Y + title.Height <= message.Y, "The title sits above the message.");
        Assert.True(message.Y + message.Height <= actions.Y, "The actions sit below the message.");
        await Expect(alert.GetByRole(AriaRole.Button, new() { Name = "Try again" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Progress_ShouldFillItsShareOfTheTrack_WhenGivenAValue(string browser)
    {
        var page = await OpenAsync(browser, "/examples/progress");
        var track = Progress(page, "Upload");

        var trackBox = await track.BoundingBoxAsync();
        var indicatorBox = await track.Locator("[data-part=indicator]").BoundingBoxAsync();

        Assert.Equal(trackBox!.Width / 4, indicatorBox!.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Progress_ShouldPutTheValueAtTheBarsEndAndInTheRingsCenter_WhenShown(string browser)
    {
        var page = await OpenAsync(browser, "/examples/progress");
        var labeled = Progress(page, "Attachment");
        var ring = Progress(page, "Syncing").Locator("[data-part=ring]");

        var bar = (await labeled.Locator("[data-part=bar]").BoundingBoxAsync())!;
        var value = (await labeled.Locator("[data-part=value]").BoundingBoxAsync())!;
        // The ring's center and size, then its text's, measured on the text itself.
        var centers = await ring.EvaluateAsync<double[]>("""
            ring => {
                const range = document.createRange();
                range.selectNodeContents(ring.querySelector('[data-part=value]'));
                const [box, text] = [ring.getBoundingClientRect(), range.getBoundingClientRect()];
                return [box.x + box.width / 2, box.y + box.height / 2, box.width,
                    text.x + text.width / 2, text.y + text.height / 2, text.width];
            }
            """);

        Assert.Equal(bar.X + bar.Width, value.X + value.Width, tolerance: 1);
        Assert.True(value.Y + value.Height <= bar.Y, "The value sits above the bar.");
        Assert.Equal(centers[0], centers[3], tolerance: 1);
        Assert.Equal(centers[1], centers[4], tolerance: 2);
        Assert.InRange(centers[5], 1, centers[2] * 0.8);
    }

    [Theory]
    [MemberData(nameof(BrowsersAndDirections))]
    public async Task Progress_ShouldSlideInTheReadingDirection_WhenIndeterminate(string browser, string direction)
    {
        var page = await OpenAsync(browser, "/examples/progress");
        await page.EvaluateAsync("direction => document.documentElement.dir = direction", direction);
        var indicator = Progress(page, "Loading the trails").Locator("[data-part=indicator]");

        var early = await LeftEdgeAtAsync(indicator, 0.2);
        var late = await LeftEdgeAtAsync(indicator, 0.6);

        Assert.True(direction == "rtl" ? late < early : late > early, $"Moved from {early} to {late} in {direction}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Progress_ShouldFillTheTrackAndOnlyPulse_WhenIndeterminateAndMotionIsReduced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/progress", new() { ReducedMotion = ReducedMotion.Reduce });
        var track = Progress(page, "Loading the trails");
        var bar = track.Locator("[data-part=indicator]");
        var spinner = Progress(page, "Loading the map");

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
        var ring = Progress(page, "Loading the map").Locator("svg");

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

    private static ILocator Progress(IPage page, string name) =>
        page.GetByRole(AriaRole.Progressbar, new() { Name = name, Exact = true });

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
