using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Attaches the arrow-key and Enter handling to the palette's search box and list while they're shown. The dialog draws
// them anew each time it opens, even when it reopens while still closing, so the handling comes and goes with them.
internal sealed class CommandPaletteKeys : ComponentBase, IAsyncDisposable
{
    private ElementReference _scope;
    private IJSObjectReference? _handle;
    private bool _disposed;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "tb-command-palette__search");
        builder.AddElementReferenceCapture(2, element => _scope = element);
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/command-palette.js");
            if (_disposed)
            {
                return;
            }

            var handle = await module.InvokeAsync<IJSObjectReference?>("attachPalette", _scope);
            if (handle is not null && _disposed)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the palette went with it.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }
    }
}
