namespace Trailblazor;

/// <summary>How much of a <see cref="TbQrCode"/> can be damaged or covered and still scan. Higher levels take a larger
/// code for the same value.</summary>
public enum QrErrorCorrection
{
    /// <summary>About 7% can be lost.</summary>
    Low,

    /// <summary>About 15% can be lost. The default.</summary>
    Medium,

    /// <summary>About 25% can be lost.</summary>
    Quartile,

    /// <summary>About 30% can be lost, such as for a code printed with a logo over its middle.</summary>
    High,
}
