using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Shows images one at a time over the whole window, such as a gallery's photos at full size, with zoom,
/// rotation and previous and next buttons. It's a modal dialog: focus stays inside it, and Escape or its close button
/// closes it, giving focus back to what opened it. Open it from a thumbnail with <c>@bind-Open</c> and
/// <c>@bind-Index</c>.</summary>
public partial class TbLightbox
{
    // Zoom steps, as multiples of the size that fits the image to the window.
    private static readonly double[] Zooms = [1, 1.5, 2, 3, 4];
    private static readonly Icon PreviousIcon = BuiltInIcons.ChevronStart with { MirrorsInRightToLeft = true };
    private static readonly Icon NextIcon = BuiltInIcons.ChevronEnd with { MirrorsInRightToLeft = true };

    private int _current;
    private int _zoom;
    // Quarter turns clockwise, from 0 to 3.
    private int _turns;
    private bool _wasOpen;

    /// <summary>The images, in order. Each needs alt text.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<LightboxImage> Images { get; set; } = [];

    /// <summary>Whether the lightbox is open. Bind it with <c>@bind-Open</c>: it turns false when the user closes it.
    /// </summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised with false when the user closes the lightbox.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>The shown image's position in <see cref="Images"/>, from 0. Bind it with <c>@bind-Index</c>, and set
    /// it to the thumbnail's position before opening the lightbox.</summary>
    [Parameter]
    public int Index { get; set; }

    /// <summary>Raised when another image is shown.</summary>
    [Parameter]
    public EventCallback<int> IndexChanged { get; set; }

    /// <summary>The lightbox's title, such as "Trail photos", shown in its header and naming it for screen readers.
    /// Required.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>Moves from the last image to the first, and back, instead of stopping at the ends. On by default.
    /// </summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>The words the lightbox shows and says to screen readers, in place of the app's text.</summary>
    [Parameter]
    public LightboxLabels Labels { get; set; } = new();

    /// <summary>A label the lightbox's use is counted under, such as <c>"trail-photos"</c>, through the
    /// <c>Trailblazor</c> meter, as for a dialog: shown each time it opens, and counted with how it closed. It names
    /// the lightbox, never the user.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    private int Current => Images.Count == 0 ? 0 : Math.Clamp(_current, 0, Images.Count - 1);

    private LightboxImage? Shown => Images.Count == 0 ? null : Images[Current];

    private bool CanGoBack => Loop || Current > 0;

    private bool CanGoOn => Loop || Current < Images.Count - 1;

    private double Zoom => Zooms[_zoom];

    private DialogOptions LightboxOptions => new()
    {
        Size = DialogSize.Full,
        InitialFocus = DialogFocus.Dialog,
        CloseButton = true,
        TelemetryName = TelemetryName,
    };

    private string Text(string? given, string key) => given ?? Localizer[key];

    private static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);

    private string Position => Format(Text(Labels.Position, TbStrings.LightboxPosition), Current + 1, Images.Count);

    private string ShownText => Format(Text(Labels.Shown, TbStrings.LightboxShown), Shown?.Alt ?? "", Current + 1,
        Images.Count);

    private string ZoomText => Format(Text(Labels.Zoom, TbStrings.LightboxZoom), Math.Round(Zoom * 100));

    private string StageStyle =>
        FormattableString.Invariant($"--_tb-lightbox-zoom: {Zoom}; --_tb-lightbox-turn: {_turns * 90}deg");

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException($"{nameof(TbLightbox)} needs a {nameof(Title)}.");
        }

        if (Images.Any(image => string.IsNullOrWhiteSpace(image.Alt)))
        {
            throw new InvalidOperationException($"Each {nameof(LightboxImage)} in a {nameof(TbLightbox)} needs "
                + $"{nameof(LightboxImage.Alt)} text.");
        }

        // A new Index from the parent, or opening again, shows the image as it first fits the window.
        if (Index != _current)
        {
            _current = Index;
            ResetView();
        }

        if (Open && !_wasOpen)
        {
            ResetView();
        }

        _wasOpen = Open;
    }

    private void ResetView() => (_zoom, _turns) = (0, 0);

    private async Task GoToAsync(int index)
    {
        _current = (index + Images.Count) % Images.Count;
        ResetView();
        await IndexChanged.InvokeAsync(_current);
    }

    private void ZoomBy(int steps) => _zoom = Math.Clamp(_zoom + steps, 0, Zooms.Length - 1);

    // A double click zooms in to twice the fitted size, or back out to fit.
    private void ToggleZoom() => _zoom = _zoom == 0 ? Array.IndexOf(Zooms, 2d) : 0;

    private void Turn(int quarters) => _turns = (_turns + quarters + 4) % 4;
}
