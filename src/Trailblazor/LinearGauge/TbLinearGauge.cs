using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A bar that shows where one value is between two ends, such as how much of a budget is spent, with the
/// value as text above it and an optional <see cref="Target"/> marked across it. <see cref="TbGaugeRange"/> children
/// mark ranges of its values, such as "High", as bands under it. It is a <c>meter</c>, drawn as markup with no
/// script. For how far along a task is, use a <see cref="TbProgress"/>.</summary>
public partial class TbLinearGauge
{
    /// <summary>A value to reach or to stay under, such as a quota, drawn as a mark across the bar and read out
    /// after the value. One beyond an end is drawn at that end, and one that isn't finite is left out.</summary>
    [Parameter]
    public double? Target { get; set; }

    // The target, when it is a number the bar can place.
    private double? Goal => Target is { } target && double.IsFinite(target) ? target : null;

    // What screen readers hear: the value, the range it is in and, after them, the target.
    private string Spoken(Reading reading) => Goal is not { } goal ? reading.Spoken : string.Format(
        CultureInfo.CurrentCulture, Labels.ValueWithTarget ?? Localizer[TbStrings.GaugeValueWithTarget],
        reading.Spoken, Text(goal));

    // A share of the bar as a number of hundredths, for the styles.
    private string Percent(double value) => Number(Share(Math.Clamp(value, Min, Max)) * 100);
}
