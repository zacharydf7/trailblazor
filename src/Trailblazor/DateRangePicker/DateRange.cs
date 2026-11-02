namespace Trailblazor;

/// <summary>The two dates a <see cref="TbDateRangePicker{TDate}"/> chooses. Either can be missing, and the default
/// range has neither.</summary>
/// <param name="Start">The first day of the range.</param>
/// <param name="End">The last day of the range, inclusive.</param>
public readonly record struct DateRange<TDate>(TDate? Start, TDate? End) where TDate : struct;
