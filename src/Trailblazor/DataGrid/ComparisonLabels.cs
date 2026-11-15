namespace Trailblazor;

/// <summary>The names of the comparisons a <see cref="TbQueryBuilder"/> offers, through
/// <see cref="QueryBuilderLabels.Comparisons"/>.</summary>
public sealed record ComparisonLabels
{
    /// <summary>Names <see cref="FilterOperator.EqualTo"/>. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridFilterEqualTo"/>.</summary>
    public string? EqualTo { get; init; }

    /// <summary>Names <see cref="FilterOperator.NotEqualTo"/>. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridFilterNotEqualTo"/>.</summary>
    public string? NotEqualTo { get; init; }

    /// <summary>Names <see cref="FilterOperator.Contains"/>. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridFilterContains"/>.</summary>
    public string? Contains { get; init; }

    /// <summary>Names <see cref="FilterOperator.DoesNotContain"/>. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridFilterDoesNotContain"/>.</summary>
    public string? DoesNotContain { get; init; }

    /// <summary>Names <see cref="FilterOperator.StartsWith"/>. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridFilterStartsWith"/>.</summary>
    public string? StartsWith { get; init; }

    /// <summary>Names <see cref="FilterOperator.EndsWith"/>. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridFilterEndsWith"/>.</summary>
    public string? EndsWith { get; init; }

    /// <summary>Names <see cref="FilterOperator.LessThan"/> for a number. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridFilterLessThan"/>.</summary>
    public string? LessThan { get; init; }

    /// <summary>Names <see cref="FilterOperator.LessThanOrEqualTo"/> for a number. Leave it null for the app's text
    /// for <see cref="TbStrings.DataGridFilterAtMost"/>.</summary>
    public string? LessThanOrEqualTo { get; init; }

    /// <summary>Names <see cref="FilterOperator.GreaterThan"/> for a number. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridFilterGreaterThan"/>.</summary>
    public string? GreaterThan { get; init; }

    /// <summary>Names <see cref="FilterOperator.GreaterThanOrEqualTo"/> for a number. Leave it null for the app's
    /// text for <see cref="TbStrings.DataGridFilterAtLeast"/>.</summary>
    public string? GreaterThanOrEqualTo { get; init; }

    /// <summary>Names <see cref="FilterOperator.IsEmpty"/>. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridFilterIsEmpty"/>.</summary>
    public string? IsEmpty { get; init; }

    /// <summary>Names <see cref="FilterOperator.IsNotEmpty"/>. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridFilterIsNotEmpty"/>.</summary>
    public string? IsNotEmpty { get; init; }

    /// <summary>Names <see cref="FilterOperator.IsAnyOf"/>, which a field with choices offers. Leave it null for the
    /// app's text for <see cref="TbStrings.QueryBuilderIsAnyOf"/>.</summary>
    public string? IsAnyOf { get; init; }
}
