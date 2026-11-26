namespace Trailblazor.BrowserTests;

public class ChartTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Where the labels, grid lines and the first series' markers are on the screen. A label with no width is one the
    // axis has thinned out.
    private const string Positions = """
        chart => {
            const middle = element => {
                const box = element.getBoundingClientRect();
                return [box.left + box.width / 2, box.top + box.height / 2];
            };
            const all = selector => [...chart.querySelectorAll(selector)].map(middle);
            return {
                valueLabels: all('.tb-chart__y > span:not(.tb-chart__y-sizer)').map(at => at[1]),
                gridLines: all('.tb-chart__grid').map(at => at[1]),
                placeLabels: [...chart.querySelectorAll('.tb-chart__x > span')].map(label => {
                    const box = label.getBoundingClientRect();
                    return box.width > 0 ? [box.left, box.right] : null;
                }),
                markers: all('.tb-chart__series--1 > .tb-chart__marker').map(at => at[0]),
            };
        }
        """;

    private const string Dashes = "lines => lines.map(line => getComputedStyle(line).strokeDasharray)";

    // How the first bar under each element is filled and outlined.
    private const string FillsAndDashes = """
        groups => groups.map(group => {
            const style = getComputedStyle(group.querySelector('rect'));
            return `${style.fill} ${style.strokeDasharray}`;
        })
        """;

    private static ILocator Chart(IPage page, string title) => page.GetByRole(AriaRole.Figure, new() { Name = title });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Labels_ShouldSitOnTheirGridLinesAndMarks_WhenTheChartIsAnyWidthAndDirection(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/lines");

        foreach (var (direction, width) in new[] { ("ltr", 1280), ("rtl", 1280), ("ltr", 400), ("rtl", 400) })
        {
            await page.SetViewportSizeAsync(width, 800);
            await page.EvaluateAsync("direction => { document.documentElement.dir = direction; }", direction);

            var at = await Chart(page, "Trail temperature").EvaluateAsync<ChartPositions>(Positions);

            Assert.Equal(at.GridLines.Length, at.ValueLabels.Length);
            Assert.All(at.GridLines.Zip(at.ValueLabels), pair => Assert.InRange(pair.Second - pair.First, -1, 1));
            Assert.Equal(10, at.Markers.Length);
            var shown = at.PlaceLabels.Select((label, place) => (label, place)).Where(one => one.label is not null)
                .ToList();
            Assert.True(shown.Count >= 2, $"{shown.Count} place labels show at {width} pixels, {direction}.");
            Assert.All(shown, one =>
                Assert.InRange((one.label![0] + one.label[1]) / 2 - at.Markers[one.place], -1, 1));
            Assert.Equal(direction == "rtl", at.Markers[0] > at.Markers[^1]);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PlaceLabels_ShouldNotOverlap_WhenTheChartIsNarrow(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/long");
        var chart = Chart(page, "Hikers a day this season");

        await page.SetViewportSizeAsync(1280, 800);
        var wide = await ShownPlaceLabelsAsync(chart);
        await page.SetViewportSizeAsync(400, 800);
        var narrow = await ShownPlaceLabelsAsync(chart);

        foreach (var labels in new[] { wide, narrow })
        {
            Assert.True(labels.Count >= 2, $"{labels.Count} place labels show.");
            Assert.All(labels.Zip(labels.Skip(1)), pair => Assert.True(pair.Second[0] >= pair.First[1],
                $"A label starts at {pair.Second[0]}, before the one before it ends at {pair.First[1]}."));
        }

        Assert.True(wide.Count > narrow.Count, $"{wide.Count} labels show wide and {narrow.Count} narrow.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Series_ShouldDifferWithoutColor_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/lines", new() { ForcedColors = ForcedColors.Active });
        var text = await SystemColorAsync(page, "CanvasText");
        var lines = Chart(page, "Trail temperature").Locator("[data-part=plot] .tb-chart__line");

        await Expect(lines).ToHaveCountAsync(2);
        await Expect(lines.First).ToHaveCSSAsync("stroke", text);
        await Expect(lines.Last).ToHaveCSSAsync("stroke", text);
        await Expect(lines.First).ToHaveCSSAsync("stroke-dasharray", "none");
        await Expect(lines.Last).Not.ToHaveCSSAsync("stroke-dasharray", "none");
        // The legend's keys are drawn as their series' lines are, so a reader can tell which is which.
        var keys = Chart(page, "Trail temperature").Locator("[data-part=legend] line");
        Assert.Equal(await lines.EvaluateAllAsync<string[]>(Dashes), await keys.EvaluateAllAsync<string[]>(Dashes));

        await page.GotoAsync(new Uri(new Uri(page.Url), "/examples/chart/stacking").ToString());
        var bars = await Chart(page, "Passes sold by season").Locator("g[data-series]")
            .EvaluateAllAsync<string[]>(FillsAndDashes);

        Assert.Equal(3, bars.Distinct().Count());
        Assert.StartsWith(text, bars[0]);
        Assert.Equal(bars, await Chart(page, "Passes sold by season").Locator("[data-part=legend] svg")
            .EvaluateAllAsync<string[]>(FillsAndDashes));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Table_ShouldOpenFromTheKeyboard_WhenItsDisclosureHasFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/basic");
        var chart = Chart(page, "Hikers by month");
        await Expect(chart.GetByRole(AriaRole.Table)).ToBeHiddenAsync();

        await chart.Locator("summary").FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(chart.GetByRole(AriaRole.Table, new() { Name = "Hikers by month" })).ToBeVisibleAsync();
        await Expect(chart.GetByRole(AriaRole.Row, new() { Name = "Jul 1,460" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chart_ShouldDrawAndOpenItsTable_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-chart");
        var chart = Chart(page, "Trail temperature");

        var at = await chart.EvaluateAsync<ChartPositions>(Positions);
        var line = await chart.Locator("[data-part=plot] .tb-chart__line").First.BoundingBoxAsync();
        await chart.Locator("summary").ClickAsync();

        Assert.Equal(10, at.Markers.Length);
        Assert.True(at.PlaceLabels.Count(label => label is not null) >= 2);
        Assert.True(line!.Width > 100 && line.Height > 20, $"The line is {line.Width} by {line.Height}.");
        await Expect(chart.GetByRole(AriaRole.Row)).ToHaveCountAsync(11);
    }

    private static async Task<List<double[]>> ShownPlaceLabelsAsync(ILocator chart) =>
        [.. (await chart.EvaluateAsync<ChartPositions>(Positions)).PlaceLabels.OfType<double[]>()
            .OrderBy(label => label[0])];

    // Playwright fills this from the script's answer, which needs a constructor without parameters.
    public sealed class ChartPositions
    {
        public double[] ValueLabels { get; set; } = [];

        public double[] GridLines { get; set; } = [];

        public double[]?[] PlaceLabels { get; set; } = [];

        public double[] Markers { get; set; } = [];
    }
}
