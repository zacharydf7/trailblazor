namespace Trailblazor;

/// <summary>Whether a <see cref="TreeDrop{TItem}"/> is allowed, and if not, why, in words shown beside the drop and
/// read out, such as "A grid can't hold other items."</summary>
/// <param name="Allowed">Whether the items may move there.</param>
/// <param name="Reason">Why they may not, when they may not.</param>
public sealed record TreeDropCheck(bool Allowed, string? Reason = null)
{
    /// <summary>The move is allowed.</summary>
    public static TreeDropCheck Allow { get; } = new(true);

    /// <summary>The move isn't allowed, for this reason.</summary>
    public static TreeDropCheck Refuse(string reason) => new(false, reason);
}
