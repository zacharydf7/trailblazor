namespace Trailblazor;

/// <summary>The words a cascader shows and says to screen readers. Leave any of them null for the app's text for the
/// matching <see cref="TbStrings"/> key.</summary>
public sealed record CascaderLabels
{
    /// <summary>Names a choice that opens more choices, with <c>{0}</c> for its text, for
    /// <see cref="TbStrings.CascaderMore"/>.</summary>
    public string? More { get; init; }

    /// <summary>Read out with the number of paths that match what was typed, with <c>{0}</c> for the number, for
    /// <see cref="TbStrings.CascaderResults"/>.</summary>
    public string? Results { get; init; }

    /// <summary>Shown when no path matches what was typed, for <see cref="TbStrings.CascaderNoResults"/>.</summary>
    public string? NoResults { get; init; }

    /// <summary>The clear button's name, for <see cref="TbStrings.CascaderClear"/>.</summary>
    public string? Clear { get; init; }
}
