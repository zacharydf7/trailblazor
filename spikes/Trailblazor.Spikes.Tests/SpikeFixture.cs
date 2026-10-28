using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trailblazor.Spikes.Tests;

[assembly: AssemblyFixture(typeof(SpikeFixture))]

namespace Trailblazor.Spikes.Tests;

public sealed class SpikeFixture : IAsyncLifetime
{
    private readonly ErrorLogCollector _errorLogs = new();
    private readonly Dictionary<string, Lazy<Task<IBrowser>>> _browsers = [];
    private WebApplicationFactory<Program> _factory = null!;
    private IPlaywright? _playwright;

    public Uri BaseAddress { get; private set; } = null!;

    public IReadOnlyCollection<string> ServerErrors =>
        _errorLogs.Entries.Where(e => e.Level >= LogLevel.Error).Select(e => e.Message).ToList();

    public IReadOnlyCollection<string> ServerWarnings =>
        _errorLogs.Entries.Where(e => e.Level == LogLevel.Warning).Select(e => e.Message).ToList();

    public async ValueTask InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(_errorLogs));
            builder.ConfigureServices(services => services.Configure<CircuitOptions>(options =>
                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(1)));
        });
        _factory.UseKestrel(0);
        _factory.StartServer();
        BaseAddress = _factory.ClientOptions.BaseAddress;

        _playwright = await Playwright.CreateAsync();
        foreach (var name in BrowsersAndModes.Browsers)
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

    private sealed class ErrorLogCollector : ILoggerProvider, ILogger
    {
        public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => this;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                Entries.Enqueue((logLevel, $"{formatter(state, exception)} {exception}"));
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public void Dispose()
        {
        }
    }
}
