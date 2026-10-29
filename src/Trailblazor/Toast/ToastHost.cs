using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Renders the toasts shown through the service. TbRoot puts one in the layout and each modal dialog has its own,
// because a modal makes everything outside it inert; only the topmost modal's host shows toasts. C#, so it can stay
// internal.
internal sealed class ToastHost : ComponentBase, IAsyncDisposable
{
    private ElementReference _element;
    private IJSObjectReference? _handle;
    private bool _disposed;

    // The modal dialog this host sits in, or null for the one in TbRoot.
    [Parameter]
    public DialogReference? Dialog { get; set; }

    [Inject]
    private ToastService Toasts { get; set; } = default!;

    [Inject]
    private DialogService Dialogs { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    private bool IsActive => Dialogs.TopmostModal == Dialog;

    protected override void OnInitialized()
    {
        Toasts.Changed += OnChanged;
        Dialogs.Changed += OnChanged;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // A manual popover sits in the top layer, above the page and any dialog opened before it.
        builder.OpenElement(0, "section");
        builder.AddAttribute(1, "popover", "manual");
        builder.AddAttribute(2, "aria-label", "Notifications");
        builder.AddAttribute(3, "aria-keyshortcuts", "F8");
        builder.AddAttribute(4, "data-part", "toaster");
        builder.AddAttribute(5, "class",
            ClassNames.Join("tb-toaster", ClassNames.Modifier("tb-toaster", Toasts.Settings.Position)));
        builder.AddElementReferenceCapture(6, element => _element = element);
        builder.OpenElement(7, "ol");
        builder.AddAttribute(8, "class", "tb-toaster__list");
        foreach (var toast in IsActive ? Toasts.Toasts.Take(Toasts.Settings.MaxVisible) : [])
        {
            builder.OpenComponent<ToastFrame>(9);
            builder.SetKey(toast);
            builder.AddComponentParameter(10, nameof(ToastFrame.Toast), toast);
            builder.CloseComponent();
        }

        builder.CloseElement();
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                var module = await Modules.ImportAsync("./_content/Trailblazor/toaster.js");
                if (_disposed)
                {
                    return;
                }

                var handle = await module.InvokeAsync<IJSObjectReference?>("attachToaster", _element);
                if (handle is null)
                {
                    return;
                }

                if (_disposed)
                {
                    await handle.DisposeHandleAsync();
                    return;
                }

                _handle = handle;
            }

            if (_handle is not null)
            {
                await _handle.InvokeVoidAsync("sync");
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the toaster went with it.
        }
    }

    private void OnChanged() => InvokeAsync(StateHasChanged);

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        Toasts.Changed -= OnChanged;
        Dialogs.Changed -= OnChanged;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }
    }
}
