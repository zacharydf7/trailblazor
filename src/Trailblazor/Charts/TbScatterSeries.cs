namespace Trailblazor;

/// <summary>A series of a <see cref="TbChart{TItem}"/> drawn as a mark for each item and no line between them, for
/// how two measures go together, such as a trail's length and its climb. It usually sits on a
/// <see cref="ChartScale.Continuous"/> scale, which places each mark by the item's <c>X</c>. A chart's scatter
/// series take a circle, a square, a diamond and a ring as their marks, in the order they are given, so they differ
/// by shape as well as by color; a fifth starts over. A missing value has no mark.</summary>
public sealed class TbScatterSeries<TItem> : TbChartSeries<TItem>
{
    internal override bool Bars => false;

    internal override bool Dots => true;
}
