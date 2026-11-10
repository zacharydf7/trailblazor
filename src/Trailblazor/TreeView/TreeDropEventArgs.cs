namespace Trailblazor;

/// <summary>Rows of a <see cref="TbTreeView{TItem, TKey}"/> dropped on another row. Raised by the tree's script when a
/// drag ends, and by the bUnit tree driver's drag steps; consumers don't normally handle it.</summary>
public sealed class TreeDropEventArgs : EventArgs
{
    /// <summary>The <c>data-key</c> of each moved row.</summary>
    public string[] Rows { get; set; } = [];

    /// <summary>The <c>data-key</c> of the row they were dropped on.</summary>
    public string Target { get; set; } = "";

    /// <summary>Whether they go before, after or inside the target.</summary>
    public TreeDropPosition Position { get; set; }
}
