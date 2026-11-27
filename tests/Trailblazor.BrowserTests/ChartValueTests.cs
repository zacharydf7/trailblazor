using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Values written on a chart's marks.
public class ChartValueTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Values_ShouldSitPastTheBarsEndsClearOfTheTitleAndTheNames_WhenThePageRunsEitherWay(string browser)
    {
        var page = await OpenAsync(browser, "/chart-values");
        var chart = page.Tb().Chart("Change by month").Chart;
        var bars = chart.Locator("g[data-series] rect");
        var labels = chart.Locator(".tb-chart__values > span");
        await Expect(labels).ToHaveTextAsync(["12", "-10", "20", "5"]);

        foreach (var direction in new[] { "ltr", "rtl" })
        {
            await page.EvaluateAsync("direction => { document.documentElement.dir = direction; }", direction);
            for (var place = 0; place < 4; place++)
            {
                var (bar, label) = (await bars.Nth(place).BoundingBoxAsync(),
                    await labels.Nth(place).BoundingBoxAsync());
                // Centered on its bar, over the end of a bar that grows up and under one that hangs down.
                Assert.True(label!.Height > 10, $"Label {place} is hidden, {direction}.");
                Assert.InRange(label.X + label.Width / 2 - (bar!.X + bar.Width / 2), -1, 1);
                var gap = place == 1 ? label.Y - (bar.Y + bar.Height) : bar.Y - (label.Y + label.Height);
                Assert.InRange(gap, -1, 1);
            }
        }

        // The tallest bar reaches the plot's top and the lowest its bottom, and their labels stay clear of the title
        // and of the names under the plot.
        var (tallest, lowest) = (await labels.Nth(2).BoundingBoxAsync(), await labels.Nth(1).BoundingBoxAsync());
        var title = await chart.Locator("[data-part=title]").BoundingBoxAsync();
        var names = await chart.Locator(".tb-chart__x-row").BoundingBoxAsync();
        Assert.True(tallest!.Y >= title!.Y + title.Height - 0.5,
            $"The label at {tallest.Y} runs into the title, which ends at {title.Y + title.Height}.");
        Assert.True(lowest!.Y + lowest.Height <= names!.Y + 0.5,
            $"The label ending at {lowest.Y + lowest.Height} runs into the names, which start at {names.Y}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Values_ShouldAllBeHidden_WhenTheBarsAreNarrowerThanTheLongestLabel(string browser)
    {
        var page = await OpenAsync(browser, "/chart-values");
        var labels = page.Tb().Chart("Too narrow").Chart.Locator(".tb-chart__values > span");
        await Expect(labels).ToHaveCountAsync(4);

        for (var place = 0; place < 4; place++)
        {
            Assert.InRange((await labels.Nth(place).BoundingBoxAsync())!.Width, 0, 0.5);
        }

        // Given room, the same chart shows them all.
        await page.Tb().Chart("Too narrow").Chart.EvaluateAsync(
            "chart => { chart.parentElement.style.width = '30rem'; }");

        for (var place = 0; place < 4; place++)
        {
            Assert.True((await labels.Nth(place).BoundingBoxAsync())!.Width > 10, $"Label {place} is still hidden.");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StackedValues_ShouldBeInTheMiddleOfEachBar_WhenTheBarIsALineOfTextHigh(string browser)
    {
        var page = await OpenAsync(browser, "/chart-values");
        var chart = page.Tb().Chart("Stacked by month").Chart;
        var bar = await chart.Locator("g[data-series='Free'] rect").Nth(1).BoundingBoxAsync();
        var label = await chart.Locator(".tb-chart__values > span").Filter(new() { HasText = "16" })
            .BoundingBoxAsync();

        Assert.True(label!.Height > 10, "The label is hidden.");
        Assert.InRange(label.X + label.Width / 2 - (bar!.X + bar.Width / 2), -1, 1);
        Assert.InRange(label.Y + label.Height / 2 - (bar.Y + bar.Height / 2), -1, 1);

        // Made too low for a line of text, a stacked bar's label is hidden.
        await chart.EvaluateAsync("chart => { chart.style.setProperty('--tb-chart-height', '3rem'); }");

        var small = await chart.Locator(".tb-chart__values > span").Filter(new() { HasTextRegex = new("^4$") })
            .BoundingBoxAsync();
        Assert.InRange(small!.Height, 0, 0.5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Values_ShouldBeBesideTheBarsEnds_WhenTheChartIsHorizontalInEitherDirection(string browser)
    {
        var page = await OpenAsync(browser, "/chart-values");
        var chart = page.Tb().Chart("Booked across").Chart;

        foreach (var direction in new[] { "ltr", "rtl" })
        {
            await page.EvaluateAsync("direction => { document.documentElement.dir = direction; }", direction);
            var bar = await chart.Locator("g[data-series] rect").Nth(2).BoundingBoxAsync();
            var label = await chart.Locator(".tb-chart__values > span").Nth(2).BoundingBoxAsync();
            var figure = await chart.BoundingBoxAsync();

            // Past the bar's end along the plot (on its left when right to left), level with the bar, and inside the
            // figure even for the longest bar.
            Assert.True(label!.Height > 10, $"The label is hidden, {direction}.");
            var gap = direction == "ltr" ? label.X - (bar!.X + bar.Width) : bar!.X - (label.X + label.Width);
            Assert.InRange(gap, 0, 4);
            Assert.InRange(label.Y + label.Height / 2 - (bar.Y + bar.Height / 2), -1, 1);
            Assert.True(label.X >= figure!.X && label.X + label.Width <= figure.X + figure.Width,
                $"The label runs out of the figure, {direction}.");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Values_ShouldStayInsideTheFigureAndClearOfTheNames_WhenHorizontalBarsReachEitherEnd(
        string browser)
    {
        var page = await OpenAsync(browser, "/chart-values");
        var change = page.Tb().Chart("Change across").Chart;
        var lowest = await change.Locator(".tb-chart__values > span").Nth(1).BoundingBoxAsync();
        var names = await change.Locator(".tb-chart__y").BoundingBoxAsync();
        var longChart = page.Tb().Chart("Long across").Chart;
        var longest = await longChart.Locator(".tb-chart__values > span").Nth(2).BoundingBoxAsync();
        var figure = await longChart.BoundingBoxAsync();

        // The bar of -10 reaches the plot's start, and its label stays clear of the names beside it.
        Assert.True(lowest!.Width > 10, "The label is hidden.");
        Assert.True(lowest.X >= names!.X + names.Width - 0.5,
            $"The label at {lowest.X} runs into the names, which end at {names.X + names.Width}.");
        // A label of eleven characters past the longest bar stays inside the figure.
        await Expect(longChart.Locator(".tb-chart__values > span").Nth(2)).ToHaveTextAsync("36,000 beds");
        Assert.True(longest!.X + longest.Width <= figure!.X + figure.Width + 0.5,
            $"The label ends at {longest.X + longest.Width}, past the figure's end at {figure.X + figure.Width}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task EdgeValues_ShouldStayInsideThePlotAndClearOfTheirNeighbors_WhenThePageRunsEitherWay(
        string browser)
    {
        var page = await OpenAsync(browser, "/chart-values");
        var chart = page.Tb().Chart("Level along").Chart;
        var labels = chart.Locator(".tb-chart__values > span");

        foreach (var direction in new[] { "ltr", "rtl" })
        {
            await page.EvaluateAsync("direction => { document.documentElement.dir = direction; }", direction);
            var plot = await chart.Locator("[data-part=plot]").BoundingBoxAsync();
            var (first, second) = (await labels.Nth(0).BoundingBoxAsync(), await labels.Nth(1).BoundingBoxAsync());
            var (third, last) = (await labels.Nth(2).BoundingBoxAsync(), await labels.Nth(3).BoundingBoxAsync());

            // The first and last labels start and end at the plot's edges, on its right first when right to left,
            // and each is clear of its neighbor's.
            Assert.True(first!.Width > 10 && last!.Width > 10, $"An edge label is hidden, {direction}.");
            Assert.True(first.X >= plot!.X - 0.5 && last.X + last.Width <= plot.X + plot.Width + 0.5,
                $"An edge label runs out of the plot, {direction}.");
            var (left, right) = direction == "ltr" ? (first, second!) : (second!, first);
            Assert.True(left.X + left.Width <= right.X + 0.5, $"The first two labels overlap, {direction}.");
            (left, right) = direction == "ltr" ? (third!, last) : (last, third!);
            Assert.True(left.X + left.Width <= right.X + 0.5, $"The last two labels overlap, {direction}.");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task LineValues_ShouldSitOverTheirMarkers_WhenDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/chart-values");
        var chart = page.Tb().Chart("Level by month").Chart;
        var marker = await chart.Locator(".tb-chart__marker").Nth(1).BoundingBoxAsync();
        var label = await chart.Locator(".tb-chart__values > span").Nth(1).BoundingBoxAsync();

        Assert.InRange(label!.X + label.Width / 2 - (marker!.X + marker.Width / 2), -1, 1);
        Assert.True(label.Y + label.Height <= marker.Y + 0.5,
            $"The label ends at {label.Y + label.Height}, over the marker at {marker.Y}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Values_ShouldBeInTheTextColorAndLeaveThePointerToTheMarks_WhenDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/chart-values");
        var label = page.Tb().Chart("Change by month").Chart.Locator(".tb-chart__values > span").First;

        await Expect(label).ToHaveCSSAsync("color", await SystemColorAsync(page, "var(--tb-color-text)"));
        await Expect(label).Not.ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(page.Locator(".tb-chart__values").First).ToHaveCSSAsync("pointer-events", "none");
    }
}
