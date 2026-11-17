namespace Trailblazor;

/// <summary>One column of a <see cref="TbKanban{TItem}"/>, such as "In progress", with its cards in order.</summary>
/// <param name="Id">Tells the column apart in a move's <see cref="SortableMove{TItem}.FromList"/> and
/// <see cref="SortableMove{TItem}.ToList"/>. Unique within the board.</param>
/// <param name="Title">The column's heading, which also names its list of cards.</param>
/// <param name="Cards">The column's cards, top first.</param>
public sealed record KanbanColumn<TItem>(string Id, string Title, IReadOnlyList<TItem> Cards)
{
    /// <summary>The most cards the column takes, such as a limit on work in progress. A move into a full column from
    /// another is refused, with the reason read out; the header shows the count against it.</summary>
    public int? Limit { get; init; }
}
