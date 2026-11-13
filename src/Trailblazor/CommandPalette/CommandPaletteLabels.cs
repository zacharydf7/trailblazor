namespace Trailblazor;

/// <summary>The words a <see cref="TbCommandPalette{TItem}"/> shows and says to screen readers, other than its name and
/// placeholder.</summary>
public sealed record CommandPaletteLabels
{
    /// <summary>Shown in the list, and read out, when nothing matches. Leave it null for the app's text for <see
    /// cref="TbStrings.CommandPaletteNoResults"/>.</summary>
    public string? NoResults { get; init; }

    /// <summary>Read out when the number of results changes, with <c>{0}</c> for the number. Leave it null for the
    /// app's text for <see cref="TbStrings.CommandPaletteResults"/>.</summary>
    public string? Results { get; init; }
}
