using System.Text.Json;

namespace Trailblazor.Spikes.Tests.Grid;

// Spike C. See spikes/grid-virtualization/README.md.
public class GridTests(SpikeFixture fixture) : SpikeTest(fixture)
{
    private const double RowHeight = 36;

    // rAF timestamps are rounded to 0.1 ms, so an on-time 60 Hz frame reads as 16.7 and the median can't go lower.
    private const double SixtyHertzFrame = 16.75;

    // A slow continuous stretch, then a fling to the last row.
    private const string FullScroll = "[{ pxPerFrame: 120, frames: 600 }, { pxPerFrame: 4000, frames: null }]";

    public static TheoryData<string> ModesOnly => new(BrowsersAndModes.Modes);

    public static TheoryData<string, string> ModesAndDensities => new(
        from mode in BrowsersAndModes.Modes
        from density in new[] { "comfortable", "compact" }
        select (mode, density));

    // Criterion 1. Timing criteria are explicit measurements: run them with -- --explicit only.
    [Fact(Explicit = true)]
    public async Task Scroll_ShouldKeepSixtyFramesPerSecond_WhenScrollingAllRowsInWebAssembly()
    {
        var page = await OpenThrottledGridAsync("wasm", "/grid");

        var result = await MeasureScrollAsync(page, FullScroll);

        RecordMeasurement($"Scroll (wasm): {JsonSerializer.Serialize(result)}");
        Assert.InRange(result.Median, 0, SixtyHertzFrame);
        Assert.InRange(result.P95, 0, 20);
        Assert.InRange(result.LongestTask, 0, 50);
        Assert.Empty(ScriptErrors);
    }

    // Criterion 2. A run counts only if its measured round trip is 50 to 56 ms.
    [Fact(Explicit = true)]
    public async Task Scroll_ShouldStaySmoothAndFillWithin150Milliseconds_WhenServerRoundTripsTake50Milliseconds()
    {
        await using var proxy = new LatencyProxy(Fixture.BaseAddress, TimeSpan.FromMilliseconds(25));
        var page = await OpenThrottledGridAsync("server", "/grid", proxy.Address);
        var roundTrip = await page.EvaluateAsync<double>("""
            async () => {
                const start = performance.now();
                await window.gridSpike.stats();
                return performance.now() - start;
            }
            """);

        var scroll = await MeasureScrollAsync(page, "[{ pxPerFrame: 120, frames: 600 }]");
        var fills = new List<double>();
        foreach (var row in new[] { 1_000, 50_000, 99_000 })
        {
            fills.Add(await page.EvaluateAsync<double>("top => window.gridSpike.measureFill(top)", row * RowHeight));
        }

        RecordMeasurement($"Scroll (server, round trip {roundTrip:F0} ms): {JsonSerializer.Serialize(scroll)}, " +
            $"fills {string.Join(", ", fills.Select(f => $"{f:F0} ms"))}");
        Assert.InRange(roundTrip, 50, 56);
        Assert.InRange(scroll.Median, 0, SixtyHertzFrame);
        Assert.InRange(scroll.P95, 0, 20);
        Assert.All(fills, fill => Assert.InRange(fill, 0, 150));
        Assert.Empty(ScriptErrors);
    }

    // Criterion 3.
    [Theory, MemberData(nameof(ModesOnly))]
    public async Task Scroll_ShouldRenderOnlyTheRowsThatBecameVisible_WhenScrollingPastTheOverscan(string mode)
    {
        await AssertOnlyNewlyVisibleRowsRenderAsync(await OpenGridAsync("chromium", mode, "/grid"));
    }

    // Server mode acknowledges each render batch a round trip later, which is when stale render state would show.
    [Fact]
    public async Task Scroll_ShouldRenderOnlyTheRowsThatBecameVisible_WhenServerRoundTripsTake50Milliseconds()
    {
        await using var proxy = new LatencyProxy(Fixture.BaseAddress, TimeSpan.FromMilliseconds(25));
        await AssertOnlyNewlyVisibleRowsRenderAsync(await OpenGridAsync("chromium", "server", "/grid", proxy.Address));
    }

    private async Task AssertOnlyNewlyVisibleRowsRenderAsync(IPage page)
    {
        var rowsBefore = await GetSettledRenderedRowsAsync(page);
        var statsBefore = await GetStatsAsync(page);

        // 30 rows, so the rendered window moves past the 10-row overscan.
        await page.EvaluateAsync("distance => document.querySelector('[data-testid=grid]').scrollTop += distance",
            30 * RowHeight);
        await page.WaitForFunctionAsync("first => window.gridSpike.renderedRows()[0] !== first", rowsBefore[0]);
        var rowsAfter = await GetSettledRenderedRowsAsync(page);
        var statsAfter = await GetStatsAsync(page);

        var newRows = rowsAfter.Except(rowsBefore).Count();
        Assert.True(newRows > 0);
        Assert.Equal(newRows, statsAfter.RowRenders - statsBefore.RowRenders);
        Assert.Equal(newRows, statsAfter.RowInstances - statsBefore.RowInstances);
        Assert.Empty(ScriptErrors);
    }

    [Theory, MemberData(nameof(ModesOnly))]
    public async Task TouchRow_ShouldRenderNoRow_WhenTheChangedRowIsOffScreen(string mode)
    {
        var page = await OpenGridAsync("chromium", mode, "/grid");
        await GetSettledRenderedRowsAsync(page);
        var before = await GetStatsAsync(page);

        await page.EvaluateAsync("() => window.gridSpike.touchRow(50000)");
        await page.WaitForTimeoutAsync(300);
        var afterOffScreen = await GetStatsAsync(page);
        await page.EvaluateAsync("() => window.gridSpike.touchRow(0)");
        await page.WaitForTimeoutAsync(300);
        var afterOnScreen = await GetStatsAsync(page);

        Assert.Equal(before.RowRenders, afterOffScreen.RowRenders);
        Assert.Equal(afterOffScreen.RowRenders + 1, afterOnScreen.RowRenders);
    }

    [Theory, MemberData(nameof(ModesAndDensities))]
    public async Task Grid_ShouldStayUnderThreeThousandNodes_WhenShowingTheFirstOrLastRows(string mode, string density)
    {
        var page = await OpenGridAsync("chromium", mode, $"/grid?density={density}");
        await GetSettledRenderedRowsAsync(page);
        var atTop = await page.EvaluateAsync<int>("() => window.gridSpike.domNodes()");

        await page.EvaluateAsync("() => window.gridSpike.measureFill(1e9)");
        var atEnd = await page.EvaluateAsync<int>("() => window.gridSpike.domNodes()");

        RecordMeasurement($"DOM nodes ({mode}, {density}): {atTop} at the top, {atEnd} at the end");
        Assert.InRange(atTop, 1, 2999);
        Assert.InRange(atEnd, 1, 2999);
    }

    // Criterion 4.
    [Fact(Explicit = true)]
    public async Task ManagedHeap_ShouldGrowLessThanTenPercent_WhenScrollingAllRowsInWebAssembly()
    {
        var page = await OpenGridAsync("chromium", "wasm", "/grid");
        await GetSettledRenderedRowsAsync(page);
        var before = await page.EvaluateAsync<long>("() => window.gridSpike.managedMemory()");

        await MeasureScrollAsync(page, FullScroll);
        var after = await page.EvaluateAsync<long>("() => window.gridSpike.managedMemory()");

        RecordMeasurement($"Managed heap (wasm): {before:N0} bytes before, {after:N0} bytes after a full scroll");
        Assert.InRange((double)after / before, 0, 1.10);
    }

    // Criterion 5.
    [Theory(Explicit = true), MemberData(nameof(ModesOnly))]
    public async Task Jump_ShouldLeaveNoBlankAreaOver100Milliseconds_WhenJumpingToTheLastRow(string mode)
    {
        var page = await OpenThrottledGridAsync(mode, "/grid");
        await GetSettledRenderedRowsAsync(page);

        var fill = await page.EvaluateAsync<double>("() => window.gridSpike.measureFill(1e9)");

        RecordMeasurement($"Jump to the last row ({mode}): filled after {fill:F0} ms");
        Assert.InRange(fill, 0, 100);
        await Expect(page.Locator("[data-row='99999']")).ToHaveCountAsync(1);
    }

    // Criterion 6.
    [Theory, MemberData(nameof(ModesOnly))]
    public async Task Provider_ShouldCancelSupersededRequestsAndKeepOneActive_WhenJumpingRapidly(string mode)
    {
        var page = await OpenGridAsync("chromium", mode, "/grid?source=provider");
        await page.EvaluateAsync("""
            async () => {
                const grid = document.querySelector('[data-testid=grid]');
                for (let i = 1; i <= 10; i++) {
                    grid.scrollTop = i * 300000;
                    await new Promise(resolve => requestAnimationFrame(resolve));
                }
            }
            """);
        await page.WaitForTimeoutAsync(1000);

        var stats = await GetStatsAsync(page);

        RecordMeasurement($"Provider ({mode}): {JsonSerializer.Serialize(stats)}");
        Assert.True(stats.RequestsCancelled > 0, "Rapid jumps should supersede some requests.");
        Assert.Equal(1, stats.MaxActiveRequests);
        Assert.Equal(stats.RequestsStarted, stats.RequestsCompleted + stats.RequestsCancelled);
        Assert.Empty(ScriptErrors);
    }

    [Theory, MemberData(nameof(ModesOnly))]
    public async Task Provider_ShouldNotLoadAgain_WhenTheGridRerendersWithUnchangedParameters(string mode)
    {
        var page = await OpenGridAsync("chromium", mode, "/grid?source=provider");
        await page.WaitForFunctionAsync("() => window.gridSpike.renderedRows().length > 0");
        await page.WaitForTimeoutAsync(500);
        var before = await GetStatsAsync(page);

        for (var i = 0; i < 3; i++)
        {
            await page.EvaluateAsync("() => window.gridSpike.rerender()");
        }

        await page.WaitForTimeoutAsync(500);
        Assert.Equal(before.RequestsStarted, (await GetStatsAsync(page)).RequestsStarted);
    }

    // Criterion 7.
    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Rows_ShouldCarryTheirPositionForAssistiveTechnology_WhenScrolled(string browser, string mode)
    {
        var page = await OpenGridAsync(browser, mode, "/grid");
        await Expect(page.GetByTestId("grid")).ToHaveAttributeAsync("aria-rowcount", "100001");

        await GetSettledRenderedRowsAsync(page);
        await AssertRowIndexesAsync(page);
        await page.EvaluateAsync("top => window.gridSpike.measureFill(top)", 50_000 * RowHeight);
        await AssertRowIndexesAsync(page);
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task ArrowKeys_ShouldKeepFocusOnTheGrid_WhenTheActiveRowScrolledOutAndItsElementWasRemoved(
        string browser, string mode)
    {
        var page = await OpenGridAsync(browser, mode, "/grid");
        var grid = page.GetByTestId("grid");
        await GetSettledRenderedRowsAsync(page);
        await grid.FocusAsync();
        for (var i = 0; i < 3; i++)
        {
            await page.Keyboard.PressAsync("ArrowDown");
        }

        await Expect(grid).ToHaveAttributeAsync("aria-activedescendant", "row-2");
        await page.EvaluateAsync("top => window.gridSpike.measureFill(top)", 50_000 * RowHeight);
        await Expect(page.Locator("#row-2")).ToHaveCountAsync(0);

        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(grid).ToHaveAttributeAsync("aria-activedescendant", "row-3");
        await Expect(grid).ToBeFocusedAsync();
        await Expect(page.Locator("#row-3")).ToBeVisibleAsync();
    }

    // Criterion 8.
    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task HeaderAndPinnedColumn_ShouldStayAlignedWithTheBody_WhenScrolling(string browser, string mode)
    {
        var page = await OpenGridAsync(browser, mode, "/grid");
        await GetSettledRenderedRowsAsync(page);

        var drift = await page.EvaluateAsync<double>("""
            () => window.gridSpike.measureAlignment([
                { pxPerFrame: 200, frames: 100 },
                { pxPerFrame: 20, frames: 20, horizontal: true },
                { pxPerFrame: -150, frames: 60 },
            ])
            """);

        Assert.InRange(drift, 0, 1);
        Assert.Empty(ScriptErrors);
    }

    private async Task<IPage> OpenGridAsync(string browser, string mode, string path, Uri? baseAddress = null)
    {
        var page = await OpenAsync(browser, mode, path, baseAddress);
        await Expect(page.GetByTestId("attached")).ToHaveTextAsync("True");
        return page;
    }

    private async Task<IPage> OpenThrottledGridAsync(string mode, string path, Uri? baseAddress = null)
    {
        var page = await OpenGridAsync("chromium", mode, path, baseAddress);
        await GetSettledRenderedRowsAsync(page);
        var session = await page.Context.NewCDPSessionAsync(page);
        await session.SendAsync("Emulation.setCPUThrottlingRate", new Dictionary<string, object> { ["rate"] = 4 });
        return page;
    }

    private static async Task<ScrollMeasurement> MeasureScrollAsync(IPage page, string segments)
    {
        var json = await page.EvaluateAsync<string>(
            $"async () => JSON.stringify(await window.gridSpike.measureScroll({segments}))");
        return JsonSerializer.Deserialize<ScrollMeasurement>(json, JsonSerializerOptions.Web)!;
    }

    // Waits until the rendered row set holds still for two checks in a row.
    private static async Task<int[]> GetSettledRenderedRowsAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => window.gridSpike.renderedRows().length > 0");
        var previous = Array.Empty<int>();
        var current = await page.EvaluateAsync<int[]>("() => window.gridSpike.renderedRows()");
        while (!current.SequenceEqual(previous))
        {
            await page.WaitForTimeoutAsync(200);
            previous = current;
            current = await page.EvaluateAsync<int[]>("() => window.gridSpike.renderedRows()");
        }

        return current;
    }

    private static async Task<GridStatsSnapshot> GetStatsAsync(IPage page)
    {
        var json = await page.EvaluateAsync<string>("async () => JSON.stringify(await window.gridSpike.stats())");
        return JsonSerializer.Deserialize<GridStatsSnapshot>(json, JsonSerializerOptions.Web)!;
    }

    private static async Task AssertRowIndexesAsync(IPage page)
    {
        // Expected index from where each row actually sits: rows start below the sticky header, one row height apart.
        var mismatches = await page.EvaluateAsync<int>("""
            () => {
                const grid = document.querySelector('[data-testid=grid]');
                const headerBottom = grid.querySelector('.grid-header').getBoundingClientRect().bottom;
                const rowHeight = parseFloat(grid.style.getPropertyValue('--row-height'));
                return [...grid.querySelectorAll('.grid-row[data-row]')].filter(row => {
                    const offset = row.getBoundingClientRect().top - headerBottom + grid.scrollTop;
                    return Number(row.getAttribute('aria-rowindex')) !== Math.round(offset / rowHeight) + 2;
                }).length;
            }
            """);
        Assert.Equal(0, mismatches);
    }

    public sealed record ScrollMeasurement(int Frames, double Median, double P95, double LongestTask,
        IReadOnlyList<SegmentMeasurement> Segments);

    public sealed record SegmentMeasurement(double PxPerFrame, int Frames, double Median, double P95,
        double LongestTask);

    public sealed record GridStatsSnapshot(int RowInstances, int RowRenders, int RequestsStarted, int RequestsCompleted,
        int RequestsCancelled, int MaxActiveRequests);
}
