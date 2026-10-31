namespace Trailblazor;

/// <summary>A move the user asked for in a <see cref="TbSortable{TItem}"/> or a reorderable
/// <see cref="TbDataGrid{TItem}"/>, by dragging or with the keyboard. The component doesn't move the item; the app
/// does, as <see cref="ApplyTo"/> shows.</summary>
/// <param name="Item">The item being moved.</param>
/// <param name="From">Its index before the move.</param>
/// <param name="To">The index it ends up at, once the move is made.</param>
public sealed record SortableMove<TItem>(TItem Item, int From, int To)
{
    /// <summary>Makes the move in <paramref name="items"/>, the list the component shows.</summary>
    public void ApplyTo(IList<TItem> items)
    {
        items.RemoveAt(From);
        items.Insert(To, Item);
    }
}
