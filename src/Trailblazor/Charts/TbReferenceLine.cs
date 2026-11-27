using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A dashed line across a <see cref="TbChart{TItem}"/> at one value of its value axis, such as a target or
/// a limit, with its name and value written at its end. Write it inside the chart. The axis reaches the value unless
/// the chart's <see cref="TbYAxis"/> sets that end, or the chart stacks its series to shares of a whole, whose axis
/// ends at 100%.</summary>
public sealed class TbReferenceLine : TbChartReference
{
    /// <summary>The value the line is drawn at. Required, and finite. With <see cref="ChartStacking.Percent"/> it
    /// is a share from 0 to 1, as the axis's values are.</summary>
    [Parameter, EditorRequired]
    public double Value { get; set; }

    internal override (double Low, double High) Values => (Value, Value);

    private protected override string Needs => $"a finite {nameof(Value)}";

    private protected override bool HasWhatItNeeds => double.IsFinite(Value);
}
