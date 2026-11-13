namespace Trailblazor;

/// <summary>A move the user asked for in a <see cref="TbSortable{TItem}"/> or a reorderable
/// <see cref="TbDataGrid{TItem}"/>, by dragging or with the keyboard. The component doesn't move the item; the app
/// does, as <see cref="ApplyTo"/> shows.</summary>
/// <param name="Item">The item being moved.</param>
/// <param name="From">Its index before the move.</param>
/// <param name="To">The index it ends up at, once the move is made.</param>
public sealed record SortableMove<TItem>(TItem Item, int From, int To)
{
    /// <summary>The <see cref="TbSortable{TItem}.ListId"/> of the list the item leaves, or null for a list without
    /// one.</summary>
    public string? FromList { get; init; }

    /// <summary>The <see cref="TbSortable{TItem}.ListId"/> of the list the item goes to. It differs from
    /// <see cref="FromList"/> when the item moves to another list in its <see cref="TbSortable{TItem}.Group"/>, and
    /// <see cref="To"/> is then its index in that list.</summary>
    public string? ToList { get; init; }

    /// <summary>Makes the move in <paramref name="items"/>, the list the component shows. Throws for a move between
    /// lists, whose indexes belong to two lists; apply that with <see cref="ApplyBetween"/>.</summary>
    public void ApplyTo(IList<TItem> items)
    {
        if (FromList != ToList)
        {
            throw new InvalidOperationException($"This move goes from the list \"{FromList}\" to \"{ToList}\"; apply "
                + $"it to both with {nameof(ApplyBetween)}.");
        }

        items.RemoveAt(From);
        items.Insert(To, Item);
    }

    /// <summary>Makes a move between lists: takes the item out of <paramref name="from"/>, the list it leaves, and
    /// puts it in <paramref name="to"/>, the list it goes to.</summary>
    public void ApplyBetween(IList<TItem> from, IList<TItem> to)
    {
        from.RemoveAt(From);
        to.Insert(To, Item);
    }
}
