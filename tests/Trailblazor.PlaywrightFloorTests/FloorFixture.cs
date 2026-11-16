using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Trailblazor.Testing.Playwright;
using Trailblazor.PlaywrightFloorTests;

[assembly: AssemblyFixture(typeof(FloorFixture))]

namespace Trailblazor.PlaywrightFloorTests;

// Runs the browser tests' host on a real Kestrel port and keeps one Chromium open for the run.
public sealed class FloorFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory =
        new WebApplicationFactory<Program>().WithWebHostBuilder(host => host.UseUrls("http://127.0.0.1:0"));
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        _factory.UseKestrel();
        _factory.StartServer();
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
    }

    public async Task<IPage> OpenAsync(string path)
    {
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();
        // Over a slow link the app hears each action late, so a helper that doesn't wait for it returns too soon.
        await page.UseSlowLinkAsync(TimeSpan.FromMilliseconds(400));
        await page.GotoAsync(new Uri(_factory.ClientOptions.BaseAddress, path).ToString());
        return page;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
        await _factory.DisposeAsync();
    }
}
