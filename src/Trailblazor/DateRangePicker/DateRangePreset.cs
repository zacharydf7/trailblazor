namespace Trailblazor;

/// <summary>A ready-made range a <see cref="TbDateRangePicker{TDate}"/> offers beside its calendar, such as
/// "Last 7 days".</summary>
/// <param name="Label">The preset's button text.</param>
/// <param name="Range">The range it chooses.</param>
public sealed record DateRangePreset<TDate>(string Label, DateRange<TDate> Range) where TDate : struct;
