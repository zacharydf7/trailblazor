using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A dashed line across a <see cref="TbChart{TItem}"/> with its name and value written beside it: at a
/// <see cref="Value"/> of its value axis, such as a target or a limit, or at an <see cref="X"/> along its items,
/// such as the day of a storm. Write it inside the chart, with one of the two. The value axis reaches a
/// <see cref="Value"/> unless the chart's <see cref="TbYAxis"/> sets that end, or the chart stacks its series to
/// shares of a whole, whose axis ends at 100%.</summary>
public sealed class TbReferenceLine : TbChartReference
{
    /// <summary>The value of the value axis the line is drawn at, which has to be finite. With
    /// <see cref="ChartStacking.Percent"/> it is a share from 0 to 1, as the axis's values are.</summary>
    [Parameter]
    public double? Value { get; set; }

    /// <summary>The place along the chart's items the line is drawn at, in place of a <see cref="Value"/>. On a
    /// continuous scale it is any date or finite number, of the kind the items' <c>X</c> values are. On an even
    /// scale it is one item's <c>X</c> value, where numbers of any type match by value and other values match only
    /// one of the same type, and the line runs through the middle of that item's place.</summary>
    [Parameter]
    public object? X { get; set; }

    internal override (double Low, double High)? Values => Value is { } value ? (value, value) : null;

    internal override (object From, object To)? Places => X is { } x ? (x, x) : null;

    private protected override string Needs => $"a finite {nameof(Value)} or an {nameof(X)}, and not both";

    private protected override bool HasWhatItNeeds =>
        X is null ? Value is { } value && double.IsFinite(value) : Value is null;
}
