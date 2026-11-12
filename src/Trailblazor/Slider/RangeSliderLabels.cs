namespace Trailblazor;

/// <summary>The words a <see cref="TbRangeSlider{TValue}"/> says to screen readers.</summary>
public sealed record RangeSliderLabels
{
    /// <summary>Names the lower thumb, after the field's label. Leave it null for the app's text for
    /// <see cref="TbStrings.RangeSliderStart"/>, "Minimum" in English.</summary>
    public string? Start { get; init; }

    /// <summary>Names the upper thumb, after the field's label. Leave it null for the app's text for
    /// <see cref="TbStrings.RangeSliderEnd"/>, "Maximum" in English.</summary>
    public string? End { get; init; }
}
