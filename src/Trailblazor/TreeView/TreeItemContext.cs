namespace Trailblazor;

/// <summary>What a <see cref="TbTreeView{TItem}"/> template knows about the row it draws.</summary>
/// <param name="Item">The row's item.</param>
/// <param name="Key">The row's key, from <c>KeySelector</c>.</param>
/// <param name="Level">How deep the row is, from 1 for a root.</param>
/// <param name="Expanded">Whether the row's children are showing.</param>
/// <param name="Selected">Whether the row is selected.</param>
/// <param name="Checked">Whether the row is checked, or null for a parent whose rows are partly checked.</param>
/// <param name="HasChildren">Whether the row has children, or may have once they load.</param>
/// <param name="Loading">Whether the row's children are loading.</param>
/// <param name="Matches">Whether the row matches the tree's <c>Filter</c>, for a template that highlights it.</param>
public sealed record TreeItemContext<TItem>(TItem Item, object Key, int Level, bool Expanded, bool Selected,
    bool? Checked, bool HasChildren, bool Loading, bool Matches);
