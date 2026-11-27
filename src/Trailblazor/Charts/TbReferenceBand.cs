using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A tinted band across a <see cref="TbChart{TItem}"/> between two values of its value axis, such as a
/// normal range, with its name and values written in it. Write it inside the chart. The axis reaches both values
/// unless the chart's <see cref="TbYAxis"/> sets that end, or the chart stacks its series to shares of a whole,
/// whose axis ends at 100%.</summary>
public sealed class TbReferenceBand : TbChartReference
{
    /// <summary>The value the band starts at. Required, finite and below <see cref="To"/>. With
    /// <see cref="ChartStacking.Percent"/> both are shares from 0 to 1, as the axis's values are.</summary>
    [Parameter, EditorRequired]
    public double From { get; set; }

    /// <summary>The value the band ends at. Required.</summary>
    [Parameter, EditorRequired]
    public double To { get; set; }

    internal override (double Low, double High) Values => (From, To);

    private protected override string Needs => $"a finite {nameof(From)} below its {nameof(To)}";

    private protected override bool HasWhatItNeeds => double.IsFinite(From) && double.IsFinite(To) && From < To;
}
