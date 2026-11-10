namespace Trailblazor;

/// <summary>The words a transfer list shows and says to screen readers.</summary>
public sealed record TransferListLabels
{
    /// <summary>The title of the list of items not chosen, such as "Hikers". Leave it null for the app's text for
    /// <see cref="TbStrings.TransferListAvailable"/>.</summary>
    public string? Available { get; init; }

    /// <summary>The title of the list of chosen items, such as "On this trip". Leave it null for the app's text for
    /// <see cref="TbStrings.TransferListChosen"/>.</summary>
    public string? Chosen { get; init; }
}
