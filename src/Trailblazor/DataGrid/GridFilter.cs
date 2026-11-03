using System.Collections;

namespace Trailblazor;

/// <summary>A condition a column's value must meet for a row of a <see cref="TbDataGrid{TItem}"/> to show. A row shows
/// when it meets all of the grid's filters, so a range is two filters on one column. Two filters are equal when their
/// columns, operators and values are, with a list of values compared item by item.</summary>
/// <param name="Column">The column's <see cref="TbColumn{TItem}.Id"/>.</param>
/// <param name="Operator">How it compares the column's value with <paramref name="Value"/>.</param>
/// <param name="Value">What it compares with: text for the text operators, which match the column's text, or its
/// <see cref="TbColumn{TItem}.FilterBy"/> value's text, ignoring case; a number, date, enum value or bool for the
/// others; a list of those for <see cref="FilterOperator.IsAnyOf"/>. A number matches any numeric type and a
/// <see cref="DateOnly"/> matches the day of a <see cref="DateTime"/> or <see cref="DateTimeOffset"/>. Unused by
/// <see cref="FilterOperator.IsEmpty"/> and <see cref="FilterOperator.IsNotEmpty"/>.</param>
public sealed record GridFilter(string Column, FilterOperator Operator, object? Value = null)
{
    /// <summary>Whether the other filter has the same column, operator and values, with a list of values compared item
    /// by item.</summary>
    public bool Equals(GridFilter? other) => other is not null && Column == other.Column
        && Operator == other.Operator && Values.SequenceEqual(other.Values);

    /// <summary>A hash code that equal filters share.</summary>
    public override int GetHashCode() => HashCode.Combine(Column, Operator, Values.FirstOrDefault());

    // A list's items, or the value alone.
    internal IEnumerable<object?> Values =>
        Value is IEnumerable values and not string ? values.Cast<object?>() : [Value];
}
