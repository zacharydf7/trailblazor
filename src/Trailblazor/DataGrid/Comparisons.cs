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

    public static string NameOf(FilterOperator op, ComparisonLabels labels, ITbLocalizer localizer) => op switch
    {
        FilterOperator.Contains => labels.Contains ?? localizer[TbStrings.DataGridFilterContains],
        FilterOperator.DoesNotContain => labels.DoesNotContain ?? localizer[TbStrings.DataGridFilterDoesNotContain],
        FilterOperator.StartsWith => labels.StartsWith ?? localizer[TbStrings.DataGridFilterStartsWith],
        FilterOperator.EndsWith => labels.EndsWith ?? localizer[TbStrings.DataGridFilterEndsWith],
        FilterOperator.EqualTo => labels.EqualTo ?? localizer[TbStrings.DataGridFilterEqualTo],
        FilterOperator.NotEqualTo => labels.NotEqualTo ?? localizer[TbStrings.DataGridFilterNotEqualTo],
        FilterOperator.LessThan => labels.LessThan ?? localizer[TbStrings.DataGridFilterLessThan],
        FilterOperator.LessThanOrEqualTo => labels.LessThanOrEqualTo ?? localizer[TbStrings.DataGridFilterAtMost],
        FilterOperator.GreaterThan => labels.GreaterThan ?? localizer[TbStrings.DataGridFilterGreaterThan],
        FilterOperator.GreaterThanOrEqualTo =>
            labels.GreaterThanOrEqualTo ?? localizer[TbStrings.DataGridFilterAtLeast],
        FilterOperator.IsEmpty => labels.IsEmpty ?? localizer[TbStrings.DataGridFilterIsEmpty],
        FilterOperator.IsAnyOf => labels.IsAnyOf ?? localizer[TbStrings.QueryBuilderIsAnyOf],
        _ => labels.IsNotEmpty ?? localizer[TbStrings.DataGridFilterIsNotEmpty],
    };

    public static bool NeedsValue(FilterOperator op) => op is not (FilterOperator.IsEmpty or FilterOperator.IsNotEmpty);
}
