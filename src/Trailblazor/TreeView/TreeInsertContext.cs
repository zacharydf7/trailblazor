namespace Trailblazor;

/// <summary>Where an item inserted from a <see cref="TbTreeView{TItem, TKey}.InsertTemplate"/> slot would go.</summary>
/// <param name="Parent">The item it would go inside, or null at the top level.</param>
/// <param name="Index">Its place among <paramref name="Parent"/>'s children, from 0.</param>
public sealed record TreeInsertContext<TItem>(TItem? Parent, int Index);
