namespace Trailblazor;

/// <summary>The words a <see cref="TbGauge"/> shows and says to screen readers. The gauge's own <c>Label</c> or
/// <c>AriaLabel</c> names it.</summary>
public sealed record GaugeLabels
{
    /// <summary>Shown in the dial, and read out, while the gauge has no value, such as "Offline". Leave it null for
    /// the app's text for <see cref="TbStrings.GaugeNoValue"/>.</summary>
    public string? NoValue { get; init; }

    /// <summary>What screen readers hear for a value in a named range, with <c>{0}</c> for the value's text and
    /// <c>{1}</c> for the range's name. Leave it null for the app's text for
    /// <see cref="TbStrings.GaugeValueInRange"/>.</summary>
    public string? ValueInRange { get; init; }
}
