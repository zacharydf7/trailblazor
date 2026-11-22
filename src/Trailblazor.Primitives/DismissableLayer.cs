using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Trailblazor.Primitives;

internal sealed class DismissableLayer : ScriptListener, IAsyncDisposable
{
    private const string ModulePath = "./_content/Trailblazor.Primitives/dismissable-layer.js";

    private readonly EventCallback<DismissReason> _onDismiss;
    private readonly DotNetObjectReference<DismissableLayer> _reference;
    private IJSObjectReference? _handle;
    private bool _disposed;

    private DismissableLayer(EventCallback<DismissReason> onDismiss, Func<Exception, Task> onException)
        : base(onException)
    {
        _onDismiss = onDismiss;
        _reference = DotNetObjectReference.Create(this);
    }

    public static async Task<DismissableLayer> CreateAsync(JsModuleLoader modules, ElementReference layer,
        ElementReference? trigger, EventCallback<DismissReason> onDismiss, Func<Exception, Task> onException)
    {
        var dismissableLayer = new DismissableLayer(onDismiss, onException);
        var module = await modules.ImportAsync(ModulePath);
        dismissableLayer._handle = await module.InvokeAsync<IJSObjectReference>("createDismissableLayer", layer,
            trigger, dismissableLayer._reference);
        return dismissableLayer;
    }

    [JSInvokable]
    public Task OnDismissRequested(DismissReason reason) => Run(() => _onDismiss.InvokeAsync(reason));

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _reference.Dispose();
    }
}
