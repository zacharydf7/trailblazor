using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A one-dimensional barcode drawn from a value, such as a ticket number, as an SVG that scales to any size.
/// Screen readers hear the value, or <see cref="AriaLabel"/>.</summary>
public partial class TbBarcode
{
    // Whole pixels for each module by default, so every bar of the same width draws at the same width.
    private const int ModulePixels = 2;
    private string _viewBox = "";
    private string _path = "";
    private string _text = "";
    private string _width = "";
    private (string Value, BarcodeFormat Format)? _drawn;

    /// <summary>The value the barcode holds. Required. <see cref="BarcodeFormat.Code128"/> takes printable ASCII text;
    /// <see cref="BarcodeFormat.Ean13"/> takes 12 digits and adds the check digit, or 13 with a correct check digit. A
    /// value the format can't hold throws; check one from users or data first with <see cref="CanEncode"/>.</summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = "";

    /// <summary>The kind of barcode. <see cref="BarcodeFormat.Code128"/> by default.</summary>
    [Parameter]
    public BarcodeFormat Format { get; set; } = BarcodeFormat.Code128;

    /// <summary>Prints the value under the bars. On by default.</summary>
    [Parameter]
    public bool ShowText { get; set; } = true;

    /// <summary>The barcode's width, as a CSS length. Without it, each module is 2 pixels, so the bars stay sharp
    /// however long the value is; a width that doesn't give each module whole pixels draws equal bars at unequal
    /// widths, which some scanners misread.</summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>The bars' height, as a CSS length. 4rem by default.</summary>
    [Parameter]
    public string Height { get; set; } = "4rem";

    /// <summary>Names the barcode for screen readers in place of <see cref="Value"/>, such as "Ticket 1042".</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Whether a barcode of this format can hold this value.</summary>
    public static bool CanEncode(string value, BarcodeFormat format = BarcodeFormat.Code128) =>
        BarcodeEncoder.Problem(value, format) is null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(Value))
        {
            throw new InvalidOperationException($"{nameof(TbBarcode)} needs a {nameof(Value)}.");
        }

        if (_drawn != (Value, Format))
        {
            // The light margins a scanner needs: ten modules on each side, or EAN-13's eleven before and seven after.
            var (before, after) = Format == BarcodeFormat.Ean13 ? (11, 7) : (10, 10);
            var bars = BarcodeEncoder.Encode(Value, Format);
            var width = before + bars.Length + after;
            _viewBox = $"0 0 {width} 1";
            _width = (width * ModulePixels).ToString(CultureInfo.InvariantCulture) + "px";
            var modules = new bool[1, bars.Length];
            for (var at = 0; at < bars.Length; at++)
            {
                modules[0, at] = bars[at];
            }

            _path = BarPath.Rows(modules, before, quietRows: 0);
            _text = Format == BarcodeFormat.Ean13 && Value.Length == 12
                ? Value + BarcodeEncoder.Ean13Check(Value).ToString(CultureInfo.InvariantCulture) : Value;
            _drawn = (Value, Format);
        }
    }
}
