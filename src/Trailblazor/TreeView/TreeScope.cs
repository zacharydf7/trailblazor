namespace Trailblazor;

// The tree a TbTreeItem belongs to and the item it sits in, cascaded as one value so a tree inside a row starts afresh.
internal sealed class TreeScope<TItem>(ITreeItemHost<TItem> tree, TbTreeItem<TItem>? parent)
{
    public ITreeItemHost<TItem> Tree { get; } = tree;

    public TbTreeItem<TItem>? Parent { get; } = parent;
}
