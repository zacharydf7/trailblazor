namespace Trailblazor;

/// <summary>The words a <see cref="TbAutocomplete{TValue}"/> shows in its list and says to screen readers.</summary>
public sealed record AutocompleteLabels
{
    /// <summary>Names the clear button. Leave it null for the app's text for <see
    /// cref="TbStrings.AutocompleteClear"/>.</summary>
    public string? Clear { get; init; }

    /// <summary>Shown in the list while a search runs. Leave it null for the app's text for <see
    /// cref="TbStrings.AutocompleteSearching"/>.</summary>
    public string? Searching { get; init; }

    /// <summary>Shown in the list when nothing matches. Leave it null for the app's text for <see
    /// cref="TbStrings.AutocompleteNoResults"/>.</summary>
    public string? NoResults { get; init; }

    /// <summary>Shown in the list, and read out, when the search fails. Leave it null for the app's text for <see
    /// cref="TbStrings.AutocompleteSearchFailed"/>.</summary>
    public string? SearchFailed { get; init; }

    /// <summary>Shown in the list before the user has typed enough to search. Leave it null for the app's text for
    /// <see cref="TbStrings.AutocompleteTypeToSearch"/>.</summary>
    public string? TypeToSearch { get; init; }

    /// <summary>Read out when the number of suggestions changes, with <c>{0}</c> for the number. Leave it null for the
    /// app's text for <see cref="TbStrings.AutocompleteResults"/>.</summary>
    public string? Results { get; init; }
}
