namespace Trailblazor;

/// <summary>Where a <see cref="TbTreeView{TItem, TKey}"/> lets rows move to, relative to the row they're dropped on.
/// Combine them, as in <c>TreeMovePositions.Before | TreeMovePositions.After</c> for a tree with no drops into a row.
/// </summary>
[Flags]
public enum TreeMovePositions
{
    /// <summary>Nowhere: no row can be moved.</summary>
    None = 0,

    /// <summary>Before the row, among its siblings.</summary>
    Before = 1,

    /// <summary>After the row, among its siblings.</summary>
    After = 2,

    /// <summary>Into the row, as its last child.</summary>
    Inside = 4,

    /// <summary>Before, after or into the row.</summary>
    All = Before | After | Inside,
}
