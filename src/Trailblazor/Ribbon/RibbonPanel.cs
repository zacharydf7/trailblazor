using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// A ribbon tab's row of groups: a toolbar, so the controls of all its groups are one tab stop that the arrow keys
// move through. It is a component of its own so that it attaches the script when its tab's panel first renders,
// which for a tab shown later is long after the tab does.
internal sealed class RibbonPanel : ComponentBase, IAsyncDisposable
{
    private ElementReference _toolbar;
    private IJSObjectReference? _handle;
    private bool _disposed;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "role", "toolbar");
        builder.AddAttribute(2, "aria-label", Label);
        builder.AddAttribute(3, "data-part", "commands");
        builder.AddAttribute(4, "class", "tb-ribbon__commands");
        builder.AddElementReferenceCapture(5, toolbar => _toolbar = toolbar);
        builder.AddContent(6, ChildContent);
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
            // The toolbar's script gives the row its one tab stop and its arrow keys. With no menu to move buttons
            // into, it leaves the row's content where it is.
            var module = await Modules.ImportAsync("./_content/Trailblazor/toolbar.js");
            if (_disposed)
            {
                return;
            }

            var handle = await module.InvokeAsync<IJSObjectReference?>("attachToolbar", _toolbar, null, null);
            if (handle is not null && _disposed)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the ribbon went with it.
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
