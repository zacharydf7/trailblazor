namespace Trailblazor;

/// <summary>The two ends a <see cref="TbRangeSlider{TValue}"/> chooses.</summary>
/// <param name="Start">The lower end.</param>
/// <param name="End">The upper end.</param>
public readonly record struct SliderRange<TValue>(TValue Start, TValue End);
