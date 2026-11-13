using System.Diagnostics;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Tests of the components needing the most round trips, rerun with each message delayed as on a slow link.
internal static class SlowLinkDelay
{
    public static readonly TimeSpan Value = TimeSpan.FromMilliseconds(150);
}

public class SlowLinkSelectKeysTests(BrowserFixture fixture) : SelectKeysTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}

public class SlowLinkSliderKeysTests(BrowserFixture fixture) : SliderKeysTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}

public class SlowLinkToggleGroupKeysTests(BrowserFixture fixture) : ToggleGroupKeysTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}

public class SlowLinkTreeViewKeysTests(BrowserFixture fixture) : TreeViewKeysTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}

public class SlowLinkTreeFocusTests(BrowserFixture fixture) : TreeFocusTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}

public class SlowLinkGridSelectTests(BrowserFixture fixture) : GridSelectTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}

// A context menu on a key it hasn't drawn waits for the server, so a second right-click can land in that wait.
public class SlowLinkContextMenuTests(BrowserFixture fixture) : ContextMenuTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}

public class SlowLinkGridEditKeysTests(BrowserFixture fixture) : GridEditKeysTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}

public class SlowLinkGridScrollTests(BrowserFixture fixture) : GridScrollTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}

public class SlowLinkSplitterKeysTests(BrowserFixture fixture) : SplitterKeysTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}

public class SlowLinkDataGridKeysTests(BrowserFixture fixture) : DataGridKeysTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}

public class SlowLinkTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(1);

    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(Delay);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task UseSlowLinkAsync_ShouldHoldBackWhatThePageSends_WhenCalled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toggle-group");
        var terrain = page.GetByRole(AriaRole.Group, new() { Name = "Map view" })
            .GetByRole(AriaRole.Button, new() { Name = "Terrain", Exact = true });
        await Expect(page.GetByText("View: Trails")).ToBeVisibleAsync();
        var clicked = Stopwatch.StartNew();

        await terrain.ClickAsync();
        await Expect(page.GetByText("View: Terrain")).ToBeVisibleAsync();

        Assert.True(clicked.Elapsed >= Delay, $"The click took {clicked.Elapsed}.");
    }
}

// The server's answer to End has to arrive after the ArrowUp pressed right after it, as on any real link; a local
// server can answer first. The delay is long enough to outlast a stall between the two presses.
public class SlowLinkSelectWaitTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(400);

    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(Delay);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiSelect_ShouldKeepTheLatestKeysOption_WhenAnEarlierKeyIsStillWaitingForItsOption(
        string browser)
    {
        var page = await OpenAsync(browser, "/select-items");
        var trails = page.GetByRole(AriaRole.Combobox, new() { Name = "Long trails" });
        await Expect(trails).ToHaveAttributeAsync("data-ready", "");
        await trails.FocusAsync();
        await trails.PressAsync("ArrowDown");
        // The first options draw a moment after the list opens, so wait for Trail 1 before the quick presses.
        await page.Keyboard.PressAsync("ArrowDown");
        await SelectTests.ExpectActiveAsync(page, trails, "Trail 1");
        for (var step = 0; step < 5; step++)
        {
            await page.Keyboard.PressAsync("ArrowDown");
        }

        await SelectTests.ExpectActiveAsync(page, trails, "Trail 6");

        // End waits for the server to draw Trail 1000, and ArrowUp, pressed meanwhile, replaces it.
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("ArrowUp");
        // Longer than the script's 2-second wait for an option, so End's search has given up by now.
        await page.WaitForTimeoutAsync(2500);

        await SelectTests.ExpectActiveAsync(page, trails, "Trail 5");
    }
}

// Each Shift+Arrow has to be pressed before the server draws the one before it, as on a slow link.
public class SlowLinkListBoxTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiListBox_ShouldKeepAnOptionShiftAddedTwice_WhenTheFirstHasntBeenDrawnYet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/listbox");
        var gear = page.GetByRole(AriaRole.Listbox, new() { Name = "Gear to pack", Exact = true });
        await Expect(gear).ToHaveAttributeAsync("data-ready", "");
        await gear.FocusAsync();

        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await page.Keyboard.PressAsync("Shift+ArrowUp");
        await page.Keyboard.PressAsync("Shift+ArrowDown");
        // Long enough for every press to reach the server and its answer to come back.
        await page.WaitForTimeoutAsync(1500);

        await Expect(page.GetByText("Packing: ")).ToHaveTextAsync("Packing: Tent, Stove");
    }
}

public class SlowLinkTreeGridTests(BrowserFixture fixture) : TreeGridTests(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(SlowLinkDelay.Value);
}
