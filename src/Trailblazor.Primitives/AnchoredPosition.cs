using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Trailblazor.Primitives;

internal sealed class AnchoredPosition : IAsyncDisposable
{
    private const string ModulePath = "./_content/Trailblazor.Primitives/anchored-position.js";

    private readonly IJSObjectReference _handle;
    private bool _disposed;

    private AnchoredPosition(IJSObjectReference handle) => _handle = handle;

    public static async Task<AnchoredPosition> CreateAsync(JsModuleLoader modules, ElementReference floating,
        ElementReference anchor, AnchoredPositionOptions options)
    {
        var module = await modules.ImportAsync(ModulePath);
        var handle = await module.InvokeAsync<IJSObjectReference>("createAnchoredPosition", floating, anchor, options);
        return new AnchoredPosition(handle);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _handle.DisposeHandleAsync();
    }
}
