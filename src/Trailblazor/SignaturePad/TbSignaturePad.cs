using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Internal.Events;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A pad people sign by drawing with a mouse, pen or finger, with Undo and Clear buttons. They can type their
/// name instead, which is how to sign from the keyboard or with a screen reader. Its value is a <see cref="Signature"/>,
/// which <see cref="Signature.ToSvg"/> turns into an image, or null before anything is drawn or typed.</summary>
public partial class TbSignaturePad
{
    private ElementReference _surface;
    private ElementReference _typedInput;
    private TbButton? _modeButton;
    private IJSObjectReference? _handle;
    private bool _typing;
    private bool _attaching;
    private bool _disposed;

    /// <summary>The words the pad shows and reads out, in place of the app's own text.</summary>
    [Parameter]
    public SignaturePadLabels Labels { get; set; } = new();

    private IReadOnlyList<SignatureStroke> Strokes => Value?.Strokes ?? [];

    private string SurfaceLabel => string.Format(CultureInfo.CurrentCulture, Strokes.Count == 0
        ? Labels.Blank ?? Localizer[TbStrings.SignaturePadBlank]
        : Labels.Drawn ?? Localizer[TbStrings.SignaturePadDrawn], ShownLabel ?? "");

    /// <summary>The signature as a PNG image the size of its space, drawn by the browser from
    /// <see cref="Signature.ToSvg"/>. Throws when nothing is drawn or typed.</summary>
    public async Task<byte[]> ToPngAsync()
    {
        if (Value is not { IsEmpty: false } signature)
        {
            throw new InvalidOperationException("The signature pad holds no signature to turn into an image.");
        }

        var module = await Modules.ImportAsync("./_content/Trailblazor/signature-pad.js");
        return await module.InvokeAsync<byte[]>("toPng", signature.ToSvg(), Signature.Width, Signature.Height);
    }

    /// <inheritdoc />
    public override async ValueTask FocusAsync(bool preventScroll = false)
    {
        if (_typing)
        {
            await _typedInput.FocusAsync(preventScroll);
        }
        else if (_modeButton is not null)
        {
            await _modeButton.FocusAsync(preventScroll);
        }
    }

    // Nothing drawn or typed counts as missing when a signature is required.
    private protected override bool IsMissing(Signature? value) => value is null || value.IsEmpty;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // A signature the app gives the pad shows as it was made: a typed name in the name field, strokes on the pad.
        if (Value is { IsEmpty: false } signature)
        {
            _typing = signature.Strokes.Count == 0 && !string.IsNullOrWhiteSpace(signature.TypedName);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        // Typing takes the surface away, whether the toggle or the app switched to it, so the script goes with it.
        if (_typing && _handle is not null)
        {
            var stale = _handle;
            _handle = null;
            await stale.DisposeHandleAsync();
        }

        if (_typing || _handle is not null || _attaching || _disposed || !RendererInfo.IsInteractive)
        {
            return;
        }

        // A render during the awaits below would otherwise attach the script a second time.
        _attaching = true;
        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/signature-pad.js");
            var handle = await module.InvokeAsync<IJSObjectReference?>("createSignaturePad", _surface);
            if (handle is null)
            {
                return;
            }

            if (_disposed || _typing)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the pad went with it.
        }
        finally
        {
            _attaching = false;
        }
    }

    private Task AddStrokeAsync(SignatureStrokeEventArgs args)
    {
        if (CantChange || ReadOnly || args.Points.Length < 2)
        {
            return Task.CompletedTask;
        }

        List<SignaturePoint> points = [];
        for (var at = 0; at + 1 < args.Points.Length; at += 2)
        {
            points.Add(new(Math.Clamp(args.Points[at], 0, Signature.Width), Math.Clamp(args.Points[at + 1], 0, Signature.Height)));
        }

        Telemetry.Used(TelemetryName, "draw");
        return SetValueAsync(new Signature([.. Strokes, new SignatureStroke(points)]));
    }

    private Task UndoAsync()
    {
        Telemetry.Used(TelemetryName, "undo");
        return SetValueAsync(Strokes.Count <= 1 ? null : new Signature([.. Strokes.Take(Strokes.Count - 1)]));
    }

    private Task ClearAsync()
    {
        Telemetry.Used(TelemetryName, "clear");
        return SetValueAsync(null);
    }

    // Switching between drawing and typing starts the signature over, so it's never both at once.
    private async Task ToggleTypingAsync()
    {
        _typing = !_typing;
        Telemetry.Used(TelemetryName, _typing ? "type" : "draw_instead");
        await SetValueAsync(null);
    }

    private Task TypeAsync(ChangeEventArgs args) =>
        SetValueAsync(args.Value is string { Length: > 0 } name && !string.IsNullOrWhiteSpace(name)
            ? new Signature([], name) : null);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Blazor calls only DisposeAsync when both exist, so the field's own cleanup has to run from here.
        Dispose();
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }
    }
}
