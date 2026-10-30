using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
    private DialogWindowState _windowState;
    private DialogPosition _position;
    private DialogSize _size;
    private bool _disposed;

    [Parameter, EditorRequired]
    public DialogReference Dialog { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    private string TitleId => $"{_id}-title";

    // A modal can't be moved or resized: it already blocks the page, so there's nothing to uncover.
    private bool Draggable => Dialog.Options.Draggable && !Dialog.Options.Modal;

    private bool Resizable => Dialog.Options.Resizable && !Dialog.Options.Modal;

    // The frame's markup never changes once shown; re-rendering it would push the dialog's original parameters back
    // in whenever another dialog opens or closes.
    protected override bool ShouldRender() => false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var options = Dialog.Options;
        builder.OpenElement(0, "dialog");
        builder.AddAttribute(1, "popover", options.Modal ? null : "manual");
        builder.AddAttribute(2, "data-part", "dialog");
        builder.AddAttribute(3, "aria-labelledby", TitleId);
        builder.AddAttribute(5, "tabindex", "-1");
        builder.AddAttribute(6, "class", ClassNames.Join("tb-dialog", SizeClass(options.Size),
            PositionClass(options.Position),
            Draggable ? "tb-dialog--draggable" : null, Resizable ? "tb-dialog--resizable" : null));
        builder.AddAttribute(7, "role", Dialog.IsAlert ? "alertdialog" : null);
        builder.AddAttribute(8, "aria-describedby", Dialog.IsAlert ? Dialog.DescriptionId : null);
        builder.AddElementReferenceCapture(9, element => _element = element);
        builder.OpenComponent<DialogHeader>(10);
        builder.AddComponentParameter(11, nameof(DialogHeader.Dialog), Dialog);
        builder.AddComponentParameter(12, nameof(DialogHeader.TitleId), TitleId);
        builder.CloseComponent();

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

        if (Resizable)
        {
            DialogMenus.AddResize(builder, 42, Dialog, Localizer);
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

    protected override void OnInitialized()
    {
        _position = Dialog.Options.Position;
        _size = Dialog.Options.Size;
        Dialog.Changed += OnChanged;
        Dialog.MoveRequested += OnMoveRequested;
        Dialog.ResizeRequested += OnResizeRequested;
    }

    [JSInvokable]
    public Task Cancel() => Dialog.CloseAsync(false, null);

    private void OnChanged() => InvokeAsync(ApplyWindowStateAsync);

    // The frame never re-renders, so a new window state reaches the dialog through the script.
    private async Task ApplyWindowStateAsync()
    {
        if (_handle is null || Dialog.WindowState == _windowState)
        {
            return;
        }

        _windowState = Dialog.WindowState;
        try
        {
            await _handle.InvokeVoidAsync("setWindowState", _windowState.ToString().ToLowerInvariant());
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the dialog went with it.
        }
    }

    // The class to replace is the one set last, so it moves on before the call, ready for a choice made meanwhile.
    private void OnMoveRequested(DialogPosition position)
    {
        var from = PositionClass(_position);
        _position = position;
        InvokeAsync(() => CallAsync("moveTo", from, PositionClass(position)));
    }

    private void OnResizeRequested(DialogSize size)
    {
        var from = SizeClass(_size);
        _size = size;
        InvokeAsync(() => CallAsync("resizeTo", from, SizeClass(size)));
    }

    private async Task CallAsync(string method, string from, string to)
    {
        if (_handle is null)
        {
            return;
        }

        try
        {
            await _handle.InvokeVoidAsync(method, from, to);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the dialog went with it.
        }
    }

    private static string PositionClass(DialogPosition position) =>
        ClassNames.Modifier("tb-dialog", "position", position);

    private static string SizeClass(DialogSize size) => ClassNames.Modifier("tb-dialog", size);

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
        Dialog.Changed -= OnChanged;
        Dialog.MoveRequested -= OnMoveRequested;
        Dialog.ResizeRequested -= OnResizeRequested;
        Dialog.CloseInBrowserAsync = null;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _self?.Dispose();
    }
}
