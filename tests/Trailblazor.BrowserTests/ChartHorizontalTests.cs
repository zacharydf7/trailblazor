using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// A chart turned on its side: its places run down it and its bars across.
public class ChartHorizontalTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string LongName = "Summit trail by the long north face route";

    private static ILocator Chart(IPage page, string title) => page.GetByRole(AriaRole.Figure, new() { Name = title });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Bars_ShouldRunAcrossBesideTheirNamesAndOverTheValueAxis_WhenHorizontal(string browser)
    {
        var page = await OpenAsync(browser, "/chart-horizontal");
        var chart = Chart(page, "Hikers by trail");
        var plot = await chart.Locator("[data-part=plot]").BoundingBoxAsync();
        var weekdays = chart.Locator("g[data-series='Weekdays'] rect");
        var names = chart.Locator(".tb-chart__y > span:not(.tb-chart__y-sizer)");

        // The axis runs from 0 to 100, so the ridge loop's 40 reaches two fifths of the way across from the left.
        var bar = await weekdays.Nth(0).BoundingBoxAsync();
        Assert.InRange(bar!.X - plot!.X, -1, 1);
        Assert.InRange(bar.Width - plot.Width * 0.4f, -1, 1);
        // Its name is left of the plot, level with the middle of its third of the plot.
        var name = await names.Nth(0).BoundingBoxAsync();
        await Expect(names.Nth(0)).ToHaveTextAsync("Ridge loop");
        Assert.True(name!.X + name.Width <= plot.X, $"The name ends at {name.X + name.Width}, inside the plot.");
        Assert.InRange(name.Y + name.Height / 2 - (plot.Y + plot.Height / 6), -1.5, 1.5);
        // The value axis is under the plot, with 60 three fifths of the way across.
        var sixty = await chart.Locator(".tb-chart__x > span").Filter(new() { HasTextString = "60" })
            .BoundingBoxAsync();
        Assert.True(sixty!.Y >= plot.Y + plot.Height - 1, $"The value label is at {sixty.Y}, inside the plot.");
        Assert.InRange(sixty.X + sixty.Width / 2 - (plot.X + plot.Width * 0.6f), -1.5, 1.5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Names_ShouldTakeNoMoreThanTwoFifthsOfTheChart_WhenOneIsLong(string browser)
    {
        var page = await OpenAsync(browser, "/chart-horizontal");
        var chart = Chart(page, "Hikers by trail");
        var body = await chart.Locator(".tb-chart__body").BoundingBoxAsync();
        var column = await chart.Locator(".tb-chart__y").BoundingBoxAsync();
        var name = chart.Locator(".tb-chart__y > span:not(.tb-chart__y-sizer)").Nth(2);

        // The long name is cut short with its end left off, and the tooltip and the table still have it whole.
        Assert.InRange(column!.Width - body!.Width * 0.4f, -1, 1);
        await Expect(name).ToHaveTextAsync(LongName);
        Assert.True(await name.EvaluateAsync<bool>("name => name.scrollWidth > name.clientWidth"));
        await Expect(name).ToHaveCSSAsync("text-overflow", "ellipsis");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Names_ShouldAllShowInFull_WhenAShortOneHasWiderLettersThanTheLongest(string browser)
    {
        var page = await OpenAsync(browser, "/chart-horizontal");
        var names = Chart(page, "Wide and narrow names").Locator(".tb-chart__y > span:not(.tb-chart__y-sizer)");
        await Expect(names).ToHaveTextAsync(["WWWWWWWW", "illi lil tilt till"]);

        // The column is as wide as the widest name as drawn, not the one with the most letters.
        Assert.All(await names.EvaluateAllAsync<bool[]>(
            "names => names.map(name => name.scrollWidth <= name.clientWidth)"), Assert.True);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Names_ShouldBeThinnedToThoseThatHaveALineEach_WhenThereAreTooManyForThePlot(string browser)
    {
        var page = await OpenAsync(browser, "/chart-horizontal");
        var names = Chart(page, "Forty trails").Locator(".tb-chart__y > span:not(.tb-chart__y-sizer)");
        await Expect(names).ToHaveCountAsync(40);

        var shown = await names.EvaluateAllAsync<float[][]>("""
            names => names.map(name => name.getBoundingClientRect()).filter(box => box.height > 0)
                .map(box => [box.top, box.bottom]).sort((one, other) => one[0] - other[0])
            """);

        // Forty names don't fit in ten rem, so some are left out, evenly, and those that show don't overlap.
        Assert.InRange(shown.Length, 3, 20);
        Assert.All(shown.Zip(shown.Skip(1)), pair => Assert.True(pair.Second[0] >= pair.First[1] - 0.5,
            $"A name that ends at {pair.First[1]} overlaps one that starts at {pair.Second[0]}."));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldBeForThePlaceThePointerIsLevelWith_WhenHorizontal(string browser)
    {
        var page = await OpenAsync(browser, "/chart-horizontal");
        var chart = Chart(page, "Hikers by trail");
        var plot = chart.Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();

        // Anywhere across the plot in its middle third is the lake path, whichever bar is under the pointer.
        await page.Mouse.MoveAsync(box!.X + box.Width * 0.9f, box.Y + box.Height * 0.5f);

        await Expect(chart.Locator(".tb-chart__tooltip-place")).ToHaveTextAsync("Lake path");
        await Expect(chart.Locator(".tb-chart__tooltip-row")).ToHaveTextAsync(["Weekdays60", "Weekend30"]);
        // The line runs across the plot at the place.
        var line = await plot.Locator(".tb-chart__crosshair").BoundingBoxAsync();
        Assert.InRange(line!.Width - box.Width, -1, 1);
        Assert.InRange(line.Y - (box.Y + box.Height / 2), -1.5, 1.5);

        await page.Mouse.MoveAsync(box.X + box.Width * 0.9f, box.Y + box.Height * 0.9f);
        await Expect(chart.Locator(".tb-chart__tooltip-place")).ToHaveTextAsync(LongName);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldMoveDownThePlacesAndAcrossTheSeries_WhenHorizontal(string browser)
    {
        var page = await OpenAsync(browser, "/chart-horizontal");
        var plot = Chart(page, "Hikers by trail").Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();
        var focused = page.Locator(":focus");

        await plot.Locator("[tabindex='0']").FocusAsync();
        await Expect(focused).ToHaveAccessibleNameAsync("Ridge loop, Weekdays: 40, 1 of 3");
        // The focus ring is on the end of the bar that shows its value.
        var ring = await focused.BoundingBoxAsync();
        Assert.InRange(ring!.X + ring.Width / 2 - (box!.X + box.Width * 0.4f), -1.5, 1.5);
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(focused).ToHaveAccessibleNameAsync("Lake path, Weekdays: 60, 2 of 3");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(focused).ToHaveAccessibleNameAsync("Lake path, Weekend: 30, 2 of 3");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(focused).ToHaveAccessibleNameAsync("Lake path, Weekdays: 60, 2 of 3");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(focused).ToHaveAccessibleNameAsync("Ridge loop, Weekdays: 40, 1 of 3");
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("picked")).ToHaveTextAsync($"Weekdays {LongName} 20");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Click_ShouldPickTheBarUnderThePointer_WhenHorizontal(string browser)
    {
        var page = await OpenAsync(browser, "/chart-horizontal");
        var chart = Chart(page, "Hikers by trail");
        var plot = chart.Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();

        // A third of the way along the lake path's weekday bar, which reaches 60, the weekend bar's end at 30 is
        // the nearer point, and the bar under the pointer is still the one picked.
        var weekdays = await chart.Locator("g[data-series='Weekdays'] rect").Nth(1).BoundingBoxAsync();
        await page.Mouse.ClickAsync(box!.X + box.Width * 0.35f, weekdays!.Y + weekdays.Height / 2);
        await Expect(page.GetByTestId("picked")).ToHaveTextAsync("Weekdays Lake path 60");

        // The helper clicks a point where it is.
        await page.Tb().Chart("Hikers by trail").ClickPointAsync("Weekend", "Lake path");
        await Expect(page.GetByTestId("picked")).ToHaveTextAsync("Weekend Lake path 30");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tap_ShouldShowTheTooltipAndPickThePointOfThePlaceItIsLevelWith_WhenHorizontal(string browser)
    {
        var page = await OpenAsync(browser, "/chart-horizontal", new() { HasTouch = true });
        var chart = Chart(page, "Hikers by trail");
        var plot = chart.Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();

        // Past the bars' ends in the lake path's third of the plot, its weekday point is the nearer of its two.
        await page.Touchscreen.TapAsync(box!.X + box.Width * 0.9f, box.Y + box.Height * 0.45f);

        await Expect(page.GetByTestId("picked")).ToHaveTextAsync("Weekdays Lake path 60");
        await Expect(chart.Locator(".tb-chart__tooltip-place")).ToHaveTextAsync("Lake path");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Bars_ShouldRunFromTheRightAndTheSeriesKeysSwap_WhenHorizontalAndRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/chart-horizontal");
        await page.EvaluateAsync("() => { document.documentElement.dir = 'rtl'; }");
        var chart = Chart(page, "Hikers by trail");
        var plot = chart.Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();

        // Zero is at the plot's right edge, and the names are on that side of it.
        var bar = await chart.Locator("g[data-series='Weekdays'] rect").Nth(0).BoundingBoxAsync();
        var name = await chart.Locator(".tb-chart__y > span:not(.tb-chart__y-sizer)").Nth(0).BoundingBoxAsync();
        Assert.InRange(bar!.X + bar.Width - (box!.X + box.Width), -1, 1);
        Assert.True(name!.X >= box.X + box.Width, $"The name starts at {name.X}, inside the plot.");

        // Left goes to the next series, as the legend runs from the right.
        await plot.Locator("[tabindex='0']").FocusAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(page.Locator(":focus")).ToHaveAccessibleNameAsync("Ridge loop, Weekend: 90, 1 of 3");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.Locator(":focus")).ToHaveAccessibleNameAsync("Lake path, Weekend: 30, 2 of 3");
    }
}
