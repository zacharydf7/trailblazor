using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Trailblazor.BrowserTests;

[assembly: AssemblyFixture(typeof(BrowserFixture))]

namespace Trailblazor.BrowserTests;

// Runs the test host and the published docs site on real Kestrel ports, and keeps one browser of each engine open
// for the whole run.
public sealed class BrowserFixture : IAsyncLifetime
{
    // Circuits log their creation and disposal at Debug, so a test can follow its own circuit in what the server logged.
    // They log failed event dispatches at Debug too, and the run checks for those when it ends.
    // The derived factory ignores UseKestrel(port), so UseUrls asks for a free one; otherwise the app takes port 5000.
    private readonly WebApplicationFactory<Program> _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(
        host => host.UseUrls("http://127.0.0.1:0")
            .ConfigureServices(services => services.AddFakeLogging().AddLogging(logging =>
                logging.AddFilter("Microsoft.AspNetCore.Components.Server.Circuits", LogLevel.Debug))));
    private readonly Dictionary<string, Lazy<Task<IBrowser>>> _browsers = [];
    private readonly ConcurrentBag<IBrowser> _spareFirefoxes = [];
    private IPlaywright? _playwright;
    private readonly List<WebApplication> _sites = [];

    // Playwright starts Chromium with --hide-scrollbars, and a test of the page's width needs them drawn.
    public const string ChromiumWithScrollbars = "chromium with scrollbars";

    public Uri BaseAddress { get; private set; } = null!;

    // What the test host has logged.
    public FakeLogCollector ServerLogs => _factory.Services.GetFakeLogCollector();

    public Uri DocsAddress { get; private set; } = null!;

    public Uri TrimmedAppAddress { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _factory.UseKestrel();
        _factory.StartServer();
        BaseAddress = _factory.ClientOptions.BaseAddress;
        DocsAddress = await StartSiteAsync("docs-site");
        TrimmedAppAddress = await StartSiteAsync("trimmed-app");

        _playwright = await Playwright.CreateAsync();
        foreach (var name in AllBrowsers.Names)
        {
            _browsers[name] = new Lazy<Task<IBrowser>>(() => _playwright[name].LaunchAsync());
        }

        _browsers[ChromiumWithScrollbars] = new Lazy<Task<IBrowser>>(() =>
            _playwright.Chromium.LaunchAsync(new() { IgnoreDefaultArgs = ["--hide-scrollbars"] }));
    }

    // Serves a WebAssembly site the build published, answering any other path with the app, as GitHub Pages does.
    private async Task<Uri> StartSiteAsync(string folder)
    {
        var root = Path.Combine(AppContext.BaseDirectory, folder, "wwwroot");
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { WebRootPath = root });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var site = builder.Build();
        _sites.Add(site);
        // The runtime's ICU data files end in .dat, which has no registered content type.
        var types = new FileExtensionContentTypeProvider
        {
            Mappings = { [".dat"] = "application/octet-stream", [".md"] = "text/markdown" },
        };
        site.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = types });
        site.MapFallbackToFile("index.html");
        await site.StartAsync();
        var address = site.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        return new Uri(address.Addresses.Single());
    }

    public async Task<IBrowserContext> NewContextAsync(string browser, BrowserNewContextOptions? options = null)
    {
        SkipWhenLeftOut(browser);
        var launched = await _browsers[browser].Value;
        return await launched.NewContextAsync(options);
    }

    // A Firefox that no other test has a page in, until the caller hands it back with its pages closed.
    public async Task<IBrowser> TakeAFirefoxAsync()
    {
        SkipWhenLeftOut("firefox");
        return _spareFirefoxes.TryTake(out var spare) ? spare : await _playwright!.Firefox.LaunchAsync();
    }

    public void HandBack(IBrowser firefox) => _spareFirefoxes.Add(firefox);

    // A case for a browser TRAILBLAZOR_BROWSERS leaves out is skipped; an unknown browser name still fails.
    private void SkipWhenLeftOut(string browser)
    {
        var engine = browser == ChromiumWithScrollbars ? "chromium" : browser;
        Assert.SkipWhen(AllBrowsers.Supported.Contains(engine) && !_browsers.ContainsKey(engine),
            $"TRAILBLAZOR_BROWSERS leaves out {engine}.");
    }

    public async ValueTask DisposeAsync()
    {
        // A failed event dispatch only shows in the server log, so any one fails the run.
        var failed = ServerLogs.GetSnapshot().Where(log => log.Id.Name == "InvokeInstanceDotNetMethodException"
            && log.Message.Contains("'DispatchEventAsync'", StringComparison.Ordinal)).ToList();

        foreach (var browser in _browsers.Values.Where(b => b.IsValueCreated))
        {
            await (await browser.Value).CloseAsync();
        }

        foreach (var firefox in _spareFirefoxes)
        {
            await firefox.CloseAsync();
        }

        _playwright?.Dispose();
        foreach (var site in _sites)
        {
            await site.DisposeAsync();
        }

        await _factory.DisposeAsync();
        if (failed.Count > 0)
        {
            throw new InvalidOperationException($"The test host failed to dispatch {failed.Count} browser events. The first "
                + $"failed with {failed[0].Exception}");
        }
    }
}
