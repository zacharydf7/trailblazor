namespace Trailblazor;

// What a grid's filter menu and a query builder share about a comparison: its name, and whether it takes a value.
internal static class Comparisons
{
    public static readonly IReadOnlyList<FilterOperator> ForText =
    [
        FilterOperator.Contains, FilterOperator.DoesNotContain, FilterOperator.StartsWith, FilterOperator.EndsWith,
        FilterOperator.EqualTo, FilterOperator.NotEqualTo, FilterOperator.IsEmpty, FilterOperator.IsNotEmpty,
    ];

    public static readonly IReadOnlyList<FilterOperator> ForNumbers =
    [
        FilterOperator.EqualTo, FilterOperator.NotEqualTo, FilterOperator.LessThan, FilterOperator.LessThanOrEqualTo,
        FilterOperator.GreaterThan, FilterOperator.GreaterThanOrEqualTo, FilterOperator.IsEmpty, FilterOperator.IsNotEmpty,
    ];

    public static string TextKeyOf(FilterOperator op) => op switch
    {
        FilterOperator.Contains => TbStrings.DataGridFilterContains,
        FilterOperator.DoesNotContain => TbStrings.DataGridFilterDoesNotContain,
        FilterOperator.StartsWith => TbStrings.DataGridFilterStartsWith,
        FilterOperator.EndsWith => TbStrings.DataGridFilterEndsWith,
        FilterOperator.EqualTo => TbStrings.DataGridFilterEqualTo,
        FilterOperator.NotEqualTo => TbStrings.DataGridFilterNotEqualTo,
        FilterOperator.LessThan => TbStrings.DataGridFilterLessThan,
        FilterOperator.LessThanOrEqualTo => TbStrings.DataGridFilterAtMost,
        FilterOperator.GreaterThan => TbStrings.DataGridFilterGreaterThan,
        FilterOperator.GreaterThanOrEqualTo => TbStrings.DataGridFilterAtLeast,
        FilterOperator.IsEmpty => TbStrings.DataGridFilterIsEmpty,
        FilterOperator.IsAnyOf => TbStrings.QueryBuilderIsAnyOf,
        _ => TbStrings.DataGridFilterIsNotEmpty,
    };

    public static bool NeedsValue(FilterOperator op) => op is not (FilterOperator.IsEmpty or FilterOperator.IsNotEmpty);
}
