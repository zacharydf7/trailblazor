using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A series of a <see cref="TbChart{TItem}"/> drawn as a line with the area between it and zero filled, for
/// an amount whose size matters as well as how it changes from one item to the next. With the chart's
/// <c>Stacking</c>, area series pile on one another to show the parts of a total. A missing value leaves a gap.
/// </summary>
public sealed class TbAreaSeries<TItem> : TbChartSeries<TItem>
{
    /// <summary>How the line joins its points. <see cref="ChartCurve.Straight"/> by default. Give the areas of a
    /// stack the same curve: each area's lower edge is drawn with its own, and parts from the line of the area under
    /// it when the two differ.</summary>
    [Parameter]
    public ChartCurve Curve { get; set; }

    /// <summary>Whether each point has a marker. Without it, a series of 30 points or fewer has them, and a longer
    /// one doesn't.</summary>
    [Parameter]
    public bool? Markers { get; set; }

    /// <summary>Whether the series writes each value over its point, as the data table writes it. The series shows
    /// them all while the room between the closest two places is as wide as the longest of them, and none otherwise.
    /// False by default.</summary>
    [Parameter]
    public bool ShowValues { get; set; }

    internal override bool Bars => false;

    internal override bool Filled => true;

    internal override ChartCurve Line => Curve;

    internal override bool? HasMarkers => Markers;

    internal override bool ShowsValues => ShowValues;
}
