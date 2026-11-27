using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A tinted band across a <see cref="TbChart{TItem}"/> with its name and ends written in it: between two
/// values of its value axis, <see cref="From"/> and <see cref="To"/>, such as a normal range, or between two places
/// along its items, <see cref="FromX"/> and <see cref="ToX"/>, such as the days of a closure. Write it inside the
/// chart, with one of the two pairs. The value axis reaches <see cref="From"/> and <see cref="To"/> unless the
/// chart's <see cref="TbYAxis"/> sets that end, or the chart stacks its series to shares of a whole, whose axis ends
/// at 100%.</summary>
public sealed class TbReferenceBand : TbChartReference
{
    /// <summary>The value of the value axis the band starts at, finite and below <see cref="To"/>. With
    /// <see cref="ChartStacking.Percent"/> both are shares from 0 to 1, as the axis's values are.</summary>
    [Parameter]
    public double? From { get; set; }

    /// <summary>The value of the value axis the band ends at.</summary>
    [Parameter]
    public double? To { get; set; }

    /// <summary>The place along the chart's items the band starts at, in place of <see cref="From"/> and
    /// <see cref="To"/>. On a continuous scale it is any date or finite number, of the kind the items' <c>X</c>
    /// values are. On an even scale it is one item's <c>X</c> value, where numbers of any type match by value and
    /// other values match only one of the same type, and the band starts where that item's place does.</summary>
    [Parameter]
    public object? FromX { get; set; }

    /// <summary>The place along the chart's items the band ends at: after <see cref="FromX"/> on a continuous
    /// scale, and on an even scale the <c>X</c> value of the same item or a later one, whose place the band covers.
    /// </summary>
    [Parameter]
    public object? ToX { get; set; }

    internal override (double Low, double High)? Values => From is { } from && To is { } to ? (from, to) : null;

    internal override (object From, object To)? Places => FromX is { } from && ToX is { } to ? (from, to) : null;

    private protected override string Needs =>
        $"a finite {nameof(From)} below its {nameof(To)}, or a {nameof(FromX)} and a {nameof(ToX)}, and not both";

    private protected override bool HasWhatItNeeds => FromX is null && ToX is null
        ? From is { } from && To is { } to && double.IsFinite(from) && double.IsFinite(to) && from < to
        : FromX is not null && ToX is not null && From is null && To is null;
}
