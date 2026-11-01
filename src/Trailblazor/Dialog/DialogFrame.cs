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
    private bool _blocking;
    private DialogPosition _position;
    private DialogSize _size;
    private bool _resized;
    private bool _disposed;

    [Parameter, EditorRequired]
    public DialogReference Dialog { get; set; } = default!;

    // The content of a TbDialog written in markup, which renders it where it is, and its attributes for the dialog.
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    private string TitleId => $"{_id}-title";

    // A modal can't be moved or resized: it already blocks the page, so there's nothing to uncover.
    private bool Draggable => Dialog.Options.Draggable && !Dialog.IsBlocking;

    private bool Resizable => Dialog.Options.Resizable && !Dialog.IsBlocking;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var options = Dialog.Options;
        builder.OpenElement(0, "dialog");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(3, "data-part", "dialog");
        builder.AddAttribute(4, "aria-labelledby", TitleId);
        builder.AddAttribute(5, "tabindex", "-1");
        builder.AddAttribute(6, "style", string.IsNullOrWhiteSpace(options.Width) ? Style
            : Styles.Join($"--tb-dialog-width: {options.Width}", Style));
        builder.AddAttribute(7, "class", ClassNames.Join("tb-dialog", SizeClass(Dialog.Size),
            PositionClass(Dialog.Position),
            Draggable ? "tb-dialog--draggable" : null, Resizable ? "tb-dialog--resizable" : null, Class));
        builder.AddAttribute(8, "role", Dialog.IsAlert ? "alertdialog" : null);
        builder.AddAttribute(9, "aria-describedby", Dialog.IsAlert || Dialog.Described ? Dialog.DescriptionId : null);
        builder.AddElementReferenceCapture(10, element => _element = element);
        builder.OpenComponent<DialogHeader>(11);
        builder.AddComponentParameter(12, nameof(DialogHeader.Dialog), Dialog);
        builder.AddComponentParameter(13, nameof(DialogHeader.TitleId), TitleId);
        builder.CloseComponent();

        builder.OpenElement(30, "div");
        builder.AddAttribute(31, "data-part", "body");
        builder.AddAttribute(32, "class", "tb-dialog__body");
        builder.OpenComponent<CascadingValue<DialogReference>>(33);
        builder.AddComponentParameter(34, "Value", Dialog);
        builder.AddComponentParameter(35, "IsFixed", true);
        builder.AddComponentParameter(36, "ChildContent", Dialog.ContentType is null ? ChildContent : (RenderFragment)(
            content =>
            {
                content.OpenComponent<DialogContent>(0);
                content.AddComponentParameter(1, nameof(DialogContent.Dialog), Dialog);
                content.CloseComponent();
            }));
        builder.CloseComponent();
        builder.CloseElement();
        if (Dialog.IsBlocking)
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
                // The dialog may have changed while the script loaded, and a new size replaces a custom width.
                await ApplyStateAsync();
                if (_resized)
                {
                    await handle.InvokeVoidAsync("resizeTo", SizeClass(_size), SizeClass(_size));
                }
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
        _blocking = Dialog.Options.Modal;
        _position = Dialog.Position;
        _size = Dialog.Size;
        Dialog.Changed += OnChanged;
        Dialog.MoveRequested += OnMoveRequested;
        Dialog.ResizeRequested += OnResizeRequested;
    }

    // The browser closed the dialog by itself, so there is nothing left to ask.
    [JSInvokable]
    public Task Cancel() => Dialog.CancelAsync();

    // Called from the script, so a failing CanDismiss goes to the renderer instead of the browser's console.
    [JSInvokable]
    public async Task Dismiss(DialogDismissReason reason)
    {
        try
        {
            await Dialog.DismissAsync(reason);
        }
        catch (Exception exception)
        {
            await DispatchExceptionAsync(exception);
        }
    }

    private void OnChanged() => InvokeAsync(() =>
    {
        StateHasChanged();
        return ApplyStateAsync();
    });

    // The script opens the dialog modal or not and places it in the tray; the markup can't.
    private async Task ApplyStateAsync()
    {
        if (_handle is null || (Dialog.WindowState == _windowState && Dialog.IsBlocking == _blocking))
        {
            return;
        }

        var turnsModal = Dialog.IsBlocking && !_blocking;
        _windowState = Dialog.WindowState;
        _blocking = Dialog.IsBlocking;
        try
        {
            // Turning modal waits for a frame, which a hidden tab doesn't draw, so it has no time limit.
            await _handle.InvokeVoidAsync("setState", Timeout.InfiniteTimeSpan,
                [_windowState.ToString().ToLowerInvariant(), _blocking]);
            if (turnsModal)
            {
                _announcer?.Show();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the dialog went with it.
        }
    }

    // The class to replace is the one set last, so it moves on before the call, ready for a choice made meanwhile. The
    // markup renders the new class too, for a change made before the script is ready.
    private void OnMoveRequested(DialogPosition position)
    {
        var from = PositionClass(_position);
        _position = position;
        InvokeAsync(() =>
        {
            StateHasChanged();
            return CallAsync("moveTo", from, PositionClass(position));
        });
    }

    private void OnResizeRequested(DialogSize size)
    {
        var from = SizeClass(_size);
        _size = size;
        _resized = true;
        InvokeAsync(() =>
        {
            StateHasChanged();
            return CallAsync("resizeTo", from, SizeClass(size));
        });
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
