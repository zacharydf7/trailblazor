namespace Trailblazor;

/// <summary>The words a list box shows and says to screen readers.</summary>
public sealed record ListBoxLabels
{
    /// <summary>Shown and read out in the list when it has no options, such as "You haven't saved a trip yet". Leave it
    /// null for the app's text for <see cref="TbStrings.ListBoxEmpty"/>.</summary>
    public string? Empty { get; init; }

    /// <summary>The filter box's placeholder and accessible name, when the list box is
    /// <see cref="TbListBoxBase{TValue}.Filterable"/>. Leave it null for the app's text for
    /// <see cref="TbStrings.SelectFilter"/>.</summary>
    public string? Filter { get; init; }

    /// <summary>Shown and read out when no option matches the filter. Leave it null for the app's text for
    /// <see cref="TbStrings.SelectNoResults"/>.</summary>
    public string? NoResults { get; init; }
}
