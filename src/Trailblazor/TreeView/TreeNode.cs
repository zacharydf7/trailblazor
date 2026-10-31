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

    // A walk with a stack, in order, since nested iterators cost a step for every level above each row.
    public IEnumerable<TreeNode<TItem>> Descendants()
    {
        var stack = new Stack<TreeNode<TItem>>(Enumerable.Reverse(Children));
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            for (var index = node.Children.Count - 1; index >= 0; index--)
            {
                stack.Push(node.Children[index]);
            }
        }
    }
}
