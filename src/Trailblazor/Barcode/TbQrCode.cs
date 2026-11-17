using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A QR code drawn from a value, such as a link to a trail map, as an SVG that scales to any size. Screen
/// readers hear the value, or <see cref="AriaLabel"/>.</summary>
public partial class TbQrCode
{
    // Four light modules on every side, as the standard asks, so a scanner can find the code.
    private const int QuietZone = 4;
    // Whole pixels for each module by default, so every module draws at the same size.
    private const int ModulePixels = 4;
    private string _viewBox = "";
    private string _path = "";
    private string _size = "";
    private (string Value, QrErrorCorrection Level)? _drawn;

    /// <summary>The text or link the code holds. Required. Digits alone, or upper-case letters, digits, spaces and
    /// $%*+-./:, make the smallest code; anything else is held as UTF-8. A value too long for
    /// <see cref="ErrorCorrection"/> throws; check one from users or data first with <see cref="CanEncode"/>.
    /// </summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = "";

    /// <summary>How much of the code can be damaged or covered and still scan. <see cref="QrErrorCorrection.Medium"/>
    /// by default.</summary>
    [Parameter]
    public QrErrorCorrection ErrorCorrection { get; set; } = QrErrorCorrection.Medium;

    /// <summary>The code's width and height, as a CSS length. Without it, each module is 4 pixels, so the code stays
    /// sharp however much it holds; a size that doesn't give each module whole pixels draws equal modules at unequal
    /// sizes, which some scanners misread.</summary>
    [Parameter]
    public string? Size { get; set; }

    /// <summary>Names the code for screen readers in place of <see cref="Value"/>, such as "Link to the trail map".
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Whether a QR code can hold this value at this error correction level: it isn't empty, and isn't too
    /// long.</summary>
    public static bool CanEncode(string value, QrErrorCorrection errorCorrection = QrErrorCorrection.Medium) =>
        !string.IsNullOrEmpty(value) && QrEncoder.Fits(value, errorCorrection);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(Value))
        {
            throw new InvalidOperationException($"{nameof(TbQrCode)} needs a {nameof(Value)}.");
        }

        if (_drawn != (Value, ErrorCorrection))
        {
            var modules = QrEncoder.Encode(Value, ErrorCorrection);
            var size = modules.GetLength(0) + QuietZone * 2;
            _viewBox = $"0 0 {size} {size}";
            _size = (size * ModulePixels).ToString(CultureInfo.InvariantCulture) + "px";
            _path = BarPath.Rows(modules, QuietZone);
            _drawn = (Value, ErrorCorrection);
        }
    }
}
