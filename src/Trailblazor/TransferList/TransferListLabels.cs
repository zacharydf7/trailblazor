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

    /// <summary>Shown and read out in a list when it has no items. Leave it null for the app's text for
    /// <see cref="TbStrings.ListBoxEmpty"/>.</summary>
    public string? Empty { get; init; }

    /// <summary>Labels a list's filter box, with <c>{0}</c> for the list's title. Leave it null for the app's text for
    /// <see cref="TbStrings.TransferListFilter"/>.</summary>
    public string? Filter { get; init; }

    /// <summary>Names the buttons that move the marked items, with <c>{0}</c> for the title of the list they go to.
    /// Leave it null for the app's text for <see cref="TbStrings.TransferListMove"/>.</summary>
    public string? Move { get; init; }

    /// <summary>Names the buttons that move every item shown, with <c>{0}</c> for the title of the list they go to.
    /// Leave it null for the app's text for <see cref="TbStrings.TransferListMoveAll"/>.</summary>
    public string? MoveAll { get; init; }

    /// <summary>Read out after a move, with <c>{0}</c> for the number of items moved and <c>{1}</c> for the title of
    /// the list they went to. Leave it null for the app's text for <see cref="TbStrings.TransferListMoved"/>.
    /// </summary>
    public string? Moved { get; init; }
}
