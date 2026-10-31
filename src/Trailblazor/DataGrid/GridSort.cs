namespace Trailblazor;

/// <summary>One column a <see cref="TbDataGrid{TItem}"/> sorts by. A grid's sorts apply in order, the first deciding
/// and the rest breaking ties.</summary>
/// <param name="Column">The column's <see cref="TbColumn{TItem}.Id"/>.</param>
/// <param name="Direction">Which way it sorts.</param>
public sealed record GridSort(string Column, SortDirection Direction);
