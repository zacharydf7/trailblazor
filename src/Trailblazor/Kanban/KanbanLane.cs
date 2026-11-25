namespace Trailblazor;

/// <summary>One swimlane of a <see cref="TbKanban{TItem}"/>: a row across its columns, such as a team or a priority,
/// with its cards in each column.</summary>
/// <param name="Id">Tells the lane apart. Unique within the board.</param>
/// <param name="Title">The lane's heading, which also names its cell of each column.</param>
/// <param name="Cards">The lane's cards in each column, top first, by the column's
/// <see cref="KanbanColumn{TItem}.Id"/>. A column the lane leaves out has no cards in it.</param>
public sealed record KanbanLane<TItem>(string Id, string Title, IReadOnlyDictionary<string, IReadOnlyList<TItem>> Cards)
{
    /// <summary>The id, <c>{Id}/{columnId}</c>, that a move gives as its <see cref="SortableMove{TItem}.FromList"/> or
    /// <see cref="SortableMove{TItem}.ToList"/> for this lane's cell of the column <paramref name="columnId"/>, so the
    /// app can find the cards the move applies to. Lane and column ids can't hold a slash.</summary>
    public string CellId(string columnId) => $"{Id}/{columnId}";
}
