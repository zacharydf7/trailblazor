using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// One open dialog: the native <dialog> with its header and the dialog's own content. C#, so it can stay internal.
internal sealed class DialogFrame : ComponentBase, IAsyncDisposable
{
    private readonly string _id = $"tb-dialog-{Guid.NewGuid():N}";
    private ElementReference _element;
    private DotNetObjectReference<DialogFrame>? _self;
    private IJSObjectReference? _handle;
    private AnnouncerHost? _announcer;
    private bool _disposed;

    [Parameter, EditorRequired]
    public DialogReference Dialog { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    private string TitleId => $"{_id}-title";

    // The frame's markup never changes once shown; re-rendering it would push the dialog's original parameters back
    // in whenever another dialog opens or closes.
    protected override bool ShouldRender() => false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var options = Dialog.Options;
        builder.OpenElement(0, "dialog");
        builder.AddAttribute(1, "popover", options.Modal ? null : "manual");
        builder.AddAttribute(2, "data-part", "dialog");
        builder.AddAttribute(3, "aria-labelledby", options.ShowHeader ? TitleId : null);
        builder.AddAttribute(4, "aria-label", options.ShowHeader ? null : Dialog.Title);
        builder.AddAttribute(5, "tabindex", "-1");
        builder.AddAttribute(6, "class", ClassNames.Join("tb-dialog", ClassNames.Modifier("tb-dialog", options.Size)));
        builder.AddAttribute(7, "role", Dialog.IsAlert ? "alertdialog" : null);
        builder.AddAttribute(8, "aria-describedby", Dialog.IsAlert ? Dialog.DescriptionId : null);
        builder.AddElementReferenceCapture(9, element => _element = element);
        if (options.ShowHeader)
        {
            builder.OpenElement(10, "header");
            builder.AddAttribute(11, "data-part", "header");
            builder.AddAttribute(12, "class", "tb-dialog__header");
            builder.OpenElement(13, "h2");
            builder.AddAttribute(14, "id", TitleId);
            builder.AddAttribute(15, "class", "tb-dialog__title");
            builder.AddContent(16, Dialog.Title);
            builder.CloseElement();
            if (options.CloseButton)
            {
                builder.OpenComponent<TbIconButton>(17);
                builder.AddComponentParameter(18, nameof(TbIconButton.Icon), BuiltInIcons.Close);
                builder.AddComponentParameter(19, nameof(TbIconButton.AriaLabel), "Close");
                builder.AddComponentParameter(20, nameof(TbIconButton.Size), Size.Small);
                builder.AddComponentParameter(21, nameof(TbIconButton.OnClick),
                    EventCallback.Factory.Create<MouseEventArgs>(this, Cancel));
                builder.AddAttribute(22, "data-part", "close");
                builder.CloseComponent();
            }

            builder.CloseElement();
        }

        builder.OpenElement(30, "div");
        builder.AddAttribute(31, "data-part", "body");
        builder.AddAttribute(32, "class", "tb-dialog__body");
        builder.OpenComponent<CascadingValue<DialogReference>>(33);
        builder.AddComponentParameter(34, "Value", Dialog);
        builder.AddComponentParameter(35, "IsFixed", true);
        builder.AddComponentParameter(36, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<DynamicComponent>(0);
            content.AddComponentParameter(1, nameof(DynamicComponent.Type), Dialog.Component);
            content.AddComponentParameter(2, nameof(DynamicComponent.Parameters),
                Dialog.Parameters.ToDictionary(pair => pair.Key, pair => pair.Value!));
            content.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
        if (options.Modal)
        {
            builder.OpenComponent<ToastHost>(37);
            builder.AddComponentParameter(38, nameof(ToastHost.Dialog), Dialog);
            builder.CloseComponent();
            builder.OpenComponent<AnnouncerHost>(39);
            builder.AddComponentParameter(40, nameof(AnnouncerHost.Dialog), Dialog);
            builder.AddComponentReferenceCapture(41, host => _announcer = (AnnouncerHost)host);
            builder.CloseComponent();
        }

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
            var module = await Modules.ImportAsync("./_content/Trailblazor/dialog.js");
            if (_disposed)
            {
                return;
            }

            _self = DotNetObjectReference.Create(this);
            var handle = await module.InvokeAsync<IJSObjectReference?>("openDialog", _element, _self, Dialog.Options);
            if (handle is not null)
            {
                if (_disposed)
                {
                    await handle.DisposeHandleAsync();
                    return;
                }

                _handle = handle;
                Dialog.CloseInBrowserAsync = CloseInBrowserAsync;
            }

            // The open dialog joins the accessibility tree with the next frame, so its live regions wait for that.
            if (_announcer is not null)
            {
                // A hidden tab draws no frames, so this waits as long as it takes rather than timing out.
                await module.InvokeVoidAsync("nextFrame", Timeout.InfiniteTimeSpan);
                _announcer.Show();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the dialog went with it.
        }
    }

    [JSInvokable]
    public Task Cancel() => Dialog.CloseAsync(false, null);

    private async Task CloseInBrowserAsync()
    {
        try
        {
            await _handle!.InvokeVoidAsync("close");
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the dialog went with it.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        Dialog.CloseInBrowserAsync = null;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _self?.Dispose();
    }
}
