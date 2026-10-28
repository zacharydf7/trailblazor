using Microsoft.AspNetCore.Mvc.Testing;
using Trailblazor.BrowserTests;

[assembly: AssemblyFixture(typeof(BrowserFixture))]

namespace Trailblazor.BrowserTests;

// Runs the test host on a real Kestrel port and keeps one browser of each engine open for the whole run.
public sealed class BrowserFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly Dictionary<string, Lazy<Task<IBrowser>>> _browsers = [];
    private IPlaywright? _playwright;

    public Uri BaseAddress { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _factory.UseKestrel(0);
        _factory.StartServer();
        BaseAddress = _factory.ClientOptions.BaseAddress;

        _playwright = await Playwright.CreateAsync();
        foreach (var name in AllBrowsers.Names)
        {
            _browsers[name] = new Lazy<Task<IBrowser>>(() => _playwright[name].LaunchAsync());
        }
    }

    public async Task<IBrowserContext> NewContextAsync(string browser, BrowserNewContextOptions? options = null)
    {
        var launched = await _browsers[browser].Value;
        return await launched.NewContextAsync(options);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var browser in _browsers.Values.Where(b => b.IsValueCreated))
        {
            await (await browser.Value).CloseAsync();
        }

        _playwright?.Dispose();
        await _factory.DisposeAsync();
    }
}
