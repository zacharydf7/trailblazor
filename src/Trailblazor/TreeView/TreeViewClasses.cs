namespace Trailblazor;

/// <summary>Extra CSS classes for the main parts inside a <see cref="TbTreeView{TItem, TKey}"/>. Style the others by
/// their <c>data-part</c>, under a class on the tree.</summary>
public sealed record TreeViewClasses
{
    /// <summary>Added to every row (<c>data-part="item"</c>), along with the classes
    /// <see cref="TbTreeView{TItem, TKey}.ItemClass"/> gives it.</summary>
    public string? Item { get; init; }

    /// <summary>Added to every row's expand toggle (<c>data-part="toggle"</c>).</summary>
    public string? Toggle { get; init; }

    /// <summary>Added to every row's checkbox (<c>data-part="check"</c>) when the tree is
    /// <see cref="TbTreeView{TItem, TKey}.Checkable"/>.</summary>
    public string? Check { get; init; }

    /// <summary>Added to every row's content (<c>data-part="content"</c>).</summary>
    public string? Content { get; init; }

    /// <summary>Added to every row's end slot (<c>data-part="end"</c>).</summary>
    public string? End { get; init; }

    /// <summary>Added to every place an <see cref="TbTreeView{TItem, TKey}.InsertTemplate"/> fills
    /// (<c>data-part="insert"</c>).</summary>
    public string? Insert { get; init; }

    /// <summary>Added to what an empty tree shows (<c>data-part="empty"</c>).</summary>
    public string? Empty { get; init; }
}
