using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A box that pans, zooms and scrolls its content, such as an image, a diagram or a page, with its buttons,
/// the keys, Ctrl and the wheel, a drag or a pinch. It can also fit the content to the box's width, or fit it whole.
/// </summary>
public sealed partial class TbViewport : TbComponentBase, IAsyncDisposable
{
    private ElementReference _root;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<ViewportReport>? _report;
    private ControlTelemetry? _telemetry;
    private double _zoom = 1;
    private ViewportFit _fit;
    // The parameters last seen, so a zoom or fit the parent didn't change stays as the user left it.
    private double? _zoomFromParent;
    private ViewportFit? _fitFromParent;
    private object? _limits;
    private bool _sendAfterRender;
    // Counts the zooms and fits the controls and the parent apply, so a fit the script answers after a later zoom
    // doesn't undo it.
    private int _applies;
    private bool _disposed;

    /// <summary>The content to pan and zoom, at its own size; the zoom scales it from there.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The box's accessible name, such as "Trail map". Required.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>The zoom, where 1 shows the content at its own size. Bind it with <c>@bind-Zoom</c> to set it from
    /// code and hear when the user, or a <see cref="Fit"/>, changes it.</summary>
    [Parameter]
    public double Zoom { get; set; } = 1;

    /// <summary>Raised when the zoom changes, by the user or by a <see cref="Fit"/> as the box or content resizes.
    /// </summary>
    [Parameter]
    public EventCallback<double> ZoomChanged { get; set; }

    /// <summary>Sets the zoom from the box's size and the content's, again whenever either resizes, until the user
    /// zooms. Bind it with <c>@bind-Fit</c> to hear when the user picks a fit or zooms away from one.</summary>
    [Parameter]
    public ViewportFit Fit { get; set; }

    /// <summary>Raised when the user picks a fit from the zoom menu, or zooms and so leaves one.</summary>
    [Parameter]
    public EventCallback<ViewportFit> FitChanged { get; set; }

    /// <summary>The smallest zoom, above 0. Defaults to 0.1, or 10%.</summary>
    [Parameter]
    public double MinZoom { get; set; } = 0.1;

    /// <summary>The largest zoom, at least <see cref="MinZoom"/>. Defaults to 8, or 800%.</summary>
    [Parameter]
    public double MaxZoom { get; set; } = 8;

    /// <summary>What each zoom in multiplies the zoom by, and each zoom out divides it by; above 1. Defaults to 1.25.
    /// </summary>
    [Parameter]
    public double ZoomStep { get; set; } = 1.25;

    /// <summary>The box's height, such as <c>"30rem"</c>. Defaults to 24rem.</summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>Shows the zoom out and zoom in buttons and the zoom menu above the box. On by default; without them,
    /// the keys, the wheel and a pinch still zoom.</summary>
    [Parameter]
    public bool ShowControls { get; set; } = true;

    /// <summary>Lets a drag with the mouse, pen or a finger pan the content, and two fingers pinch to zoom it. On by
    /// default; turn it off for content whose text people select, and the box scrolls as any other does.</summary>
    [Parameter]
    public bool DragToPan { get; set; } = true;

    /// <summary>The words the controls show and say to screen readers. Each one left null uses the app's text.
    /// </summary>
    [Parameter]
    public ViewportLabels Labels { get; set; } = new();

    /// <summary>A label the viewport's use is counted under, such as <c>"trail-map"</c>, through the
    /// <c>Trailblazor</c> meter: shown when it first renders, and used with the action <c>zoom</c> each time the user
    /// zooms, with the input <c>keyboard</c> or <c>pointer</c>, and <c>fit</c> each time they pick a fit. Panning isn't
    /// counted, and the zoom itself is never recorded.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IAnnouncer Announcer { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    private string Percent => _zoom.ToString("P0", CultureInfo.CurrentCulture);

    private string ZoomLabel => string.Format(CultureInfo.CurrentCulture, Labels.Zoom ?? Localizer[TbStrings.ViewportZoom],
        Percent);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException($"{nameof(TbViewport)} needs a {nameof(Label)}, which names it for "
                + "screen readers.");
        }

        if (MinZoom <= 0 || MaxZoom < MinZoom || ZoomStep <= 1)
        {
            throw new InvalidOperationException($"{nameof(TbViewport)} needs a {nameof(MinZoom)} above 0, a "
                + $"{nameof(MaxZoom)} at least as large and a {nameof(ZoomStep)} above 1.");
        }

        if (Zoom != _zoomFromParent)
        {
            _zoomFromParent = Zoom;
            _sendAfterRender |= Clamp(Zoom) != _zoom;
            _zoom = Clamp(Zoom);
        }

        if (Fit != _fitFromParent)
        {
            _fitFromParent = Fit;
            _sendAfterRender |= Fit != _fit;
            _fit = Fit;
        }

        object limits = (MinZoom, MaxZoom, ZoomStep, DragToPan);
        if (!limits.Equals(_limits))
        {
            _sendAfterRender |= _limits is not null;
            _limits = limits;
            _zoom = Clamp(_zoom);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Telemetry.Rendered(TelemetryName);
        try
        {
            if (firstRender)
            {
                await AttachAsync();
            }
            else if (_sendAfterRender)
            {
                await SendAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the viewport went with it.
        }
    }

    private async Task AttachAsync()
    {
        var module = await Modules.ImportAsync("./_content/Trailblazor/viewport.js");
        if (_disposed)
        {
            return;
        }

        _report = DotNetObjectReference.Create(new ViewportReport(OnZoomAsync));
        var handle = await module.InvokeAsync<IJSObjectReference?>("attachViewport", _root, _report);
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
        await SendAsync();
    }

    // The script applies the zoom, or works out a fit's, and the zoom it settles on is the viewport's.
    private async Task SendAsync()
    {
        _sendAfterRender = false;
        if (_handle is null)
        {
            return;
        }

        var apply = ++_applies;
        var before = (_zoom, _fit);
        var zoom = await UpdateAsync(_zoom, _fit);
        if (apply != _applies)
        {
            return;
        }

        _zoom = zoom;
        await RaiseChangesAsync(before);
        StateHasChanged();
    }

    private ValueTask<double> UpdateAsync(double zoom, ViewportFit fit) =>
        _handle!.InvokeAsync<double>("update", zoom, fit.ToString(), MinZoom, MaxZoom, ZoomStep, DragToPan);

    private Task ZoomInAsync(MouseEventArgs args) => ZoomToAsync(_zoom * ZoomStep, InputOf(args));

    private Task ZoomOutAsync(MouseEventArgs args) => ZoomToAsync(_zoom / ZoomStep, InputOf(args));

    private Task ActualSizeAsync(MouseEventArgs args) => ZoomToAsync(1, InputOf(args));

    // A click a key makes has no detail, so the zoom says which input made it.
    private static string InputOf(MouseEventArgs args) => args.Detail > 0 ? "pointer" : "keyboard";

    private Task ZoomToAsync(double zoom, string? input)
    {
        Telemetry.Used(TelemetryName, "zoom", input);
        return ApplyAsync(Clamp(zoom), ViewportFit.None);
    }

    private Task FitAsync(ViewportFit fit)
    {
        Telemetry.Used(TelemetryName, "fit");
        return ApplyAsync(_zoom, fit);
    }

    // Before the script is ready, a zoom applies through the content's style and a fit waits for the script. The script
    // works out a fit's zoom, so both change together once it answers; any other zoom is stored and raised before the
    // script call, so a quick second click steps from the new zoom.
    private async Task ApplyAsync(double zoom, ViewportFit fit)
    {
        var apply = ++_applies;
        var before = (_zoom, _fit);
        if (fit != ViewportFit.None && _handle is not null)
        {
            zoom = await UpdateAsync(zoom, fit);
            if (apply != _applies)
            {
                return;
            }
        }

        (_zoom, _fit) = (zoom, fit);
        Announcer.Polite(ZoomLabel);
        await RaiseChangesAsync(before);
        if (fit == ViewportFit.None && _handle is not null)
        {
            await UpdateAsync(zoom, fit);
        }
    }

    // The keys, the wheel and a pinch zoom in the browser, which reports each zoom, and so does a fit as the box or
    // the content resizes. Only a zoom from the keys is read out, since a pointer's shows where the user looks.
    private async Task OnZoomAsync(double zoom, string fit, string input)
    {
        var before = (_zoom, _fit);
        _zoom = zoom;
        _fit = Enum.Parse<ViewportFit>(fit);
        if (input != "resize")
        {
            Telemetry.Used(TelemetryName, "zoom", input);
        }

        if (input == "keyboard")
        {
            Announcer.Polite(ZoomLabel);
        }

        await RaiseChangesAsync(before);
        StateHasChanged();
    }

    private async Task RaiseChangesAsync((double Zoom, ViewportFit Fit) before)
    {
        if (_zoom != before.Zoom)
        {
            await ZoomChanged.InvokeAsync(_zoom);
        }

        if (_fit != before.Fit)
        {
            await FitChanged.InvokeAsync(_fit);
        }
    }

    private double Clamp(double zoom) => Math.Clamp(zoom, MinZoom, MaxZoom);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _report?.Dispose();
    }
}
