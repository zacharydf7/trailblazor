using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Trailblazor.BrowserTests;

// These tests run alone, so the circuit and any warnings a test finds in the server's logs are its own.
[CollectionDefinition(nameof(CircuitDrops), DisableParallelization = true)]
public sealed class CircuitDrops;

[Collection(nameof(CircuitDrops))]
public class CircuitTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Each page is left mid-interaction: a select's list open, a tree row expanded, a grid cell focused.
    [Theory]
    [InlineData("/select")]
    [InlineData("/tree-view")]
    [InlineData("/examples/data-grid")]
    public async Task Server_ShouldDisposeTheCircuitWithoutErrors_WhenItDropsMidInteraction(string path)
    {
        var from = Fixture.ServerLogs.GetSnapshot().Count;
        var page = await OpenAsync("chromium", path);
        var circuit = Fixture.ServerLogs.GetSnapshot().Skip(from)
            .First(record => record.Message.StartsWith("Created circuit", StringComparison.Ordinal))
            .GetStructuredStateValue("CircuitId")!;
        await StartInteractionAsync(page, path);

        await page.EvaluateAsync("() => Blazor.disconnect()");
        // Keys pressed as the circuit goes can still reach the server while it disposes the components.
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Escape");

        var logs = await LogsUntilDisposedAsync(circuit, from);
        Assert.Empty(logs.Where(record => record.Level >= LogLevel.Warning
                && (record.GetStructuredStateValue("CircuitId") is not { } id || id == circuit))
            .Select(record => $"{record.Category}: {record.Message} {record.Exception}"));
    }

    // Blazor's own event dispatch throws for a DOM event once the circuit is gone; only a script's own call counts here.
    // The grid's script makes no calls of its own, so only the select and the tree are checked.
    [Theory]
    [InlineData("/select")]
    [InlineData("/tree-view")]
    public async Task Scripts_ShouldRaiseNoErrors_WhenTheCircuitDropsMidInteraction(string path)
    {
        var page = await OpenAsync("chromium", path);
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);
        await StartInteractionAsync(page, path);

        await page.EvaluateAsync("() => Blazor.disconnect()");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Escape");
        // A rejected call reaches the page's error events a little after the key that made it, so the test waits.
        await page.WaitForTimeoutAsync(500);

        Assert.DoesNotContain(errors, CameFromAScriptsOwnCall);
    }

    private static bool CameFromAScriptsOwnCall(string error)
    {
        var frames = error.Split('\n');
        var call = Array.FindIndex(frames, frame => frame.Contains(".invokeMethodAsync", StringComparison.Ordinal));
        return call >= 0 && call + 1 < frames.Length
            && frames[call + 1].Contains("/_content/Trailblazor", StringComparison.Ordinal);
    }

    private static async Task StartInteractionAsync(IPage page, string path)
    {
        switch (path)
        {
            case "/select":
                await page.GetByRole(AriaRole.Combobox, new() { Name = "Size" }).ClickAsync();
                await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Size" })).ToBeVisibleAsync();
                break;
            case "/tree-view":
                await Expect(page.Locator("[role=tree]").First).ToHaveAttributeAsync("data-ready", "");
                await page.GetByRole(AriaRole.Treeitem).First.FocusAsync();
                await page.Keyboard.PressAsync("ArrowRight");
                break;
            default:
                var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail keys" });
                await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
                    .ToHaveAttributeAsync("data-ready", "");
                await grid.GetByRole(AriaRole.Checkbox, new() { Name = "Select all rows" }).FocusAsync();
                await page.Keyboard.PressAsync("ArrowRight");
                break;
        }
    }

    // The server disposes the circuit as soon as the disconnect arrives, logging each step at Debug.
    private async Task<IReadOnlyList<FakeLogRecord>> LogsUntilDisposedAsync(string circuit, int from)
    {
        for (var wait = 0; wait < 100; wait++)
        {
            var logs = Fixture.ServerLogs.GetSnapshot().Skip(from).ToList();
            if (logs.Any(record => record.GetStructuredStateValue("CircuitId") == circuit
                && record.Message.StartsWith("Disposing circuit", StringComparison.Ordinal)
                && !record.Message.EndsWith("started.", StringComparison.Ordinal)))
            {
                return logs;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException($"The server didn't dispose circuit {circuit} within 10 seconds of the disconnect.");
    }
}
