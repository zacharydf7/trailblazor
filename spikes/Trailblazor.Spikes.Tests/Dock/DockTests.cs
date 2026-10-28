using System.Text.Json;
using Trailblazor.Spikes.Client.Dock;

namespace Trailblazor.Spikes.Tests.Dock;

// Spike B. See spikes/dock-keep-alive/README.md.
public class DockTests(SpikeFixture fixture) : SpikeTest(fixture)
{
    public static TheoryData<string> ModesOnly => new(BrowsersAndModes.Modes);

    // Whether each bundled engine has Element.moveBefore, as observed on 2026-10-28.
    public static TheoryData<string, bool> MoveBeforeSupport => new()
    {
        { "chromium", true },
        { "firefox", true },
        { "webkit", false },
    };

    // Criterion 1.
    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Panels_ShouldKeepTheirStateAndNeverRerender_WhenMovedAHundredTimes(string browser, string mode)
    {
        var page = await OpenDockAsync(browser, mode, "/dock");
        await ActivateAsync(page, "p1");
        await page.GetByTestId("input-p1").FillAsync("typed");
        await ActivateAsync(page, "p2");
        await page.GetByTestId("list-p2").EvaluateAsync("list => list.scrollTop = 300");
        await ActivateAsync(page, "p0");
        for (var i = 0; i < 3; i++)
        {
            await page.GetByTestId("increment-p0").ClickAsync();
        }

        await Expect(page.GetByTestId("count-p0")).ToHaveTextAsync("3");
        var framesBefore = await GetSettledFrameLoadsAsync(page);
        var before = await GetStatsAsync(page);

        for (var i = 0; i < 100; i++)
        {
            await MoveAsync(page, $"p{i % 19}", $"g{i * 7 % 6}");
        }

        foreach (var panel in new[] { "p0", "p1", "p2" })
        {
            await MoveAsync(page, panel, "g0");
        }

        await ActivateAsync(page, "p1");
        await Expect(page.GetByTestId("input-p1")).ToHaveValueAsync("typed");
        await ActivateAsync(page, "p2");
        Assert.Equal(300, await page.GetByTestId("list-p2").EvaluateAsync<double>("list => list.scrollTop"), 1.0);
        await ActivateAsync(page, "p0");
        await Expect(page.GetByTestId("count-p0")).ToHaveTextAsync("3");
        Assert.Equal(framesBefore, await GetSettledFrameLoadsAsync(page));
        var after = await GetStatsAsync(page);
        foreach (var (panel, counts) in after.Where(entry => entry.Key != DockHosts.NotKeptAlive))
        {
            Assert.Equal(1, counts.Initializations);
            Assert.Equal(0, counts.Disposals);
            Assert.Equal(before[panel].Renders, counts.Renders);
        }

        Assert.Empty(ScriptErrors);
    }

    // Criterion 2.
    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Hosts_ShouldStayOnTheirSlotsInEveryFrame_WhenASplitterIsDraggedAndTheWindowResizes(string browser,
        string mode)
    {
        var page = await OpenDockAsync(browser, mode, "/dock");
        var splitter = (await page.GetByTestId("splitter").First.BoundingBoxAsync())!;
        var x = splitter.X + splitter.Width / 2;
        var y = splitter.Y + splitter.Height / 2;
        await page.EvaluateAsync("() => window.dockSpike.startSampling()");

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + 150, y, new() { Steps = 30 });
        await page.Mouse.UpAsync();
        await page.SetViewportSizeAsync(1100, 700);
        await page.SetViewportSizeAsync(1280, 720);
        await page.WaitForTimeoutAsync(200);

        Assert.InRange(await page.EvaluateAsync<double>("() => window.dockSpike.stopSampling()"), 0, 1);
        Assert.Empty(ScriptErrors);
    }

    // Criterion 3, on Chromium with the CPU slowed 4×. The cold first move is recorded, not asserted; see the README.
    [Theory, MemberData(nameof(ModesOnly))]
    public async Task Redock_ShouldSettleQuicklyWithoutLongTasksAndCallDotNetOnce_WhenThePathIsWarm(string mode)
    {
        var page = await OpenDockAsync("chromium", mode, "/dock");
        var session = await page.Context.NewCDPSessionAsync(page);
        await session.SendAsync("Emulation.setCPUThrottlingRate", new Dictionary<string, object> { ["rate"] = 4 });

        var moves = new[] { ("p1", "g2"), ("p5", "g0"), ("p9", "g3"), ("p16", "g1"), ("p2", "g4"), ("p13", "g2") };
        var results = new List<MoveMeasurement>();
        foreach (var (panel, group) in moves)
        {
            var callsBefore = await GetLayoutCallCountAsync(page);
            var json = await page.EvaluateAsync<string>(
                "async ([panel, group]) => JSON.stringify(await window.dockSpike.measureMove(panel, group))",
                new[] { panel, group });
            var calls = await GetLayoutCallCountAsync(page) - callsBefore;
            var measurement = JsonSerializer.Deserialize<MoveMeasurement>(json, JsonSerializerOptions.Web)!;
            results.Add(measurement with { Calls = calls });
        }

        RecordMeasurement($"Re-dock ({mode}), cold first: {JsonSerializer.Serialize(results)}");
        Assert.All(results.Skip(1), result =>
        {
            Assert.InRange(result.Frames, 1, 2);
            Assert.InRange(result.LongestTask, 0, 50);
            Assert.Equal(1, result.Calls);
        });
        Assert.Empty(ScriptErrors);
    }

    // Criterion 4: hidden panels are out of reach.
    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task HiddenPanel_ShouldNotTakeFocus_WhenItsTabIsInactive(string browser, string mode)
    {
        var page = await OpenDockAsync(browser, mode, "/dock");

        var focused = await page.GetByTestId("input-p5").EvaluateAsync<bool>(
            "input => { input.focus(); return document.activeElement === input; }");

        Assert.False(focused);
    }

    [Theory, MemberData(nameof(ModesOnly))]
    public async Task HiddenPanel_ShouldBeAbsentFromChromiumsAccessibilityTree_WhenItsTabIsInactive(string mode)
    {
        var page = await OpenDockAsync("chromium", mode, "/dock");
        var session = await page.Context.NewCDPSessionAsync(page);

        var tree = (await session.SendAsync("Accessibility.getFullAXTree"))!.Value;

        var exposedNames = tree.GetProperty("nodes").EnumerateArray()
            .Where(node => !node.GetProperty("ignored").GetBoolean() && node.TryGetProperty("name", out _))
            .Select(node => node.GetProperty("name").GetProperty("value").ToString())
            .ToList();
        Assert.Contains("Add one", exposedNames);
        Assert.DoesNotContain("Input p5", exposedNames);
    }

    // Criterion 4 on Chromium; the control turns content-visibility off to prove the measurement can see the cost.
    [Theory, MemberData(nameof(ModesOnly))]
    public async Task HiddenHeavyPanels_ShouldAddAtMostTenPercentLayoutTime_WhenTheDockRelayouts(string mode)
    {
        var oneHeavy = await MedianLayoutSecondsAsync(mode, "/dock?layout=single&heavy=visible");
        var allHeavy = await MedianLayoutSecondsAsync(mode, "/dock?layout=single&heavy=all");
        var control = await MedianLayoutSecondsAsync(mode, "/dock?layout=single&heavy=all&contentVisibility=off");

        RecordMeasurement(
            $"Layout ({mode}): one heavy {oneHeavy:F4}s, all heavy {allHeavy:F4}s, control {control:F4}s (medians)");
        Assert.InRange(allHeavy / oneHeavy, 0, 1.10);
        Assert.True(control > oneHeavy * 2, "The control should be clearly slower; the measurement is too noisy.");
    }

    // Criterion 4: KeepAlive="false".
    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task NotKeptAlivePanel_ShouldBeDisposed_WhenItIsHidden(string browser, string mode)
    {
        var page = await OpenDockAsync(browser, mode, "/dock");
        await page.GetByTestId("toggle-autohide").ClickAsync();
        await ActivateAsync(page, DockHosts.NotKeptAlive);
        await Expect(page.GetByTestId($"frame-{DockHosts.NotKeptAlive}")).ToHaveCountAsync(1);

        await page.GetByTestId("toggle-autohide").ClickAsync();

        await Expect(page.GetByTestId($"frame-{DockHosts.NotKeptAlive}")).ToHaveCountAsync(0);
        var stats = await GetStatsAsync(page);
        Assert.Equal(1, stats[DockHosts.NotKeptAlive].Initializations);
        Assert.Equal(1, stats[DockHosts.NotKeptAlive].Disposals);
    }

    // Criterion 5 fails: this records the observed order, so it starts failing the day a fix changes it.
    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Tab_ShouldVisitEveryTabStripBeforeAnyPanelContent_WhenHostsLiveOutsideTheLayout(string browser,
        string mode)
    {
        var page = await OpenDockAsync(browser, mode, "/dock");
        await page.GetByTestId("tab-p0").FocusAsync();

        var order = new List<string>();
        for (var i = 0; i < 30; i++)
        {
            order.Add(await page.EvaluateAsync<string>("() => document.activeElement?.dataset.testid ?? ''"));
            await page.Keyboard.PressAsync("Tab");
        }

        RecordMeasurement($"Tab order ({browser}, {mode}): {string.Join(", ", order)}");
        var firstContent = order.IndexOf("increment-p0");
        Assert.True(firstContent > order.IndexOf("tab-p17"), $"Tab order was: {string.Join(", ", order)}");
    }

    // Criterion 5 fallback precondition, outside Blazor: can a node move in the DOM keeping iframe and input state?
    [Theory, MemberData(nameof(MoveBeforeSupport))]
    public async Task MoveBefore_ShouldKeepIframeAndInputState_WhenTheEngineSupportsIt(string browser, bool supported)
    {
        var context = await Fixture.NewContextAsync(browser);
        var page = await context.NewPageAsync();
        await page.SetContentAsync("""
            <div id="from"><div id="host"><input id="input" aria-label="Input">
            <iframe title="Frame" srcdoc="<p>Frame</p>" onload="window.loads = (window.loads || 0) + 1"></iframe>
            </div></div><div id="to"></div>
            """);
        await page.WaitForFunctionAsync("() => window.loads === 1");
        await page.Locator("#input").FillAsync("typed");

        Assert.Equal(supported, await page.EvaluateAsync<bool>("() => 'moveBefore' in Element.prototype"));
        if (supported)
        {
            await page.EvaluateAsync(
                "() => document.getElementById('to').moveBefore(document.getElementById('host'), null)");
            await page.WaitForTimeoutAsync(500);

            Assert.Equal(1, await page.EvaluateAsync<int>("() => window.loads"));
            Assert.Equal("typed", await page.Locator("#to #input").InputValueAsync());
        }

        await context.CloseAsync();
    }

    // Criterion 6, popovers with native anchoring; the JS fallback is nondeterministic here, so it's only reported.
    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task PanelPopover_ShouldStayOnItsTrigger_WhenThePanelMoves(string browser, string mode)
    {
        var page = await OpenDockAsync(browser, mode, "/dock?positioning=native");
        await page.GetByTestId("popover-trigger").ClickAsync();
        await Expect(page.GetByTestId("panel-popover")).ToBeVisibleAsync();

        await MoveAsync(page, "p0", "g3");
        await page.WaitForTimeoutAsync(200);

        var trigger = (await page.GetByTestId("popover-trigger").BoundingBoxAsync())!;
        var popover = (await page.GetByTestId("panel-popover").BoundingBoxAsync())!;
        Assert.Equal(trigger.X, popover.X, 1.0);
        Assert.Equal(trigger.Y + trigger.Height + 4, popover.Y, 1.0);
        Assert.Empty(ScriptErrors);
    }

    private async Task<IPage> OpenDockAsync(string browser, string mode, string path)
    {
        var page = await OpenAsync(browser, mode, path);
        await Expect(page.GetByTestId("attached")).ToHaveTextAsync("True");
        return page;
    }

    private async Task<double> MedianLayoutSecondsAsync(string mode, string path)
    {
        var page = await OpenDockAsync("chromium", mode, path);
        var session = await page.Context.NewCDPSessionAsync(page);
        await session.SendAsync("Performance.enable");

        var samples = new List<double>();
        for (var sample = 0; sample < 5; sample++)
        {
            var before = await GetLayoutSecondsAsync(session);
            // An inherited font-size change invalidates layout in every host, unless the host lets the browser skip it.
            await page.EvaluateAsync("""
                () => {
                    const dock = document.querySelector('[data-testid=dock]');
                    for (let i = 0; i < 50; i++) {
                        dock.style.fontSize = `${14 + (i % 2)}px`;
                        dock.offsetHeight;
                    }
                }
                """);
            samples.Add(await GetLayoutSecondsAsync(session) - before);
        }

        return samples.Order().ElementAt(samples.Count / 2);
    }

    private static async Task<double> GetLayoutSecondsAsync(ICDPSession session)
    {
        var metrics = (await session.SendAsync("Performance.getMetrics"))!.Value.GetProperty("metrics");
        return metrics.EnumerateArray().Single(m => m.GetProperty("name").GetString() == "LayoutDuration")
            .GetProperty("value").GetDouble();
    }

    // Prerendered iframes load once and their interactive replacements load again, so count until the total holds.
    private static async Task<int> GetSettledFrameLoadsAsync(IPage page)
    {
        var previous = -1;
        var current = await page.EvaluateAsync<int>("() => window.dockFrameLoads ?? 0");
        while (current != previous)
        {
            await page.WaitForTimeoutAsync(500);
            previous = current;
            current = await page.EvaluateAsync<int>("() => window.dockFrameLoads ?? 0");
        }

        return current;
    }

    // Read through JSON.stringify: Playwright's direct mapping of nested objects fails.
    private static async Task<Dictionary<string, PanelCounts>> GetStatsAsync(IPage page)
    {
        var json = await page.EvaluateAsync<string>("async () => JSON.stringify(await window.dockSpike.stats())");
        return JsonSerializer.Deserialize<Dictionary<string, PanelCounts>>(json, JsonSerializerOptions.Web)!;
    }

    private static Task<int> GetLayoutCallCountAsync(IPage page) =>
        page.EvaluateAsync<int>("() => window.dockSpike.layoutCallCount()");

    private static Task MoveAsync(IPage page, string panel, string group) =>
        page.EvaluateAsync("([panel, group]) => window.dockSpike.move(panel, group)", new[] { panel, group });

    private static async Task ActivateAsync(IPage page, string panel)
    {
        await page.EvaluateAsync("panel => window.dockSpike.activate(panel)", panel);
        await Expect(page.GetByTestId($"tab-{panel}")).ToHaveAttributeAsync("aria-selected", "true");
    }

    public sealed record MoveMeasurement(int Frames, double LongestTask, int Calls = 0);
}
