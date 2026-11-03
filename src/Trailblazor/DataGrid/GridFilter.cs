namespace Trailblazor;

/// <summary>A condition a column's value must meet for a row of a <see cref="TbDataGrid{TItem}"/> to show. A row shows
/// when it meets all of the grid's filters, so a range is two filters on one column.</summary>
/// <param name="Column">The column's <see cref="TbColumn{TItem}.Id"/>.</param>
/// <param name="Operator">How it compares the column's value with <paramref name="Value"/>.</param>
/// <param name="Value">What it compares with: text for the text operators, which match the column's text, or its
/// <see cref="TbColumn{TItem}.FilterBy"/> value's text, ignoring case; a number, date, enum value or bool for the
/// others. A number matches any numeric type and a <see cref="DateOnly"/> matches the day of a
/// <see cref="DateTime"/> or <see cref="DateTimeOffset"/>. Unused by <see cref="FilterOperator.IsEmpty"/> and
/// <see cref="FilterOperator.IsNotEmpty"/>.</param>
public sealed record GridFilter(string Column, FilterOperator Operator, object? Value = null);
