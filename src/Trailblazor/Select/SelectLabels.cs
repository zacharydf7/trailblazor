namespace Trailblazor;

/// <summary>The words a select shows and says to screen readers.</summary>
public sealed record SelectLabels
{
    /// <summary>Names the filter box and shows in it while it's empty. Leave it null for the app's text for <see
    /// cref="TbStrings.SelectFilter"/>.</summary>
    public string? Filter { get; init; }

    /// <summary>Shown and read out when no option contains the filter's text. Leave it null for the app's text for
    /// <see cref="TbStrings.SelectNoResults"/>.</summary>
    public string? NoResults { get; init; }

    /// <summary>Names the clear button. Leave it null for the app's text for <see cref="TbStrings.SelectClear"/>.
    /// </summary>
    public string? Clear { get; init; }

    /// <summary>Read out when a chip is removed from a multiple select, with <c>{0}</c> for its option. Leave it null
    /// for the app's text for <see cref="TbStrings.MultiSelectRemoved"/>.</summary>
    public string? Removed { get; init; }

    /// <summary>Names the option that chooses every option of a multiple select. Leave it null for the app's text for
    /// <see cref="TbStrings.MultiSelectSelectAll"/>.</summary>
    public string? SelectAll { get; init; }
}
