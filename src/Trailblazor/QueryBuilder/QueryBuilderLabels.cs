namespace Trailblazor;

/// <summary>The words a <see cref="TbQueryBuilder"/>'s lists and buttons show and say to screen readers. The comparisons'
/// names and the Yes and No choices aren't among them: they come from the app's text, worded as in a data grid's
/// filters.</summary>
public sealed record QueryBuilderLabels
{
    /// <summary>Names each group's list that picks whether an item must meet all of its conditions or any of them. Leave it null for the app's text for <see cref="TbStrings.QueryBuilderMatch"/>.</summary>
    public string? Match { get; init; }

    /// <summary>The Match list's choice for all of the conditions. Leave it null for the app's text for <see cref="TbStrings.QueryBuilderAll"/>.</summary>
    public string? All { get; init; }

    /// <summary>The Match list's choice for any of the conditions. Leave it null for the app's text for <see cref="TbStrings.QueryBuilderAny"/>.</summary>
    public string? Any { get; init; }

    /// <summary>Names a condition's list of fields. Leave it null for the app's text for <see cref="TbStrings.QueryBuilderField"/>.</summary>
    public string? Field { get; init; }

    /// <summary>Names a condition's list of comparisons. Leave it null for the app's text for <see cref="TbStrings.QueryBuilderComparison"/>.</summary>
    public string? Comparison { get; init; }

    /// <summary>Names a condition's value. Leave it null for the app's text for <see cref="TbStrings.DataGridFilterValue"/>.</summary>
    public string? Value { get; init; }

    /// <summary>Names a condition, with <c>{0}</c> for its number in its group. Leave it null for the app's text for <see cref="TbStrings.QueryBuilderCondition"/>.</summary>
    public string? Condition { get; init; }

    /// <summary>Names an inner group, with <c>{0}</c> for its number among its group's inner groups. Leave it null for the app's text for <see cref="TbStrings.QueryBuilderGroup"/>.</summary>
    public string? Group { get; init; }

    /// <summary>The button that adds a condition to a group. Leave it null for the app's text for <see cref="TbStrings.QueryBuilderAddCondition"/>.</summary>
    public string? AddCondition { get; init; }

    /// <summary>The button that adds a group inside a group. Leave it null for the app's text for <see cref="TbStrings.QueryBuilderAddGroup"/>.</summary>
    public string? AddGroup { get; init; }

    /// <summary>Names a condition's remove button, with <c>{0}</c> for its number. Leave it null for the app's text for <see cref="TbStrings.QueryBuilderRemoveCondition"/>.</summary>
    public string? RemoveCondition { get; init; }

    /// <summary>Names an inner group's remove button, with <c>{0}</c> for its number. Leave it null for the app's text for <see cref="TbStrings.QueryBuilderRemoveGroup"/>.</summary>
    public string? RemoveGroup { get; init; }
}
