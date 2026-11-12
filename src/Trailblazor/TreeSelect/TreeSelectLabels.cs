namespace Trailblazor;

/// <summary>The words a tree select shows and says to screen readers.</summary>
public sealed record TreeSelectLabels
{
    /// <summary>Names the filter box and shows in it while it's empty. Leave it null for the app's text for <see
    /// cref="TbStrings.TreeSelectFilter"/>.</summary>
    public string? Filter { get; init; }

    /// <summary>Shown and read out when no item contains the filter text. Leave it null for the app's text for <see
    /// cref="TbStrings.SelectNoResults"/>.</summary>
    public string? NoResults { get; init; }

    /// <summary>Counts the checked items a multi tree select's field doesn't name, with <c>{0}</c> for how many.
    /// Leave it null for the app's text for <see cref="TbStrings.TreeSelectMore"/>.</summary>
    public string? More { get; init; }

    /// <summary>Names the clear button. Leave it null for the app's text for <see cref="TbStrings.TreeSelectClear"/>.
    /// </summary>
    public string? Clear { get; init; }

    /// <summary>The words of the tree that opens, such as its loading and retry text. Leave it null for the app's
    /// text.</summary>
    public TreeViewLabels? Tree { get; init; }
}
