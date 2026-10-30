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
    private WebApplication? _docs;

    public Uri BaseAddress { get; private set; } = null!;

    public Uri DocsAddress { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _factory.UseKestrel(0);
        _factory.StartServer();
        BaseAddress = _factory.ClientOptions.BaseAddress;
        DocsAddress = await StartDocsSiteAsync();

        _playwright = await Playwright.CreateAsync();
        foreach (var name in AllBrowsers.Names)
        {
            _browsers[name] = new Lazy<Task<IBrowser>>(() => _playwright[name].LaunchAsync());
        }
    }

    // Serves the docs site the build published, answering any other path with the app, as GitHub Pages does.
    private async Task<Uri> StartDocsSiteAsync()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "docs-site", "wwwroot");
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { WebRootPath = root });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _docs = builder.Build();
        // The runtime's ICU data files end in .dat, which has no registered content type.
        var types = new FileExtensionContentTypeProvider { Mappings = { [".dat"] = "application/octet-stream" } };
        _docs.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = types });
        _docs.MapFallbackToFile("index.html");
        await _docs.StartAsync();
        var address = _docs.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        return new Uri(address.Addresses.Single());
    }

    public async Task<IBrowserContext> NewContextAsync(string browser, BrowserNewContextOptions? options = null)
    {
        // A case for a browser TRAILBLAZOR_BROWSERS leaves out is skipped; an unknown browser name still fails.
        Assert.SkipWhen(AllBrowsers.Supported.Contains(browser) && !_browsers.ContainsKey(browser),
            $"TRAILBLAZOR_BROWSERS leaves out {browser}.");
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
        if (_docs is not null)
        {
            await _docs.DisposeAsync();
        }

        await _factory.DisposeAsync();
    }
}
