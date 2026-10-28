namespace Trailblazor.Spikes.Tests.Overlays;

// Spike A, criteria 7 and 8. See spikes/overlays/README.md.
public class PositioningTests(SpikeFixture fixture) : SpikeTest(fixture)
{
    public static TheoryData<string, string, string> Cases => new(
        BrowsersAndModes.Browsers.SelectMany(browser => BrowsersAndModes.Modes.SelectMany(mode =>
            new[] { (browser, mode, "js"), (browser, mode, "native") })));

    // Tools 0 and 1 overflow the left edge when centered: JS shifts to its 8 px padding, native lands flush at 0.
    public static TheoryData<string, string, string, int, double> EdgeTooltips => new(
        from browser in BrowsersAndModes.Browsers
        from mode in BrowsersAndModes.Modes
        from tool in new[] { 0, 1 }
        from path in new[] { ("js", 8.0), ("native", 0.0) }
        select (browser, mode, path.Item1, tool, path.Item2));

    public static TheoryData<string, string, string, string> Placements => new(
        from browser in BrowsersAndModes.Browsers
        from mode in BrowsersAndModes.Modes
        from positioning in new[] { "js", "native" }
        from side in new[] { "top", "bottom", "start", "end" }
        select (browser, mode, positioning, side));

    public static TheoryData<string> BrowsersOnly => new(BrowsersAndModes.Browsers);

    [Theory, MemberData(nameof(EdgeTooltips))]
    public async Task Tooltip_ShouldShiftInsideTheViewportBelowItsTrigger_WhenItWouldOverflowTheLeftEdge(string browser,
        string mode, string positioning, int tool, double expectedX)
    {
        var page = await OpenTooltipAsync(browser, mode, positioning, tool);

        var trigger = await BoxAsync(page, $"tool-{tool}");
        var tooltip = await BoxAsync(page, "tooltip");
        Assert.Equal(expectedX, tooltip.X, 1.0);
        Assert.Equal(trigger.Y + trigger.Height + 8, tooltip.Y, 1.0);
        Assert.True(await IsTopmostAtCenterAsync(page, "tooltip"));
        Assert.Empty(ScriptErrors);
    }

    [Theory, MemberData(nameof(Cases))]
    public async Task Tooltip_ShouldCenterBelowItsTrigger_WhenThereIsRoom(string browser, string mode,
        string positioning)
    {
        var page = await OpenTooltipAsync(browser, mode, positioning, tool: 2);

        var trigger = await BoxAsync(page, "tool-2");
        var tooltip = await BoxAsync(page, "tooltip");
        Assert.Equal(trigger.X + (trigger.Width - tooltip.Width) / 2, tooltip.X, 1.0);
        Assert.Equal(trigger.Y + trigger.Height + 8, tooltip.Y, 1.0);
        Assert.Empty(ScriptErrors);
    }

    [Theory, MemberData(nameof(Placements))]
    public async Task Placement_ShouldSitOnTheRequestedSide_WhenThereIsRoom(string browser, string mode,
        string positioning, string side)
    {
        var page = await OpenAsync(browser, mode, $"/positioning?positioning={positioning}&side={side}");

        await page.GetByTestId("center-anchor").ClickAsync();
        await Expect(page.GetByTestId("shown")).ToHaveTextAsync("1");
        await WaitForFramesAsync(page);

        var anchor = await BoxAsync(page, "center-anchor");
        var floating = await BoxAsync(page, "placement");
        (double X, double Y) expected = side switch
        {
            "top" => (anchor.X + (anchor.Width - floating.Width) / 2, anchor.Y - 8 - floating.Height),
            "bottom" => (anchor.X + (anchor.Width - floating.Width) / 2, anchor.Y + anchor.Height + 8),
            "start" => (anchor.X - 8 - floating.Width, anchor.Y + (anchor.Height - floating.Height) / 2),
            _ => (anchor.X + anchor.Width + 8, anchor.Y + (anchor.Height - floating.Height) / 2),
        };
        Assert.Equal(expected.X, floating.X, 1.0);
        Assert.Equal(expected.Y, floating.Y, 1.0);
        Assert.Empty(ScriptErrors);
    }

    [Theory, MemberData(nameof(Cases))]
    public async Task Listbox_ShouldLineUpWithItsTrigger_WhenOpenedFromAScrolledDialogTable(string browser, string mode,
        string positioning)
    {
        var page = await OpenFormDialogAsync(browser, mode, $"/positioning?positioning={positioning}");
        await page.GetByTestId("form-body").EvaluateAsync("body => body.scrollTop = 200");

        await page.GetByTestId("select-10").ClickAsync();
        await Expect(page.GetByTestId("shown")).ToHaveTextAsync("1");
        await WaitForFramesAsync(page);

        await AssertBelowAndStartAlignedAsync(page, "select-10", "dialog-listbox");
        Assert.Empty(ScriptErrors);
    }

    [Theory, MemberData(nameof(Cases))]
    public async Task Listbox_ShouldFollowItsTrigger_WhenTheDialogBodyScrollsWhileOpen(string browser, string mode,
        string positioning)
    {
        var page = await OpenFormDialogAsync(browser, mode, $"/positioning?positioning={positioning}");
        await page.GetByTestId("select-10").ClickAsync();
        await Expect(page.GetByTestId("shown")).ToHaveTextAsync("1");

        await page.GetByTestId("form-body").EvaluateAsync("body => body.scrollTop += 40");
        await WaitForFramesAsync(page);

        await AssertBelowAndStartAlignedAsync(page, "select-10", "dialog-listbox");
        Assert.Empty(ScriptErrors);
    }

    [Theory, MemberData(nameof(Cases))]
    public async Task Listbox_ShouldFlipAboveItsTrigger_WhenTheTriggerIsNearTheViewportBottom(string browser,
        string mode, string positioning)
    {
        var page = await OpenAsync(browser, mode, $"/positioning?positioning={positioning}");

        await page.GetByTestId("bottom-select").ClickAsync();
        await Expect(page.GetByTestId("shown")).ToHaveTextAsync("1");
        await WaitForFramesAsync(page);

        await AssertAboveAndStartAlignedAsync(page, "bottom-select", "page-listbox");
        Assert.Empty(ScriptErrors);
    }

    [Theory, MemberData(nameof(Cases))]
    public async Task Listbox_ShouldFollowItsTrigger_WhenTheWindowResizesWhileOpen(string browser, string mode,
        string positioning)
    {
        var page = await OpenAsync(browser, mode, $"/positioning?positioning={positioning}");
        await page.GetByTestId("bottom-select").ClickAsync();
        await Expect(page.GetByTestId("shown")).ToHaveTextAsync("1");

        await page.SetViewportSizeAsync(1000, 600);
        await WaitForFramesAsync(page);

        await AssertAboveAndStartAlignedAsync(page, "bottom-select", "page-listbox");
        Assert.Empty(ScriptErrors);
    }

    [Theory, MemberData(nameof(BrowsersOnly))]
    public async Task Circuit_ShouldTearDownWithoutServerErrors_WhenThePageIsLeftWithOverlaysOpen(string browser)
    {
        var run = Guid.NewGuid().ToString("N");
        var page = await OpenFormDialogAsync(browser, "server", $"/positioning?positioning=js&run={run}");
        await page.GetByTestId("select-3").ClickAsync();
        await Expect(page.GetByTestId("shown")).ToHaveTextAsync("1");
        var errorsBefore = Fixture.ServerErrors.Count;

        await page.GotoAsync("about:blank");

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!Fixture.ServerWarnings.Any(w => w.Contains(run)) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        Assert.True(Fixture.ServerWarnings.Any(w => w.Contains(run)), "The circuit was not torn down within 15 seconds.");
        Assert.Equal(errorsBefore, Fixture.ServerErrors.Count);
    }

    private async Task<IPage> OpenTooltipAsync(string browser, string mode, string positioning, int tool)
    {
        var page = await OpenAsync(browser, mode, $"/positioning?positioning={positioning}");
        await page.GetByTestId($"tool-{tool}").ClickAsync();
        await Expect(page.GetByTestId("shown")).ToHaveTextAsync("1");
        await WaitForFramesAsync(page);
        return page;
    }

    private async Task<IPage> OpenFormDialogAsync(string browser, string mode, string path)
    {
        var page = await OpenAsync(browser, mode, path);
        await page.GetByTestId("open-form-dialog").ClickAsync();
        await Expect(page.GetByTestId("form-dialog")).ToHaveAttributeAsync("open", "");
        return page;
    }

    private static async Task AssertBelowAndStartAlignedAsync(IPage page, string triggerId, string listboxId)
    {
        var trigger = await BoxAsync(page, triggerId);
        var listbox = await BoxAsync(page, listboxId);
        Assert.Equal(trigger.X, listbox.X, 1.0);
        Assert.Equal(trigger.Y + trigger.Height + 4, listbox.Y, 1.0);
    }

    private static async Task AssertAboveAndStartAlignedAsync(IPage page, string triggerId, string listboxId)
    {
        var trigger = await BoxAsync(page, triggerId);
        var listbox = await BoxAsync(page, listboxId);
        Assert.Equal(trigger.X, listbox.X, 1.0);
        Assert.Equal(trigger.Y - 4, listbox.Y + listbox.Height, 1.0);
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(IPage page, string testId) =>
        (await page.GetByTestId(testId).BoundingBoxAsync())!;
}
