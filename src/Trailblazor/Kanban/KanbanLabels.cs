namespace Trailblazor;

/// <summary>The words a Kanban board shows and says to screen readers. Leave any of them null for the app's text for
/// the matching <see cref="TbStrings"/> key.</summary>
public sealed record KanbanLabels
{
    /// <summary>A column's count against its <see cref="KanbanColumn{TItem}.Limit"/>, with <c>{0}</c> for the cards in
    /// it and <c>{1}</c> for the limit, for <see cref="TbStrings.KanbanCount"/>.</summary>
    public string? Count { get; init; }

    /// <summary>Why a card can't move into a full column, with <c>{0}</c> for the column's title and <c>{1}</c> for its
    /// limit, for <see cref="TbStrings.KanbanFull"/>.</summary>
    public string? Full { get; init; }

    /// <summary>Names the button that collapses a column and opens it again, with <c>{0}</c> for its title, for
    /// <see cref="TbStrings.KanbanCards"/>. Its <c>aria-expanded</c> says whether the column is open.</summary>
    public string? Cards { get; init; }

    /// <summary>The words each column's move handles show and say: their names, hint and menu, and what is read out
    /// after a move.</summary>
    public SortableLabels Moves { get; init; } = new();
}
