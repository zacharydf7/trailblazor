using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A series of a <see cref="TbChart{TItem}"/> drawn as a bar from zero for each item, for comparing amounts.
/// The chart's <see cref="TbChart{TItem}.Stacking"/> sets whether several bar series stand side by side or stack.
/// </summary>
public sealed class TbBarSeries<TItem> : TbChartSeries<TItem>
{
    /// <summary>Whether the series writes each value on its bar, as the data table writes it: past the bar's end,
    /// or in its middle when the chart stacks its bars. The series shows them all while each of its bars is as wide
    /// as the longest of them, and none otherwise; in a stack a bar shows its own while it is as wide as that value
    /// and a line of text high. In a horizontal chart a bar instead needs to be a line of text thick, and in a stack
    /// also as long as its label. False by default.</summary>
    [Parameter]
    public bool ShowValues { get; set; }

    internal override bool Bars => true;

    internal override bool ShowsValues => ShowValues;
}
