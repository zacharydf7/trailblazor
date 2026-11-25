namespace Trailblazor;

/// <summary>Whether a move the user asked for is allowed, and if not, why, in words shown beside the drop and read out,
/// such as "The default rule stays last." The <c>CanMove</c> of <see cref="TbSortable{TItem}"/>,
/// <see cref="TbKanban{TItem}"/>, <see cref="TbDataGrid{TItem}"/> and <see cref="TbTreeView{TItem, TKey}"/> answers with
/// one.</summary>
/// <param name="Allowed">Whether the items may move there.</param>
/// <param name="Reason">Why they may not, when they may not.</param>
public sealed record MoveCheck(bool Allowed, string? Reason = null)
{
    /// <summary>The move is allowed.</summary>
    public static MoveCheck Allow { get; } = new(true);

    /// <summary>The move isn't allowed, for this reason.</summary>
    public static MoveCheck Refuse(string reason) => new(false, reason);
}
