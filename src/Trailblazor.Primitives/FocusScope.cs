using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Trailblazor.Primitives;

internal sealed class FocusScope : IAsyncDisposable
{
    private const string ModulePath = "./_content/Trailblazor.Primitives/focus-scope.js";

    private readonly IJSObjectReference _handle;
    private bool _disposed;

    private FocusScope(IJSObjectReference handle) => _handle = handle;

    public static async Task<FocusScope> CreateAsync(JsModuleLoader modules, ElementReference container,
        FocusScopeOptions options)
    {
        var module = await modules.ImportAsync(ModulePath);
        var handle = await module.InvokeAsync<IJSObjectReference>("createFocusScope", container, options);
        return new FocusScope(handle);
    }

    public ValueTask FocusMarkedIfUnmovedAsync() => _handle.InvokeVoidAsync("focusMarkedIfUnmoved");

    public ValueTask SetTrappedAsync(bool trapped) => _handle.InvokeVoidAsync("setTrapped", trapped);

    public ValueTask NoteFocusAsync() => _handle.InvokeVoidAsync("noteFocus");

    public ValueTask HoldEndedAsync() => _handle.InvokeVoidAsync("holdEnded");

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
