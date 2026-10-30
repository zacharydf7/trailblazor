using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

internal sealed class BrowserService(JsModuleLoader modules) : IBrowserService
{
    public async Task<bool> CopyTextAsync(string text) =>
        await (await ImportAsync()).InvokeAsync<bool>("copyText", text);

    public async Task DownloadAsync(Stream content, string fileName, string? contentType = null)
    {
        using var stream = new DotNetStreamReference(content, leaveOpen: true);
        await (await ImportAsync()).InvokeVoidAsync("download", stream, fileName, contentType);
    }

    public async Task<bool> SaveAsync(Stream content, string fileName, string? contentType = null)
    {
        using var stream = new DotNetStreamReference(content, leaveOpen: true);
        return await (await ImportAsync()).InvokeAsync<bool>("save", stream, fileName, contentType);
    }

    public async Task<bool> CanPickColorAsync() => await (await ImportAsync()).InvokeAsync<bool>("canPickColor");

    public async Task<string?> PickColorAsync() => await (await ImportAsync()).InvokeAsync<string?>("pickColor");

    public async Task ScrollIntoViewAsync(ElementReference element, ScrollAlignment alignment = ScrollAlignment.Nearest,
        bool smooth = false) =>
        await (await ImportAsync()).InvokeVoidAsync("scrollIntoView", element, alignment.ToString().ToLowerInvariant(),
            smooth);

    public async Task<Viewport> GetViewportAsync() => await (await ImportAsync()).InvokeAsync<Viewport>("getViewport");

    public Task<IAsyncDisposable> ObserveViewportAsync(Func<Viewport, Task> onChange) =>
        ObserveAsync("observeViewport", onChange);

    public Task<IAsyncDisposable> ObserveBreakpointAsync(Func<Breakpoint, Task> onChange) =>
        ObserveAsync("observeBreakpoint", onChange, Viewport.MinWidths);

    private async Task<IAsyncDisposable> ObserveAsync<T>(string function, Func<T, Task> onChange, params object[] args)
    {
        var listener = DotNetObjectReference.Create(new BrowserListener<T>(onChange));
        try
        {
            var handle = await (await ImportAsync()).InvokeAsync<IJSObjectReference>(function, [listener, .. args]);
            return new Observation(handle, listener);
        }
        catch
        {
            listener.Dispose();
            throw;
        }
    }

    private Task<IJSObjectReference> ImportAsync() => modules.ImportAsync("./_content/Trailblazor/browser.js");

    private sealed class Observation(IJSObjectReference handle, IDisposable listener) : IAsyncDisposable
    {
        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await handle.DisposeHandleAsync();
            listener.Dispose();
        }
    }
}

// Hears the browser report a change, so the service needn't expose [JSInvokable] methods.
internal sealed class BrowserListener<T>(Func<T, Task> onChange)
{
    [JSInvokable]
    public Task OnChange(T value) => onChange(value);
}
