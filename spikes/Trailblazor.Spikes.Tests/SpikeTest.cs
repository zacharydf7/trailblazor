namespace Trailblazor.Spikes.Tests;

public abstract class SpikeTest(SpikeFixture fixture) : IAsyncDisposable
{
    private readonly List<IBrowserContext> _contexts = [];

    protected SpikeFixture Fixture { get; } = fixture;

    protected List<string> ScriptErrors { get; } = [];

    protected async Task<IPage> OpenAsync(string browser, string mode, string path)
    {
        var context = await Fixture.NewContextAsync(browser);
        _contexts.Add(context);
        var page = await context.NewPageAsync();
        page.PageError += (_, error) => ScriptErrors.Add(error);
        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                ScriptErrors.Add(message.Text);
            }
        };

        var separator = path.Contains('?') ? '&' : '?';
        var url = mode == "wasm" ? $"{path}{separator}mode=wasm" : path;
        await page.GotoAsync(new Uri(Fixture.BaseAddress, url).ToString());
        var renderer = mode == "wasm" ? "WebAssembly" : "Server";
        await page.Locator($"main[data-interactive][data-renderer='{renderer}']")
            .WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return page;
    }

    // Numbers for the README results; the test runner doesn't show output from passing tests.
    protected static void RecordMeasurement(string line) =>
        File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "measurements.log"),
            $"{DateTime.UtcNow:O} {line}{Environment.NewLine}");

    // Scroll events and ResizeObserver callbacks run during the next rendering update.
    protected static Task WaitForFramesAsync(IPage page) =>
        page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");

    // True when the element, not something stacked above it, receives pointer input at its center.
    protected static Task<bool> IsTopmostAtCenterAsync(IPage page, string testId) =>
        page.EvaluateAsync<bool>("""
            testId => {
                const element = document.querySelector(`[data-testid=${testId}]`);
                const box = element.getBoundingClientRect();
                const hit = document.elementFromPoint(box.left + box.width / 2, box.top + box.height / 2);
                return element.contains(hit);
            }
            """, testId);

    public async ValueTask DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.CloseAsync();
        }
    }
}
