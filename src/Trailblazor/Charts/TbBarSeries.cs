namespace Trailblazor;

/// <summary>A series of a <see cref="TbChart{TItem}"/> drawn as a bar from zero for each item, for comparing amounts.
/// The chart's <see cref="TbChart{TItem}.Stacking"/> sets whether several bar series stand side by side or stack.
/// </summary>
public sealed class TbBarSeries<TItem> : TbChartSeries<TItem>
{
    internal override bool Bars => true;
}
