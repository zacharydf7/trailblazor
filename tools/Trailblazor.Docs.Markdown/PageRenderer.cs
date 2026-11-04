using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Trailblazor.Docs.Markdown;

// Renders the docs site's pages to HTML outside the browser, as a server would before the app starts, so no script
// and no after-render work runs.
public sealed class PageRenderer : IAsyncDisposable
{
    private readonly SiteNavigation _navigation = new();
    private readonly ServiceProvider _services;
    private readonly HtmlRenderer _renderer;

    public PageRenderer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<NavigationManager>(_navigation);
        services.AddSingleton<IJSRuntime, NoScripts>();
        // Pages ask for one, though only after the first render, which a static render never reaches.
        services.AddSingleton(new HttpClient());
        services.AddTrailblazor();
        _services = services.BuildServiceProvider();
        _renderer = new HtmlRenderer(_services, _services.GetRequiredService<ILoggerFactory>());
    }

    // Renders at the page's own address, so links to its sections include the page's path.
    public Task<string> RenderAsync(string path, Type page, IDictionary<string, object?>? parameters = null) =>
        _renderer.Dispatcher.InvokeAsync(async () =>
        {
            _navigation.Open(path);
            var rendered = await _renderer.RenderComponentAsync(page,
                parameters is null ? ParameterView.Empty : ParameterView.FromDictionary(parameters));
            return rendered.ToHtmlString();
        });

    public async ValueTask DisposeAsync()
    {
        await _renderer.DisposeAsync();
        await _services.DisposeAsync();
    }

    private sealed class SiteNavigation : NavigationManager
    {
        public SiteNavigation() => Initialize(DocsMarkdown.Site, DocsMarkdown.Site);

        public void Open(string path) => Uri = ToAbsoluteUri(path).ToString();
    }

    // Scripts run only after the first render, which a static render never reaches.
    private sealed class NoScripts : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) => ValueTask.FromResult(default(TValue)!);
    }
}
