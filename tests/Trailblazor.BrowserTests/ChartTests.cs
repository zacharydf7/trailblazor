using Trailblazor.Testing.Playwright;

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
    public async Task Keys_ShouldMoveBetweenNamedPoints_WhenThePlotHasFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/lines");
        var chart = Chart(page, "Trail temperature");
        var plot = chart.Locator("[data-part=plot]");
        var tooltip = chart.Locator("[data-part=tooltip]");
        var focused = page.Locator(":focus");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");

        // The plot is one tab stop, on its first point until another has been visited.
        await Expect(plot.Locator("[tabindex='0']")).ToHaveCountAsync(1);
        await plot.Locator("[tabindex='0']").FocusAsync();
        await Expect(focused).ToHaveRoleAsync(AriaRole.Img);
        await Expect(focused).ToHaveAccessibleNameAsync("Jul 1, Summit: 14, 1 of 10");
        await Expect(tooltip).ToContainTextAsync("Jul 1");

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(focused).ToHaveAccessibleNameAsync("Jul 2, Summit: 15, 2 of 10");
        await Expect(tooltip).ToContainTextAsync("Jul 2");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(focused).ToHaveAccessibleNameAsync("Jul 2, Valley: 24, 2 of 10");
        await page.Keyboard.PressAsync("End");
        await Expect(focused).ToHaveAccessibleNameAsync("Jul 10, Valley: 26, 10 of 10");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(focused).ToHaveAccessibleNameAsync("Jul 10, Valley: 26, 10 of 10");
        await page.Keyboard.PressAsync("Home");
        await Expect(focused).ToHaveAccessibleNameAsync("Jul 1, Valley: 22, 1 of 10");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(focused).ToHaveAccessibleNameAsync("Jul 1, Summit: 14, 1 of 10");
        await Expect(plot.Locator("[tabindex='0']")).ToHaveCountAsync(1);

        await page.Keyboard.PressAsync("Escape");
        await Expect(tooltip).ToBeHiddenAsync();
        await Expect(focused).ToHaveRoleAsync(AriaRole.Img);

        // The point focus left is no longer an image of its own.
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(focused).ToHaveAccessibleNameAsync("Jul 2, Summit: 15, 2 of 10");
        await Expect(plot.GetByRole(AriaRole.Img)).ToHaveCountAsync(2);

        // The next tab stop after the plot is the data table's disclosure, and Shift+Tab comes back to the point last
        // visited, which is still the plot's one tab stop.
        await page.Keyboard.PressAsync("Tab");
        await Expect(focused).ToHaveTextAsync("Data table");
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(focused).ToHaveAccessibleNameAsync("Jul 2, Summit: 15, 2 of 10");
        await Expect(plot.Locator("[tabindex='0']")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldSwapLeftAndRight_WhenTheChartRunsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/lines");
        await page.EvaluateAsync("() => { document.documentElement.dir = 'rtl'; }");
        var plot = Chart(page, "Trail temperature").Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        await plot.Locator("[tabindex='0']").FocusAsync();
        var first = await page.Locator(":focus").BoundingBoxAsync();

        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(page.Locator(":focus")).ToHaveAccessibleNameAsync("Jul 2, Summit: 15, 2 of 10");
        var second = await page.Locator(":focus").BoundingBoxAsync();
        Assert.True(second!.X < first!.X, $"The second point is at {second.X}, and the first at {first.X}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldStepOverAGap_WhenASeriesHasNoValueAtAPlace(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/gaps");
        var plot = Chart(page, "Snow depth at the hut").Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        await plot.Locator("[tabindex='0']").FocusAsync();
        var focused = page.Locator(":focus");
        await Expect(focused).ToHaveAccessibleNameAsync("Mon, Measured: 62, 1 of 7");

        // The forecast has no value on Wednesday, and nothing was measured on Thursday.
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(focused).ToHaveAccessibleNameAsync("Wed, Measured: 51, 3 of 7");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(focused).ToHaveAccessibleNameAsync("Wed, Measured: 51, 3 of 7");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(focused).ToHaveAccessibleNameAsync("Fri, Measured: 40, 5 of 7");
        await Expect(plot.Locator("[data-part=tooltip]")).ToContainTextAsync("Forecast43");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldShowEverySeriesValueAtThePlace_WhenThePointerIsOverThePlot(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/gaps");
        var chart = Chart(page, "Snow depth at the hut");
        var plot = chart.Locator("[data-part=plot]");
        var tooltip = chart.Locator("[data-part=tooltip]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();

        // Thursday is the fourth of seven places, and has a forecast but no measurement.
        await page.Mouse.MoveAsync(box!.X + box.Width * 3.5f / 7, box.Y + 10);
        await Expect(tooltip).ToBeVisibleAsync();
        await Expect(tooltip.Locator(".tb-chart__tooltip-place")).ToHaveTextAsync("Thu");
        await Expect(tooltip.Locator(".tb-chart__tooltip-row")).ToHaveTextAsync(["MeasuredNo value", "Forecast48"]);
        var line = await plot.Locator(".tb-chart__crosshair").BoundingBoxAsync();
        Assert.InRange(line!.X - (box.X + box.Width * 3.5f / 7), -1.5, 1.5);

        await page.Mouse.MoveAsync(box.X + box.Width * 0.5f / 7, box.Y + 10);
        await Expect(tooltip.Locator(".tb-chart__tooltip-place")).ToHaveTextAsync("Mon");

        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height + 80);
        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Point_ShouldTellTheApp_WhenClickedOrChosenWithTheKeys(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/click");
        var plot = Chart(page, "Rescues by month").Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();

        // July is the third of five places; a click anywhere over it picks its bar.
        await page.Mouse.ClickAsync(box!.X + box.Width * 2.5f / 5, box.Y + 10);
        await Expect(page.GetByText("Jul: 9 rescues, most of them on July 4.")).ToBeVisibleAsync();

        await plot.Locator("[tabindex='0']").FocusAsync();
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByText("Sep: 3 rescues, most of them on September 6.")).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Home");
        await page.Keyboard.PressAsync("Space");
        await Expect(page.GetByText("May: 2 rescues, most of them on May 24.")).ToBeVisibleAsync();

        // A screen reader's click on the point that has focus has no position, and picks that point.
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Locator(":focus").EvaluateAsync("point => point.click()");
        await Expect(page.GetByText("Jun: 5 rescues, most of them on June 14.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Point_ShouldBeTheBarUnderThePointer_WhenBarsStandSideBySide(string browser)
    {
        var page = await OpenAsync(browser, "/chart-click");
        var plot = Chart(page, "Visits").Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();

        // Low inside Saturday's tall evening bar, where the short morning bar's end is the nearer of the two.
        await page.Mouse.ClickAsync(box!.X + box.Width * 0.225f, box.Y + box.Height * 0.95f);
        await Expect(page.GetByTestId("picked")).ToHaveTextAsync("Evening Sat 10");

        // Over the morning bar's column but above the bar, which no mark is under: the nearest end is its own.
        await page.Mouse.ClickAsync(box.X + box.Width * 0.108f, box.Y + box.Height * 0.4f);
        await Expect(page.GetByTestId("picked")).ToHaveTextAsync("Morning Sat 2");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ClickPointAsync_ShouldPickThePoint_WhenItIsOnThePlotsBottomEdge(string browser)
    {
        var page = await OpenAsync(browser, "/chart-click");

        // Monday's morning bar has no height, so its point is on the plot's bottom edge, where the click lands on the
        // grid line drawn there.
        await page.Tb().Chart("Visits").ClickPointAsync("Morning", "Mon");

        await Expect(page.GetByTestId("picked")).ToHaveTextAsync("Morning Mon 0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Points_ShouldFollowTheData_WhenTheItemsChange(string browser)
    {
        var page = await OpenAsync(browser, "/chart-click");
        var plot = Chart(page, "Visits").Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        await plot.Locator("[tabindex='0']").FocusAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(page.Locator(":focus")).ToHaveAccessibleNameAsync("Mon, Morning: 0, 3 of 3");

        // Monday goes, so the tab stop goes back to the first point, which is now one of two.
        await page.GetByRole(AriaRole.Button, new() { Name = "Two days" }).ClickAsync();
        await Expect(plot.Locator("[tabindex='0']")).ToHaveAccessibleNameAsync("Sat, Morning: 2, 1 of 2");

        // With nothing to draw, the plot has no tab stop and nothing named.
        await page.GetByRole(AriaRole.Button, new() { Name = "No days" }).ClickAsync();
        await Expect(plot).ToContainTextAsync("No data");
        await Expect(plot.Locator("[tabindex], [role]")).ToHaveCountAsync(0);
        await Expect(plot.Locator("[data-part=tooltip]")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Escape_ShouldHideTheTooltipAndLeaveTheDialogOpen_WhenTheChartIsInADialog(string browser)
    {
        var page = await OpenAsync(browser, "/chart-click");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open trend" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Trend" });
        var chart = Chart(page, "Evenings and mornings");
        var plot = chart.Locator("[data-part=plot]");
        var tooltip = chart.Locator("[data-part=tooltip]");
        var note = dialog.GetByRole(AriaRole.Button, new() { Name = "Note" });
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");

        // The line is given before the bars, and comes first for the keys and in the tooltip as it does in the legend.
        await plot.Locator("[tabindex='0']").FocusAsync();
        await Expect(page.Locator(":focus")).ToHaveAccessibleNameAsync("Sat, Evening: 10, 1 of 3");
        await Expect(tooltip.Locator(".tb-chart__tooltip-name")).ToHaveTextAsync(["Evening", "Morning"]);
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.Locator(":focus")).ToHaveAccessibleNameAsync("Sat, Morning: 2, 1 of 3");

        // If Escape closed the dialog, the click on its Note button below would fail.
        await page.Keyboard.PressAsync("Escape");
        await Expect(tooltip).ToBeHiddenAsync();
        await note.ClickAsync();
        await Expect(dialog.GetByTestId("noted")).ToHaveTextAsync("1");

        // With focus elsewhere in the dialog, Escape hides the tooltip the pointer shows, and the next one closes the
        // dialog.
        var box = await plot.BoundingBoxAsync();
        await page.Mouse.MoveAsync(box!.X + box.Width / 2, box.Y + 10);
        await Expect(tooltip).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(tooltip).ToBeHiddenAsync();
        await note.ClickAsync();
        await Expect(dialog.GetByTestId("noted")).ToHaveTextAsync("2");
        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Escape_ShouldCancelAnEditFirst_WhenThePointerRestsOnAChartInTheSameDialog(string browser)
    {
        var page = await OpenAsync(browser, "/chart-click");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open trend" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Trend" });
        var chart = Chart(page, "Evenings and mornings");
        var plot = chart.Locator("[data-part=plot]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var name = page.Tb().InPlaceEditor("Trend name");
        await name.EditAsync();
        var box = await plot.BoundingBoxAsync();
        await page.Mouse.MoveAsync(box!.X + box.Width / 2, box.Y + 10);
        await Expect(chart.Locator("[data-part=tooltip]")).ToBeVisibleAsync();

        // The field being edited hears Escape before the chart and the dialog do, and keeps it.
        await page.Keyboard.PressAsync("Escape");

        await Expect(name.InPlaceEditor.Locator("[data-part=editor]")).ToHaveCountAsync(0);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Note" }).ClickAsync();
        await Expect(dialog.GetByTestId("noted")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Legend_ShouldHideAndShowASeries_WhenItsButtonIsPressedWithTheKeys(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/lines");
        var chart = Chart(page, "Trail temperature");
        var plot = chart.Locator("[data-part=plot]");
        var summit = chart.GetByRole(AriaRole.Button, new() { Name = "Summit" });
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        await Expect(summit).ToHaveAttributeAsync("aria-pressed", "true");

        // The tab stop is put on a point of the second series.
        await plot.Locator("[tabindex='0']").FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(page.Locator(":focus")).ToHaveAccessibleNameAsync("Jul 2, Valley: 24, 2 of 10");

        await summit.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(summit).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(summit).ToBeFocusedAsync();
        // The entry is struck through, so it doesn't differ from the others by color alone.
        await Expect(summit).ToHaveCSSAsync("text-decoration-line", "line-through");
        await Expect(plot.Locator("path[data-series]")).ToHaveCountAsync(1);
        await Expect(chart.GetByRole(AriaRole.Img, new() { Name = "Line chart: Valley", Exact = true }))
            .ToBeVisibleAsync();
        // The valley alone starts the axis at 20, where the two together started it at 10.
        await Expect(chart.Locator(".tb-chart__y > span:not(.tb-chart__y-sizer)").First).ToHaveTextAsync("20");
        // The tab stop stays with its own series, and the tooltip lists only the series that shows.
        await Expect(plot.Locator("[tabindex='0']")).ToHaveAccessibleNameAsync("Jul 2, Valley: 24, 2 of 10");
        var box = await plot.BoundingBoxAsync();
        await page.Mouse.MoveAsync(box!.X + box.Width / 2, box.Y + 10);
        await Expect(chart.Locator(".tb-chart__tooltip-name")).ToHaveTextAsync(["Valley"]);

        await page.Keyboard.PressAsync("Space");

        await Expect(summit).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(summit).ToHaveCSSAsync("text-decoration-line", "none");
        await Expect(plot.Locator("path[data-series]")).ToHaveCountAsync(2);
        await Expect(plot.Locator("[tabindex='0']")).ToHaveAccessibleNameAsync("Jul 2, Valley: 24, 2 of 10");

        // Tab goes on to the next legend button. Hiding the series the tab stop is on sends it to the first point.
        var valley = chart.GetByRole(AriaRole.Button, new() { Name = "Valley" });
        await page.Keyboard.PressAsync("Tab");
        await Expect(valley).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(valley).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(plot.Locator("[tabindex='0']")).ToHaveAccessibleNameAsync("Jul 1, Summit: 14, 1 of 10");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldHideTheSeriesOnceInteractive_WhenThePageIsStillPrerendered(string browser)
    {
        var (page, held) = await OpenHeldAsync(browser, "/examples/chart/lines");
        var chart = page.Tb().Chart("Trail temperature");

        var hiding = chart.HideSeriesAsync("Summit");
        await page.WaitForTimeoutAsync(300);
        held.SetResult();
        await hiding;

        Assert.True(await chart.IsSeriesHiddenAsync("Summit"));
        await Expect(chart.Chart.GetByRole(AriaRole.Button, new() { Name = "Summit" }))
            .ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStayInsideThePlot_WhenThePlotIsNarrow(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/stacking",
            new() { ViewportSize = new() { Width = 320, Height = 800 } });
        var chart = Chart(page, "Passes sold by season");
        var plot = chart.Locator("[data-part=plot]");
        var tooltip = chart.Locator("[data-part=tooltip]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();

        // The tooltip is on the side of the line with more room, and its names wrap when that isn't room enough.
        (float Share, string Place)[] spots = [(0.05f, "Spring"), (0.45f, "Summer"), (0.55f, "Fall"), (0.95f, "Winter")];
        foreach (var (share, place) in spots)
        {
            await page.Mouse.MoveAsync(box!.X + box.Width * share, box.Y + 10);
            await Expect(tooltip.Locator(".tb-chart__tooltip-place")).ToHaveTextAsync(place);
            var shown = await tooltip.BoundingBoxAsync();
            Assert.InRange(shown!.X, box.X - 1, box.X + box.Width);
            Assert.InRange(shown.X + shown.Width, box.X, box.X + box.Width + 1);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tap_ShouldShowTheTooltipAndPickThePoint_WhenThePlotIsTouched(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chart/click", new() { HasTouch = true });
        var chart = Chart(page, "Rescues by month");
        var plot = chart.Locator("[data-part=plot]");
        var tooltip = chart.Locator("[data-part=tooltip]");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = await plot.BoundingBoxAsync();

        await page.Touchscreen.TapAsync(box!.X + box.Width * 2.5f / 5, box.Y + box.Height / 2);

        await Expect(page.GetByText("Jul: 9 rescues, most of them on July 4.")).ToBeVisibleAsync();
        await Expect(tooltip.Locator(".tb-chart__tooltip-place")).ToHaveTextAsync("Jul");

        // The finger left the plot as it lifted, and the tooltip stays until a tap somewhere else.
        var text = await page.GetByText("Jul: 9 rescues").BoundingBoxAsync();
        await page.Touchscreen.TapAsync(text!.X + 5, text.Y + 5);
        await Expect(tooltip).ToBeHiddenAsync();
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
