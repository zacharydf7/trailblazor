namespace Trailblazor;

/// <summary>A total a <see cref="TbDataGrid{TItem}.DataProvider"/> is asked to work out over every row that meets the
/// filters, for the grid's totals row.</summary>
/// <param name="Column">The key of the column the total is for.</param>
/// <param name="Aggregate">Which total to work out.</param>
public sealed record GridAggregate(string Column, ColumnAggregate Aggregate);
