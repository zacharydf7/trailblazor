namespace Trailblazor;

/// <summary>Which kind of barcode a <see cref="TbBarcode"/> draws.</summary>
public enum BarcodeFormat
{
    /// <summary>Code 128, for any printable ASCII text, such as an order or ticket number. The default.</summary>
    Code128,

    /// <summary>EAN-13, the 13-digit retail product number. Give 12 digits for the check digit to be added, or all 13
    /// for it to be checked.</summary>
    Ean13,
}
