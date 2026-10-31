namespace Trailblazor;

/// <summary>A move the user asked for in a <see cref="TbTreeView{TItem, TKey}"/>, by dragging or with the keyboard: the
/// items to move and where to put them. The tree doesn't move them; the app does.</summary>
/// <param name="Items">The rows being moved, in the tree's order: the dragged row, or, when a selected row is dragged,
/// every selected row that shows, apart from any inside another that moves with it.</param>
/// <param name="Target">The row the items go beside or into.</param>
/// <param name="Position">Whether they go before, after or inside <paramref name="Target"/>.</param>
public sealed record TreeDrop<TItem>(IReadOnlyList<TItem> Items, TItem Target, TreeDropPosition Position);
