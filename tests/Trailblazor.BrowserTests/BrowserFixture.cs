using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.BrowserTests;

[assembly: AssemblyFixture(typeof(BrowserFixture))]

namespace Trailblazor.BrowserTests;

// Runs the test host and the published docs site on real Kestrel ports, and keeps one browser of each engine open
// for the whole run.
public sealed class BrowserFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly Dictionary<string, Lazy<Task<IBrowser>>> _browsers = [];
    private IPlaywright? _playwright;
    private readonly List<WebApplication> _sites = [];

    // Playwright starts Chromium with --hide-scrollbars, and a test of the page's width needs them drawn.
    public const string ChromiumWithScrollbars = "chromium with scrollbars";

    public Uri BaseAddress { get; private set; } = null!;

    public Uri DocsAddress { get; private set; } = null!;

    public Uri TrimmedAppAddress { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _factory.UseKestrel(0);
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
        // A case for a browser TRAILBLAZOR_BROWSERS leaves out is skipped; an unknown browser name still fails.
        var engine = browser == ChromiumWithScrollbars ? "chromium" : browser;
        Assert.SkipWhen(AllBrowsers.Supported.Contains(engine) && !_browsers.ContainsKey(engine),
            $"TRAILBLAZOR_BROWSERS leaves out {engine}.");
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
        foreach (var site in _sites)
        {
            await site.DisposeAsync();
        }

        await _factory.DisposeAsync();
    }
}
