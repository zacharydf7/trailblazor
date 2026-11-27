namespace Trailblazor;

/// <summary>One point of a <see cref="TbChart{TItem}"/>: an item's value in one series, as
/// <see cref="TbChart{TItem}.OnPointClick"/> hands it over. A <see cref="TbPieChart{TItem}"/> hands over a slice
/// the same way.</summary>
/// <param name="Item">The item the point belongs to.</param>
/// <param name="Series">The <see cref="TbChartSeries{TItem}.Name"/> of the series the point belongs to, or a
/// slice's label.</param>
/// <param name="Value">The item's value in that series.</param>
public sealed record ChartPoint<TItem>(TItem Item, string Series, double Value);
