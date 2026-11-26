using System.Globalization;
using System.Text.Json;

namespace Trailblazor.Spikes.Tests.Charts;

// Spike E. See spikes/charts/README.md.
public class ChartTests(SpikeFixture fixture) : SpikeTest(fixture)
{
    private const double Frame = 16.75;

    // Reads how much ink a screenshot has in columns, rows and boxes of pixels: 1 for black, 0 for white.
    private const string MeasureInk = """
        async ({ png, columns, rows, boxes }) => {
            const image = new Image();
            image.src = 'data:image/png;base64,' + png;
            await image.decode();
            const canvas = document.createElement('canvas');
            canvas.width = image.width;
            canvas.height = image.height;
            const context = canvas.getContext('2d');
            context.drawImage(image, 0, 0);
            const ink = (x, y, width, height) => {
                const data = context.getImageData(x, y, width, height).data;
                const out = [];
                for (let i = 0; i < data.length; i += 4) {
                    out.push(1 - (data[i] + data[i + 1] + data[i + 2]) / 765);
                }
                return out;
            };
            const sum = list => list.reduce((total, one) => total + one, 0);
            return {
                columns: columns.map(c => sum(ink(c.x, c.y, 1, c.height))),
                rows: rows.map(r => ink(r.x, r.y, r.width, 1)),
                boxes: boxes.map(b => sum(ink(b.x, b.y, b.width, b.height))),
            };
        }
        """;

    private static readonly (int Width, int Height)[] Sizes = [(306, 160), (900, 300), (1660, 480)];

    public static TheoryData<string> Browsers => new(BrowsersAndModes.Browsers);

    public static TheoryData<string, string> BrowsersAndShapes => new(
        from browser in BrowsersAndModes.Browsers
        from shape in new[] { "line", "columns", "stacked", "large" }
        select (browser, shape));

    public static TheoryData<string, int, int> BrowsersAndSizes => new(
        from browser in BrowsersAndModes.Browsers
        from size in Sizes
        select (browser, size.Width, size.Height));

    public static TheoryData<string, string, int, int> BrowsersDirectionsAndSizes => new(
        from browser in BrowsersAndModes.Browsers
        from direction in new[] { "ltr", "rtl" }
        from size in Sizes
        select (browser, direction, size.Width, size.Height));

    public static TheoryData<string, string, bool> BrowsersShapesAndScript => new(
        from browser in BrowsersAndModes.Browsers
        from shape in new[] { "line", "columns", "large" }
        from script in new[] { true, false }
        select (browser, shape, script));

    public static TheoryData<string, string> BrowsersAndSchemes => new(
        from browser in BrowsersAndModes.Browsers
        from scheme in new[] { "light", "dark" }
        select (browser, scheme));

    // Criterion 1.
    [Theory, MemberData(nameof(BrowsersAndShapes))]
    public async Task Chart_ShouldShowItsMarksLabelsLegendAndTable_WhenScriptIsOff(string browser, string shape)
    {
        var page = await OpenAsync(browser, "server", $"/chart?shape={shape}", options: WithoutScript());
        var chart = page.Locator(".spike-chart");
        var seriesCount = shape == "large" ? 5 : 3;

        var marks = await page.EvaluateAsync<double[][]>("""
            () => [...document.querySelectorAll('[data-series]')].map(element => {
                const box = element.getBoundingClientRect();
                return [box.width, box.height];
            })
            """);
        Assert.Equal(seriesCount, marks.Length);
        Assert.All(marks, size => Assert.True(size[0] > 100 && size[1] > 2, $"A series is {size[0]} by {size[1]}."));
        Assert.True(await CountShownAsync(page, ".spike-chart__y > span") >= 4);
        Assert.True(await CountShownAsync(page, ".spike-chart__x > span") >= 2);
        Assert.True(await CountShownAsync(page, ".spike-chart__grid") >= 3);
        await Expect(chart.GetByRole(AriaRole.Button)).ToHaveCountAsync(seriesCount);

        // The large chart leaves its table out of the markup and draws it when opened, which needs script.
        if (shape != "large")
        {
            await chart.Locator("summary").ClickAsync();
            await Expect(chart.GetByRole(AriaRole.Table)).ToBeVisibleAsync();
            await Expect(chart.Locator("tbody tr")).ToHaveCountAsync(shape == "line" ? 30 : 12);
        }
    }

    // Criterion 1, for the donut.
    [Theory, MemberData(nameof(Browsers))]
    public async Task Donut_ShouldShowItsPartsAndLegend_WhenScriptIsOff(string browser)
    {
        var page = await OpenAsync(browser, "server", "/chart?shape=donut", options: WithoutScript());

        var parts = await page.EvaluateAsync<double[]>("""
            () => [...document.querySelectorAll('.spike-donut path')].map(part => part.getBoundingClientRect().width)
            """);

        Assert.Equal(5, parts.Length);
        Assert.All(parts, width => Assert.True(width > 5, $"A part is {width} wide."));
        await Expect(page.Locator(".spike-chart__legend li")).ToHaveCountAsync(5);
    }

    // Criterion 2.
    [Theory, MemberData(nameof(BrowsersAndSizes))]
    public async Task Marks_ShouldKeepTheirThicknessDashesAndCorners_WhenThePlotIsAnySize(string browser, int width,
        int height)
    {
        var page = await OpenAsync(browser, "server", $"/chart?shape=probe&width={width}&height={height}",
            options: Wide());
        var plot = await BoxAsync(page, ".spike-chart__plot");
        int X(double fraction) => (int)Math.Round(plot.X + fraction * plot.Width);
        int Y(double fraction) => (int)Math.Round(plot.Y + fraction * plot.Height);

        // The slanted line rises a tenth of the height over each place; at 3/11 of the width its value is 25.
        var slantX = X(3d / 11);
        var slantValue = ((slantX - plot.X) / plot.Width * 11 - 0.5) * 10;
        var slantY = (int)Math.Round(plot.Y + (1 - slantValue / 100) * plot.Height);
        var slope = Math.Atan2(plot.Height / 10, plot.Width / 11);
        var ink = await MeasureAsync(page,
            columns: [new(X(2.5 / 11), Y(0.5) - 8, 16), new(slantX, slantY - 12, 24)],
            rows:
            [
                new(X(5d / 11) - 8, Y(0.9), 16),
                new(X(0.5 / 11), (int)Math.Floor(plot.Y + 0.2 * plot.Height - 0.5), X(6.5 / 11) - X(0.5 / 11)),
                new((int)plot.X - 4, (int)Math.Floor(plot.Y + 0.5 * plot.Height - 0.5), (int)plot.Width + 8),
            ],
            boxes: []);
        var flat = ink.Columns[0];
        var slant = ink.Columns[1] * Math.Cos(slope);
        var upright = ink.Rows[0].Sum();
        var (dashes, gaps) = Runs(ink.Rows[1]);
        // The flat line runs between the middles of the first and last places, plus a round end of 1 pixel each.
        var inked = ink.Rows[2].Select((share, index) => (share, index)).Where(one => one.share > 0.5).ToList();
        var startsAt = (int)plot.X - 4 + inked[0].index - (plot.X + plot.Width * 0.5 / 11 - 1);
        var endsAt = (int)plot.X - 4 + inked[^1].index + 1 - (plot.X + plot.Width * 10.5 / 11 + 1);

        await page.GotoAsync(new Uri(Fixture.BaseAddress,
            $"/chart?shape=probe-columns&width={width}&height={height}").ToString());
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        // The corner's box against one along the top edge and one down the left edge: what a square corner would
        // ink is the product of what those two edges ink, however an engine trims or softens an edge.
        var column = await BoxAsync(page, ".spike-chart__bar");
        var corner = new Box((int)Math.Floor(column.X) - 1, (int)Math.Floor(column.Y) - 1, 8, 8);
        var edges = (await MeasureAsync(page, [], [],
            [corner, corner with { X = corner.X + 12 }, corner with { Y = corner.Y + 12 }])).Boxes;
        var cut = edges[1] * edges[2] / 64 - edges[0];

        await page.GotoAsync(new Uri(Fixture.BaseAddress,
            $"/chart?shape=line&width={width}&height={height}").ToString());
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        var markers = await page.EvaluateAsync<double[][]>("""
            () => [...document.querySelectorAll('.spike-chart__marker')].map(marker => {
                const box = marker.getBoundingClientRect();
                return [box.width, box.height];
            })
            """);

        RecordMeasurement(FormattableString.Invariant(
            $"Shapes ({browser}, plot {plot.Width:0}x{plot.Height:0}): flat {flat:0.00}, slant {slant:0.00}, ") +
            FormattableString.Invariant($"upright {upright:0.00}, dashes {JsonSerializer.Serialize(dashes)}, ") +
            FormattableString.Invariant($"gaps {JsonSerializer.Serialize(gaps)}, corner cut {cut:0.00} ") +
            FormattableString.Invariant($"(radius {Math.Sqrt(cut / (1 - Math.PI / 4)):0.0}), ") +
            FormattableString.Invariant($"line starts {startsAt:0.00} and ends {endsAt:0.00} from its places"));
        Assert.InRange(flat, 1.5, 2.5);
        Assert.InRange(slant, 1.5, 2.5);
        Assert.InRange(upright, 1.5, 2.5);
        Assert.InRange(startsAt, -1, 1);
        Assert.InRange(endsAt, -1, 1);
        Assert.True(dashes.Count >= 3, $"Only {dashes.Count} whole dashes were found.");
        Assert.All(dashes, dash => Assert.InRange(dash, 5, 7));
        Assert.All(gaps, gap => Assert.InRange(gap, 3, 5));
        // A square corner cuts nothing, and a 4-pixel radius cuts 16 - 4π, about 3.4 pixels of ink. Engines soften
        // the arc differently, so a radius from 3.1 to 4.4 pixels passes; one stretched with the plot would be about
        // six times larger at the widest size than at the narrowest.
        Assert.InRange(cut, 2.1, 4.1);
        Assert.Equal(90, markers.Length);
        // The box on the screen, so a stretched marker would show. Firefox's takes in the 2 pixel stroke.
        Assert.All(markers, size =>
        {
            Assert.InRange(size[0], 7.5, 10.5);
            Assert.InRange(size[1] - size[0], -1, 1);
        });
    }

    // Criterion 3.
    [Theory, MemberData(nameof(BrowsersDirectionsAndSizes))]
    public async Task Labels_ShouldSitOnTheirLinesAndPoints_WhenThePlotIsAnySizeAndDirection(string browser,
        string direction, int width, int height)
    {
        var page = await OpenAsync(browser, "server",
            $"/chart?shape=line&dir={direction}&width={width}&height={height}", options: Wide());

        var measured = await page.EvaluateAsync<Alignment>("""
            () => {
                const middle = element => {
                    const box = element.getBoundingClientRect();
                    return { x: box.left + box.width / 2, y: box.top + box.height / 2, width: box.width };
                };
                const all = selector => [...document.querySelectorAll(selector)].map(middle);
                const path = document.querySelector('path.spike-chart__series--1');
                const onScreen = path.getScreenCTM();
                let x = 0;
                let y = 0;
                const corners = [...path.getAttribute('d').matchAll(/[Ml](-?\d+) (-?\d+)/g)].map(match => {
                    x = match[0][0] === 'M' ? Number(match[1]) : x + Number(match[1]);
                    y = match[0][0] === 'M' ? Number(match[2]) : y + Number(match[2]);
                    return new DOMPoint(x, y).matrixTransform(onScreen);
                });
                const markers = all('circle.spike-chart__series--1');
                const width = selector => document.querySelector(selector).getBoundingClientRect().width;
                return {
                    widths: [width('.spike-chart__plot'), width('.spike-chart__marks'), width('.spike-chart__x')],
                    valueLabels: all('.spike-chart__y > span:not(.spike-chart__y-sizer)').map(one => one.y),
                    gridLines: all('.spike-chart__grid').map(one => one.y),
                    placeLabels: all('.spike-chart__x > span').map(one => one.width > 0 ? one.x : null),
                    markerXs: markers.map(one => one.x),
                    markerYs: markers.map(one => one.y),
                    cornerXs: corners.map(one => one.x),
                    cornerYs: corners.map(one => one.y),
                };
            }
            """);

        var labelDrift = measured.PlaceLabels.Select((label, place) => label - measured.MarkerXs[place])
            .Where(drift => drift is not null).Select(drift => drift!.Value).ToList();
        var cornerDrift = measured.CornerXs.Select((corner, place) => corner - measured.MarkerXs[place]).ToList();
        RecordMeasurement(FormattableString.Invariant(
            $"Alignment ({browser}, {direction}, {width}): widths {string.Join(" / ", measured.Widths)}, ") +
            FormattableString.Invariant($"labels {labelDrift.Min():0.00} to {labelDrift.Max():0.00}, ") +
            FormattableString.Invariant($"line {cornerDrift.Min():0.00} to {cornerDrift.Max():0.00} from the markers"));
        Assert.Equal(measured.GridLines.Length, measured.ValueLabels.Length);
        for (var index = 0; index < measured.GridLines.Length; index++)
        {
            Assert.InRange(measured.ValueLabels[index] - measured.GridLines[index], -1, 1);
        }

        var shown = 0;
        for (var place = 0; place < measured.PlaceLabels.Length; place++)
        {
            if (measured.PlaceLabels[place] is { } label)
            {
                shown++;
                Assert.InRange(label - measured.MarkerXs[place], -1, 1);
            }
        }

        Assert.True(shown >= 2, $"Only {shown} place labels show.");
        Assert.Equal(30, measured.CornerXs.Length);
        for (var place = 0; place < 30; place++)
        {
            Assert.InRange(measured.CornerXs[place] - measured.MarkerXs[place], -1, 1);
            Assert.InRange(measured.CornerYs[place] - measured.MarkerYs[place], -1, 1);
        }

        Assert.Equal(direction == "rtl", measured.MarkerXs[0] > measured.MarkerXs[^1]);
    }

    // Criterion 3, for columns: each stacked column is centered on its place.
    [Theory, MemberData(nameof(BrowsersDirectionsAndSizes))]
    public async Task Columns_ShouldSitOverTheirPlaceLabels_WhenThePlotIsAnySizeAndDirection(string browser,
        string direction, int width, int height)
    {
        var page = await OpenAsync(browser, "server",
            $"/chart?shape=stacked&dir={direction}&width={width}&height={height}", options: Wide());

        var middles = await page.EvaluateAsync<double?[][]>("""
            () => {
                const middle = element => {
                    const box = element.getBoundingClientRect();
                    return box.width > 0 ? box.left + box.width / 2 : null;
                };
                return [
                    [...document.querySelectorAll('g[data-series="0"] rect')].map(middle),
                    [...document.querySelectorAll('.spike-chart__x > span')].map(middle),
                ];
            }
            """);

        Assert.Equal(12, middles[0].Length);
        var drift = middles[1].Select((label, place) => label - middles[0][place]).Where(one => one is not null)
            .Select(one => one!.Value).ToList();
        RecordMeasurement(FormattableString.Invariant($"Columns ({browser}, {direction}, {width}): ") +
            FormattableString.Invariant($"{drift.Count} labels, {drift.Min():0.00} to {drift.Max():0.00}"));
        Assert.True(drift.Count >= 2, $"Only {drift.Count} place labels show.");
        Assert.All(drift, one => Assert.InRange(one, -1, 1));
        Assert.Equal(direction == "rtl", middles[0][0] > middles[0][^1]);
    }

    // Criterion 1, for a chart whose table isn't in its markup.
    [Theory, MemberData(nameof(Browsers))]
    public async Task LargeChart_ShouldDrawItsTable_WhenItIsOpened(string browser)
    {
        var page = await OpenAsync(browser, "server", "/chart?shape=large");
        var chart = page.Locator(".spike-chart");
        await Expect(chart.GetByRole(AriaRole.Table)).ToHaveCountAsync(0);

        await chart.Locator("summary").ClickAsync();

        await Expect(chart.GetByRole(AriaRole.Table)).ToBeVisibleAsync();
        await Expect(chart.Locator("tbody tr")).ToHaveCountAsync(1000);
    }

    // Criterion 4.
    [Theory, MemberData(nameof(BrowsersShapesAndScript))]
    public async Task PlaceLabels_ShouldNotOverlap_WhenThePlotIsNarrow(string browser, string shape, bool script)
    {
        var page = await OpenAsync(browser, "server", $"/chart?shape={shape}&width=306",
            options: script ? null : WithoutScript());

        var narrow = await ShownPlaceLabelsAsync(page);
        var plot = await BoxAsync(page, ".spike-chart__plot");

        RecordMeasurement(FormattableString.Invariant(
            $"Labels ({browser}, {shape}, script {script}, plot {plot.Width:0}): {narrow.Length} show"));
        Assert.InRange(plot.Width, 270, 290);
        Assert.True(narrow.Length >= 2, $"Only {narrow.Length} labels show.");
        for (var index = 1; index < narrow.Length; index++)
        {
            Assert.True(narrow[index][0] >= narrow[index - 1][1],
                $"Label {index} starts at {narrow[index][0]}, before the last ends at {narrow[index - 1][1]}.");
        }
    }

    // Criterion 4: thinning lets go once there is room.
    [Theory, MemberData(nameof(Browsers))]
    public async Task PlaceLabels_ShouldAllShow_WhenThePlotIsWide(string browser)
    {
        var page = await OpenAsync(browser, "server", "/chart?shape=columns&width=1200", options: Wide());

        Assert.Equal(12, (await ShownPlaceLabelsAsync(page)).Length);
    }

    // Criterion 5. Timing criteria are explicit measurements: run them with -explicit only.
    [Theory(Explicit = true), InlineData("line"), InlineData("large")]
    public async Task Pointer_ShouldSendNothingAndFollowWithinAFrame_WhenMovedOverThePlot(string shape)
    {
        var sent = new List<int>();
        var page = await OpenAsync("chromium", "server", $"/chart?shape={shape}", beforeNavigation: opened =>
        {
            opened.WebSocket += (_, socket) =>
                socket.FrameSent += (_, frame) => sent.Add(frame.Binary?.Length ?? frame.Text?.Length ?? 0);
            return Task.CompletedTask;
        });
        var session = await page.Context.NewCDPSessionAsync(page);
        await session.SendAsync("Emulation.setCPUThrottlingRate", new Dictionary<string, object> { ["rate"] = 4 });
        var plot = await BoxAsync(page, ".spike-chart__plot");
        await page.WaitForTimeoutAsync(500);
        sent.Clear();

        for (var step = 0; step <= 100; step++)
        {
            await page.Mouse.MoveAsync((float)(plot.X + 4 + (plot.Width - 8) * step / 100),
                (float)(plot.Y + plot.Height / 2));
            await page.WaitForTimeoutAsync(20);
        }

        var measured = await page.EvaluateAsync<double[][]>(
            "() => document.querySelector('.spike-chart__plot').spikeTimings");
        var toFrame = measured.Select(one => one[0]).Order().ToArray();
        var timings = measured.Select(one => one[1]).Order().ToArray();
        var work = measured.Select(one => one[2]).Order().ToArray();
        var lastLabel = await page.EvaluateAsync<string>("""
            share => {
                const labels = document.querySelector('.spike-chart__plot').dataset.x.split('|');
                return labels[Math.floor(share * labels.length)];
            }
            """, (plot.Width - 4) / plot.Width);
        // A keep-alive ping is 3 bytes; anything longer would be the chart talking to the server.
        var messages = sent.Where(length => length > 8).ToList();
        RecordMeasurement(FormattableString.Invariant(
            $"Pointer ({shape}): {timings.Length} updates, median {timings[timings.Length / 2]:0.0} ms, ") +
            FormattableString.Invariant($"p95 {timings[(int)(timings.Length * 0.95)]:0.0} ms, ") +
            FormattableString.Invariant($"max {timings[^1]:0.0} ms, ") +
            FormattableString.Invariant($"work median {work[work.Length / 2]:0.0} ms, max {work[^1]:0.0} ms, ") +
            FormattableString.Invariant($"wait for the frame median {toFrame[toFrame.Length / 2]:0.0} ms, ") +
            FormattableString.Invariant($"max {toFrame[^1]:0.0} ms, ") +
            $"{messages.Count} messages sent");
        await Expect(page.Locator(".spike-chart__tooltip-heading")).ToHaveTextAsync(lastLabel);
        Assert.Empty(messages);
        Assert.True(timings.Length >= 50, $"Only {timings.Length} updates ran.");
        Assert.InRange(timings[(int)(timings.Length * 0.95)], 0, Frame);
        Assert.Empty(ScriptErrors);
    }

    // Criterion 6.
    [Theory, MemberData(nameof(Browsers))]
    public async Task Keys_ShouldMoveBetweenNamedPoints_WhenThePlotHasFocus(string browser)
    {
        var sent = new List<int>();
        var page = await OpenAsync(browser, "server", "/chart?shape=line", beforeNavigation: opened =>
        {
            opened.WebSocket += (_, socket) =>
                socket.FrameSent += (_, frame) => sent.Add(frame.Binary?.Length ?? frame.Text?.Length ?? 0);
            return Task.CompletedTask;
        });
        var values = await page.EvaluateAsync<string[][]>("""
            () => [...document.querySelectorAll('[data-series]')].map(series => series.dataset.values.split('|'))
            """);
        var chart = page.Locator(".spike-chart");
        var focused = page.Locator(":focus");
        var tooltip = chart.Locator("[data-part=tooltip]");
        await Expect(chart.Locator(".spike-chart__focus[tabindex='0']")).ToHaveCountAsync(1);
        await page.WaitForTimeoutAsync(500);
        sent.Clear();

        await chart.GetByRole(AriaRole.Button, new() { Name = "Canceled" }).FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(focused).ToHaveRoleAsync(AriaRole.Img);
        await Expect(focused).ToHaveAccessibleNameAsync($"Mar 1, Completed: {values[0][0]}, 1 of 30");
        await Expect(tooltip).ToContainTextAsync("Mar 1");

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(focused).ToHaveAccessibleNameAsync($"Mar 2, Completed: {values[0][1]}, 2 of 30");
        await Expect(tooltip).ToContainTextAsync("Mar 2");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(focused).ToHaveAccessibleNameAsync($"Mar 2, Failed: {values[1][1]}, 2 of 30");
        await page.Keyboard.PressAsync("End");
        await Expect(focused).ToHaveAccessibleNameAsync($"Mar 30, Failed: {values[1][29]}, 30 of 30");
        await page.Keyboard.PressAsync("Home");
        await Expect(focused).ToHaveAccessibleNameAsync($"Mar 1, Failed: {values[1][0]}, 1 of 30");
        await Expect(chart.Locator(".spike-chart__focus[tabindex='0']")).ToHaveCountAsync(1);

        await page.Keyboard.PressAsync("Escape");
        await Expect(tooltip).ToBeHiddenAsync();
        await Expect(focused).ToHaveRoleAsync(AriaRole.Img);
        Assert.DoesNotContain(sent, length => length > 8);

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("activated")).ToHaveTextAsync($"Mar 1, Failed: {values[1][0]}, 1 of 30");

        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(focused).ToHaveRoleAsync(AriaRole.Button);
        Assert.Empty(ScriptErrors);
    }

    // Criterion 7: how much markup the large chart is.
    [Fact]
    public async Task LargeChart_ShouldStaySmall_WhenItHasFiveThousandPoints()
    {
        var page = await OpenAsync("chromium", "server", "/chart?shape=large");

        var size = await page.EvaluateAsync<int[]>("""
            () => {
                const chart = document.querySelector('.spike-chart');
                return [chart.querySelectorAll('*').length + 1, chart.outerHTML.length];
            }
            """);

        RecordMeasurement($"Large chart: {size[0]} elements, {size[1]} characters of markup");
        Assert.InRange(size[0], 1, 600);
        Assert.InRange(size[1], 1, 120_000);
    }

    // Criterion 7: how long the large chart takes to draw in WebAssembly.
    [Theory(Explicit = true), InlineData("large", "wasm", 4), InlineData("large", "wasm", 1)]
    [InlineData("large", "server", 4), InlineData("line", "wasm", 4), InlineData("columns", "wasm", 4)]
    [InlineData("large&points=500", "wasm", 4), InlineData("large&points=500", "wasm", 1)]
    public async Task Chart_ShouldDrawQuickly_WhenShownAndWhenAPointIsAdded(string shape, string mode, int throttle)
    {
        var page = await OpenAsync("chromium", mode, $"/chart?shape={shape}&start=hidden");
        var session = await page.Context.NewCDPSessionAsync(page);
        await session.SendAsync("Emulation.setCPUThrottlingRate",
            new Dictionary<string, object> { ["rate"] = throttle });

        var shown = await TimeClickAsync(page, "show");
        await Expect(page.Locator("[data-series]")).ToHaveCountAsync(shape.StartsWith("large") ? 5 : 3);
        var added = new List<double>();
        for (var run = 0; run < 5; run++)
        {
            added.Add(await TimeClickAsync(page, "add"));
        }

        RecordMeasurement(FormattableString.Invariant(
            $"Drawing ({shape}, {mode}, {throttle}x): first draw {shown:0} ms, a point added ") +
            string.Join(", ", added.Select(one => one.ToString("0", CultureInfo.InvariantCulture))) + " ms; .NET: " +
            await page.Locator(".spike-chart").GetAttributeAsync("data-build"));
        if (shape == "large" && mode == "wasm" && throttle == 4)
        {
            Assert.InRange(shown, 0, 100);
            Assert.InRange(added.Order().ElementAt(2), 0, 50);
        }
    }

    // Criterion 7: how much a new point sends over Interactive Server.
    [Theory, InlineData(1000), InlineData(500)]
    public async Task LargeChart_ShouldSendLittle_WhenAPointIsAddedOverTheServer(int points)
    {
        var received = new List<int>();
        var path = $"/chart?shape=large&points={points}";
        var page = await OpenAsync("chromium", "server", path, beforeNavigation: opened =>
        {
            opened.WebSocket += (_, socket) =>
                socket.FrameReceived += (_, frame) => received.Add(frame.Binary?.Length ?? frame.Text?.Length ?? 0);
            return Task.CompletedTask;
        });
        await page.WaitForTimeoutAsync(500);
        received.Clear();

        await page.GetByTestId("add").ClickAsync();
        await Expect(page.Locator(".spike-chart__plot")).ToHaveAttributeAsync("data-count", $"{points + 1}");
        await page.WaitForTimeoutAsync(200);

        RecordMeasurement($"Large chart (server, {points} points a series): a point added sent {received.Sum()} bytes");
        Assert.InRange(received.Sum(), 1, 40_000);
    }

    // Criterion 8: forced colors.
    [Theory, MemberData(nameof(Browsers))]
    public async Task Series_ShouldDifferWithoutColor_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "server", "/chart?shape=line",
            options: new() { ForcedColors = ForcedColors.Active });

        var lines = await page.EvaluateAsync<string[][]>("""
            () => {
                const probe = document.createElement('span');
                probe.style.color = 'Canvas';
                document.body.append(probe);
                const canvas = getComputedStyle(probe).color;
                probe.remove();
                return [...document.querySelectorAll('.spike-chart__line')].map(line => {
                    const style = getComputedStyle(line);
                    return [style.stroke, style.strokeDasharray, canvas];
                });
            }
            """);
        await page.GotoAsync(new Uri(Fixture.BaseAddress, "/chart?shape=columns").ToString());
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        var bars = await page.EvaluateAsync<string[]>("""
            () => [...document.querySelectorAll('g[data-series]')].map(series => {
                const style = getComputedStyle(series.querySelector('rect'));
                return `${style.fill} ${style.strokeDasharray}`;
            })
            """);

        RecordMeasurement($"Forced colors ({browser}): lines {JsonSerializer.Serialize(lines)}, bars " +
            JsonSerializer.Serialize(bars));
        Assert.Equal(3, lines.Length);
        Assert.All(lines, line => Assert.NotEqual(line[2], line[0]));
        Assert.Single(lines.Select(line => line[0]).Distinct());
        Assert.NotEqual(lines[0][1], lines[1][1]);
        Assert.NotEqual(lines[1][1], lines[2][1]);
        Assert.NotEqual(bars[0], bars[1]);
        Assert.NotEqual(bars[1], bars[2]);
    }

    // Criterion 8: contrast of every series color against the surface.
    [Theory, MemberData(nameof(BrowsersAndSchemes))]
    public async Task SeriesColors_ShouldHaveThreeToOneAgainstTheSurface_WhenInEitherTheme(string browser,
        string scheme)
    {
        var page = await OpenAsync(browser, "server", "/chart?shape=line",
            options: new() { ColorScheme = scheme == "dark" ? ColorScheme.Dark : ColorScheme.Light });
        await page.EvaluateAsync("scheme => { document.documentElement.dataset.tbTheme = scheme; }", scheme);

        var colors = await page.EvaluateAsync<double[][]>("""
            () => {
                const chart = document.querySelector('.spike-chart');
                const context = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
                const read = background => {
                    const swatch = document.createElement('span');
                    swatch.style.background = background;
                    chart.append(swatch);
                    context.fillStyle = getComputedStyle(swatch).backgroundColor;
                    swatch.remove();
                    context.fillRect(0, 0, 1, 1);
                    return [...context.getImageData(0, 0, 1, 1).data].slice(0, 3);
                };
                const slots = [];
                for (let slot = 1; slot <= 8; slot++) {
                    const holder = document.createElement('span');
                    holder.className = `spike-chart__series--${slot}`;
                    chart.append(holder);
                    const swatch = document.createElement('span');
                    swatch.style.background = 'var(--_series)';
                    holder.append(swatch);
                    context.fillStyle = getComputedStyle(swatch).backgroundColor;
                    holder.remove();
                    context.fillRect(0, 0, 1, 1);
                    slots.push([...context.getImageData(0, 0, 1, 1).data].slice(0, 3));
                }
                return [read('var(--tb-color-surface)'), ...slots];
            }
            """);

        var surface = Luminance(colors[0]);
        var ratios = colors.Skip(1).Select(color => Contrast(Luminance(color), surface)).ToList();
        RecordMeasurement(FormattableString.Invariant($"Contrast ({browser}, {scheme}): ") +
            string.Join(", ", ratios.Select(ratio => ratio.ToString("0.00", CultureInfo.InvariantCulture))));
        Assert.Equal(scheme == "dark", surface < 0.2);
        Assert.All(ratios, ratio => Assert.InRange(ratio, 3, 21));
    }

    private static BrowserNewContextOptions WithoutScript() => new() { JavaScriptEnabled = false };

    private static BrowserNewContextOptions Wide() => new() { ViewportSize = new() { Width = 1800, Height = 900 } };

    private static async Task<Box> BoxAsync(IPage page, string selector)
    {
        var box = await page.Locator(selector).First.EvaluateAsync<double[]>("""
            element => {
                const box = element.getBoundingClientRect();
                return [box.left, box.top, box.width, box.height];
            }
            """);
        return new Box(box[0], box[1], box[2], box[3]);
    }

    private static Task<int> CountShownAsync(IPage page, string selector) => page.EvaluateAsync<int>("""
        selector => [...document.querySelectorAll(selector)].filter(element => {
            const box = element.getBoundingClientRect();
            return box.width > 0 || box.height > 0;
        }).length
        """, selector);

    // The left and right edges of each place label that has any width, from left to right.
    private static Task<double[][]> ShownPlaceLabelsAsync(IPage page) => page.EvaluateAsync<double[][]>("""
        () => [...document.querySelectorAll('.spike-chart__x > span')]
            .map(label => label.getBoundingClientRect())
            .filter(box => box.width > 0)
            .map(box => [box.left, box.right])
            .sort((one, other) => one[0] - other[0])
        """);

    private static async Task<Ink> MeasureAsync(IPage page, Strip[] columns, Strip[] rows, Box[] boxes)
    {
        var png = Convert.ToBase64String(await page.ScreenshotAsync());
        var json = await page.EvaluateAsync<string>($"async spec => JSON.stringify(await ({MeasureInk})(spec))", new
        {
            png,
            columns = columns.Select(strip => new { x = strip.X, y = strip.Y, height = strip.Length }),
            rows = rows.Select(strip => new { x = strip.X, y = strip.Y, width = strip.Length }),
            boxes = boxes.Select(box => new { x = box.X, y = box.Y, width = box.Width, height = box.Height }),
        });
        return JsonSerializer.Deserialize<Ink>(json, JsonSerializerOptions.Web)!;
    }

    // The lengths of the whole dashes and gaps in a row of ink, leaving out the run at each end.
    private static (List<int> Dashes, List<int> Gaps) Runs(double[] row)
    {
        var runs = new List<(bool Inked, int Length)>();
        foreach (var inked in row.Select(ink => ink > 0.4))
        {
            if (runs.Count > 0 && runs[^1].Inked == inked)
            {
                runs[^1] = (inked, runs[^1].Length + 1);
            }
            else
            {
                runs.Add((inked, 1));
            }
        }

        var whole = runs.Skip(1).SkipLast(1).ToList();
        return ([.. whole.Where(run => run.Inked).Select(run => run.Length)],
            [.. whole.Where(run => !run.Inked).Select(run => run.Length)]);
    }

    // From the click to the frame after the one that painted the change.
    private static Task<double> TimeClickAsync(IPage page, string testId) => page.EvaluateAsync<double>("""
        async testId => {
            const frame = document.querySelector('[data-testid=frame]');
            const changed = new Promise(resolve => {
                const observer = new MutationObserver(() => {
                    observer.disconnect();
                    requestAnimationFrame(() => requestAnimationFrame(() => resolve(performance.now())));
                });
                observer.observe(frame, { subtree: true, childList: true, attributes: true });
            });
            const start = performance.now();
            document.querySelector(`[data-testid=${testId}]`).click();
            return await changed - start;
        }
        """, testId);

    private static double Luminance(double[] color)
    {
        static double Channel(double value)
        {
            var share = value / 255;
            return share <= 0.04045 ? share / 12.92 : Math.Pow((share + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color[0]) + 0.7152 * Channel(color[1]) + 0.0722 * Channel(color[2]);
    }

    private static double Contrast(double one, double other) =>
        (Math.Max(one, other) + 0.05) / (Math.Min(one, other) + 0.05);

    public sealed record Box(double X, double Y, double Width, double Height);

    public sealed record Strip(int X, int Y, int Length);

    public sealed record Ink(double[] Columns, double[][] Rows, double[] Boxes);

    // Playwright fills this from a script's answer, which needs a constructor without parameters.
    public sealed class Alignment
    {
        public double[] Widths { get; set; } = [];

        public double[] ValueLabels { get; set; } = [];

        public double[] GridLines { get; set; } = [];

        public double?[] PlaceLabels { get; set; } = [];

        public double[] MarkerXs { get; set; } = [];

        public double[] MarkerYs { get; set; } = [];

        public double[] CornerXs { get; set; } = [];

        public double[] CornerYs { get; set; } = [];
    }
}
