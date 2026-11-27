using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Reference lines and bands: marks at values of a chart's value axis.
public class ChartReferenceTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string River = "River level at the ford this week";

    // Where on the page a line's two ends are. A line's box differs between browsers, which count its stroke or not.
    private const string Ends = """
        line => {
            const at = (x, y) => new DOMPoint(x.baseVal.value, y.baseVal.value).matrixTransform(line.getScreenCTM());
            const [from, to] = [at(line.x1, line.y1), at(line.x2, line.y2)];
            return [from.x, from.y, to.x, to.y];
        }
        """;

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task References_ShouldSitAtTheirValuesWithTheirTextOnThem_WhenDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/references");
        var chart = page.Tb().Chart(River).Chart;
        var plot = await chart.Locator("[data-part=plot]").BoundingBoxAsync();
        var line = await chart.Locator(".tb-chart__reference").EvaluateAsync<double[]>(Ends);
        var band = await chart.Locator(".tb-chart__band").BoundingBoxAsync();
        var limit = chart.Locator("[data-part=reference]").Filter(new() { HasText = "Flood warning" });
        var usual = chart.Locator("[data-part=reference]").Filter(new() { HasText = "Usual for June" });

        // The axis runs from 20, where the band starts, up to the line at 70, which no day's level reaches.
        Assert.InRange(line[1] - plot!.Y, -1, 1);
        Assert.InRange(line[3] - plot.Y, -1, 1);
        Assert.InRange(Math.Abs(line[2] - line[0]) - plot.Width, -1, 1);
        // The band runs from 20 to 45, the lower half of the plot.
        Assert.InRange(band!.Y - (plot.Y + plot.Height / 2), -1, 1);
        Assert.InRange(band.Y + band.Height - (plot.Y + plot.Height), -1, 1);
        // The line's text is at the plot's end, under the line since the line is at the top, and the band's text is
        // in the band's top corner.
        await Expect(limit).ToHaveTextAsync("Flood warning: 70 cm");
        await Expect(usual).ToHaveTextAsync("Usual for June: 20 cm to 45 cm");
        var (limitBox, usualBox) = (await limit.BoundingBoxAsync(), await usual.BoundingBoxAsync());
        Assert.InRange(limitBox!.X + limitBox.Width - (plot.X + plot.Width), -1, 1);
        Assert.InRange(limitBox.Y - plot.Y, -1, 1);
        Assert.InRange(usualBox!.Y - band.Y, -1, 1);
        Assert.Equal(["Usual for June: 20 cm to 45 cm", "Flood warning: 70 cm"],
            await page.Tb().Chart(River).ReferencesAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task References_ShouldTakeTheirTonesAndLeaveThePointerToTheMarks_WhenDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/references");
        var chart = page.Tb().Chart(River).Chart;

        await Expect(chart.Locator(".tb-chart__reference")).ToHaveCSSAsync("stroke",
            await SystemColorAsync(page, "var(--tb-color-danger-text)"));
        await Expect(chart.Locator(".tb-chart__band")).ToHaveCSSAsync("fill",
            await SystemColorAsync(page, "var(--tb-color-info-text)"));
        // The line is dashed, so it reads as neither a grid line nor a series' line.
        await Expect(chart.Locator(".tb-chart__reference")).Not.ToHaveCSSAsync("stroke-dasharray", "none");
        foreach (var part in new[] { ".tb-chart__reference", ".tb-chart__band", "[data-part=reference] >> nth=0" })
        {
            await Expect(chart.Locator(part)).ToHaveCSSAsync("pointer-events", "none");
        }

        // The text is in the text color whatever the reference's tone.
        await Expect(chart.Locator("[data-part=reference]").First).ToHaveCSSAsync("color",
            await SystemColorAsync(page, "var(--tb-color-text)"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task References_ShouldRunUpThePlotFromTheStart_WhenTheChartIsHorizontalInEitherDirection(
        string browser)
    {
        var page = await OpenAsync(browser, "/chart-reference");
        var chart = page.Tb().Chart("Hikers by trail").Chart;

        foreach (var direction in new[] { "ltr", "rtl" })
        {
            await page.EvaluateAsync("direction => { document.documentElement.dir = direction; }", direction);
            var plot = await chart.Locator("[data-part=plot]").BoundingBoxAsync();
            var line = await chart.Locator(".tb-chart__reference").First.EvaluateAsync<double[]>(Ends);
            var band = await chart.Locator(".tb-chart__band").BoundingBoxAsync();
            var limit = await chart.Locator("[data-part=reference]").Filter(new() { HasText = "Limit" })
                .BoundingBoxAsync();
            var record = await chart.Locator("[data-part=reference]").Filter(new() { HasText = "Record" })
                .BoundingBoxAsync();
            var usual = await chart.Locator("[data-part=reference]").Filter(new() { HasText = "Usual" })
                .BoundingBoxAsync();
            // How far a value is from the plot's left: values start at the right when right to left.
            double Across(double share) => plot!.X + plot.Width * (direction == "ltr" ? share : 1 - share);

            // The axis runs from 0 to 40, so the line at 30 is three quarters of the way across and runs from the
            // plot's top to its bottom.
            Assert.InRange(line[0] - Across(0.75), -1, 1);
            Assert.InRange(line[2] - Across(0.75), -1, 1);
            Assert.InRange(Math.Abs(line[3] - line[1]) - plot!.Height, -1, 1);
            // The band runs from a quarter to a half of the way.
            Assert.InRange(band!.X + band.Width / 2 - Across(0.375), -1, 1);
            Assert.InRange(band.Width - plot.Width / 4, -1, 1);
            // Past the middle of the plot a line's text ends at its line, so the text of the line at the axis's end
            // stays inside the plot. Lines' texts are at the plot's bottom.
            double End(LocatorBoundingBoxResult box) => direction == "ltr" ? box.X + box.Width : box.X;
            double Start(LocatorBoundingBoxResult box) => direction == "ltr" ? box.X : box.X + box.Width;
            Assert.InRange(End(limit!) - Across(0.75), -1.5, 1.5);
            Assert.InRange(End(record!) - Across(1), -1.5, 1.5);
            Assert.InRange(limit!.Y + limit.Height - (plot.Y + plot.Height), -1, 1);
            // A band's text starts at the band's lower edge, at the plot's top, clear of the lines' texts.
            Assert.InRange(Start(usual!) - Across(0.25), -1.5, 1.5);
            Assert.InRange(usual!.Y - plot.Y, -1, 1);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Reference_ShouldBeInTheMutedTextColor_WhenItHasNoTone(string browser)
    {
        var page = await OpenAsync(browser, "/chart-reference");
        var chart = page.Tb().Chart("Hikers by trail").Chart;
        var muted = await SystemColorAsync(page, "var(--tb-color-text-muted)");

        await Expect(chart.Locator(".tb-chart__band")).ToHaveCSSAsync("fill", muted);
        await Expect(chart.Locator(".tb-chart__reference").Last).ToHaveCSSAsync("stroke", muted);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task References_ShouldKeepTheirShapesInSystemColors_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/references",
            new() { ForcedColors = ForcedColors.Active });
        var chart = page.Tb().Chart(River).Chart;
        var text = await SystemColorAsync(page, "CanvasText");

        // The line keeps its dashes, and the band has a dotted outline in place of its tint.
        await Expect(chart.Locator(".tb-chart__reference")).ToHaveCSSAsync("stroke", text);
        await Expect(chart.Locator(".tb-chart__reference")).Not.ToHaveCSSAsync("stroke-dasharray", "none");
        await Expect(chart.Locator(".tb-chart__band")).ToHaveCSSAsync("fill", "none");
        await Expect(chart.Locator(".tb-chart__band")).ToHaveCSSAsync("stroke", text);
    }
}
