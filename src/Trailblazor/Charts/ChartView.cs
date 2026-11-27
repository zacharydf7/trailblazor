namespace Trailblazor;

/// <summary>The part of a chart's <see cref="ChartScale.Continuous"/> scale that it shows, from <see cref="From"/>
/// to <see cref="To"/>: numbers, or dates of the type the items' <see cref="TbChart{TItem}.X"/> values are. The
/// chart gives whole days for <see cref="DateOnly"/> items, whole numbers as <see cref="long"/> for items of a
/// whole-number type, and other numbers as <see cref="double"/>.</summary>
/// <param name="From">The value at the plot's start.</param>
/// <param name="To">The value at the plot's end, after <paramref name="From"/>.</param>
public sealed record ChartView(object From, object To);
