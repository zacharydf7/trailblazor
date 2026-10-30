namespace Trailblazor;

// One item placed in the tree, with the parent and children it was found with.
internal sealed class TreeNode<TItem>(TItem item, object key, string id, TreeNode<TItem>? parent)
{
    public TItem Item { get; } = item;

    public object Key { get; } = key;

    // The key as text, which names the row in the browser.
    public string Id { get; } = id;

    public TreeNode<TItem>? Parent { get; } = parent;

    public int Level { get; } = parent is null ? 1 : parent.Level + 1;

    public List<TreeNode<TItem>> Children { get; } = [];

    // True while lazy children haven't loaded, so the row still shows its toggle.
    public bool MayHaveChildren { get; set; }

    public bool HasChildren => Children.Count > 0 || MayHaveChildren;

    public IEnumerable<TreeNode<TItem>> Ancestors()
    {
        for (var node = Parent; node is not null; node = node.Parent)
        {
            yield return node;
        }
    }

    public IEnumerable<TreeNode<TItem>> Descendants() =>
        Children.SelectMany(child => child.Descendants().Prepend(child));
}
